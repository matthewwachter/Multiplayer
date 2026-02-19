# Desync Detection

A **desync** occurs when two clients' game states diverge — they executed the same commands but ended up with different results. The desync detection system catches this by comparing RNG state snapshots after every tick and command.

## What Causes Desyncs

In deterministic lockstep, all clients must produce identical results from identical inputs. Desyncs happen when something breaks determinism:

| Cause | Example | Mitigation |
|-------|---------|------------|
| Unseeded `Rand` calls | `Rand.Value` called from a UI callback during ticking | Harmony patches on Rand to track stack traces |
| Hash code instability | `Dictionary` iteration using `object.GetHashCode()` | `HashCodes.cs` patches `System.HashCode.Combine` to use deterministic hashing |
| Floating-point rounding | Different CPUs using different rounding modes | `RoundMode` check stored in every opinion |
| Missing sync patches | A UI action modifies game state without going through sync | Stack trace analysis in desync ZIP |
| Cache pollution | `PawnCapacitiesHandler` caching different values per-context | `Determinism.cs` separates cache for interface vs. ticking |
| Time-dependent code | `DateTime.Now` or `Time.deltaTime` in game logic | Patches replace with constants or game-tick-based values |

## SyncCoordinator

`SyncCoordinator` (`Source/Client/Desyncs/SyncCoordinator.cs`) is the central orchestrator. It builds opinions, compares them across clients, and handles desyncs.

### Opinion Building

After each tick or command, the coordinator records RNG state:

```csharp
// Called from AsyncTimeComp.Tick() / PostContext()
sync.TryAddMapRandomState(map.uniqueID, randState);

// Called from AsyncWorldTimeComp.Tick() / PostContext()
sync.TryAddWorldRandomState(randState);

// Called from ExecuteCmd() finally blocks
sync.TryAddCommandRandomState(randState);
```

Each method extracts the upper 32 bits of the 64-bit RNG state: `(uint)(state >> 32)`.

### Opinion Comparison

When a client receives a remote opinion (via `Server_SyncInfo` packet), `AddClientOpinionAndCheckDesync()` compares it with local opinions at the same tick:

1. Find matching opinions by `startTick`
2. Call `CheckForDesync()` which compares:
   - Floating-point rounding modes
   - Map IDs present
   - Per-map random states (list comparison)
   - World random states
   - Command random states
   - Stack trace hashes (unless simulating)
3. If any differ, call `HandleDesync()`

### HandleDesync

```csharp
void HandleDesync(ClientSyncOpinion local, ClientSyncOpinion remote, string message)
{
    // Find exact divergence point
    int diffAt = FindTraceHashesDiffTick(local, remote);

    // Notify server
    Send(Packets.Client_Desynced, startTick, diffAt);

    // Save diagnostics
    var desyncInfo = new SaveableDesyncInfo(this, local, remote, diffAt, ...);

    if (settings.autoRejoinOnDesync && rejoinAttempts < MaxAutoRetries)
    {
        rejoinAttempts++;
        desyncInfo.SaveWhenReady();  // Background save
        Rejoiner.DoRejoin();         // Auto-rejoin
    }
    else
    {
        Show DesyncedWindow;         // Manual recovery
    }
}
```

## ClientSyncOpinion

`ClientSyncOpinion` (`Source/Client/Desyncs/ClientSyncOpinion.cs`) stores one "snapshot" of game state for comparison:

```csharp
public class ClientSyncOpinion(int startTick)
{
    public bool isLocalClientsOpinion;

    // RNG snapshots (upper 32 bits only)
    public List<uint> commandRandomStates;
    public List<uint> worldRandomStates;
    public List<MapRandomStateData> mapStates;  // {mapId, List<uint>}

    // Stack trace hashes for precise divergence location
    public List<int> desyncStackTraceHashes;
    public List<StackTraceLogItem> desyncStackTraces;

    // Metadata
    public bool simulating;
    public RoundModeEnum roundMode;
}
```

### Comparison Logic

`CheckForDesync()` returns null if opinions match, or an error message describing the first difference:

1. Compare `roundMode` — different CPU rounding modes
2. Compare map IDs — different maps loaded
3. Compare per-map `randState` lists — different RNG after map ticks
4. Compare world `randState` list — different RNG after world ticks
5. Compare command `randState` list — different RNG after commands
6. Compare trace hashes — pinpoints the exact divergent call (skipped during simulation)

### Network Serialization

Opinions are serialized to `SyncOpinion` structs and exchanged via `ClientSyncInfoPacket` / `ServerSyncInfoPacket`. The coordinator maintains a backlog of up to 30 opinions for comparison.

## Detection Flow

```
Game Tick / Command Execution
│
├── PreContext()
│   ├── Rand.PushState()
│   └── Rand.StateCompressed = savedRandState
│
├── (game code runs, calling Rand.Value, Rand.Int, etc.)
│
├── PostContext()
│   ├── randState = Rand.StateCompressed    ◄── capture new RNG
│   └── Rand.PopState()
│
├── TryAdd*RandomState(randState)           ◄── record in opinion
│
└── FinishLocalOpinion()                    ◄── after all ticks/cmds
    ├── Capture RoundMode
    └── Serialize & send to server (Client_SyncInfo)

Server receives opinion
│
└── Broadcasts via Server_SyncInfo to other clients

Client receives remote opinion
│
└── AddClientOpinionAndCheckDesync()
    ├── Find matching local opinion by startTick
    ├── CheckForDesync() — compare all fields
    └── If mismatch → HandleDesync()
```

## Auto-Rejoin

When a desync is detected, the mod can automatically attempt to rejoin:

| Setting | Default | Purpose |
|---------|---------|---------|
| `autoRejoinOnDesync` | `true` | Enable automatic rejoin |
| Max retries | 3 | `Rejoiner.MaxAutoRetries` |

### Rejoin Flow

1. Send `Client_RequestRejoin` to server
2. Server transitions connection back to `ServerLoading`
3. Client clears game state: `MemoryUtility.ClearAllMapsAndWorld()`
4. Server sends fresh world data
5. Client reloads — full state reset
6. If rejoin fails after 3 attempts, show `DesyncedWindow` for manual recovery

### DesyncedWindow

Provides manual recovery options:
- **Try Resync** — manually trigger rejoin
- **Save** — save current game state
- **Chat** — open chat window
- **Open Folder** — open desync diagnostics folder (once ZIP is saved)
- **Quit** — return to main menu

## Desync Diagnostics

`SaveableDesyncInfo` (`Source/Client/Desyncs/SaveableDesyncInfo.cs`) collects diagnostic data into a ZIP file:

### ZIP Contents

| File | Contents |
|------|----------|
| `desync_info` | System metadata: CPU, GPU, RAM, OS, mod versions, debug flags |
| `local_traces.txt` | Stack traces with RNG state and context from local client |
| `host_traces.txt` | Stack traces from the server/host |
| `local_logs.txt` | Game logs (privacy-redacted: paths, IDs, renderer info removed) |
| `local_metadata.txt` | Active mods list and Harmony patch summary |
| `replay.rwmts` | Game replay file (if `includeReplayInDesync` setting enabled) |

ZIPs are saved to `Multiplayer.DesyncsDir` as `Desync-{N:00}.zip`, auto-incrementing. Only the 10 most recent files are kept.

### Trace Analysis

When a desync occurs, `FindTraceHashesDiffTick()` locates the first trace hash that differs between local and remote opinions. The `diffAt` index pinpoints the exact Rand call where divergence started.

`GetFormattedStackTracesForRange(diffAt)` then formats traces within a radius (default 40) around the divergence point, showing:
- Tick number
- RNG state at that point
- Faction context
- Call depth
- Thing context (if applicable)
- Full stack trace

## Determinism Patches

`Determinism.cs` contains ~35 Harmony patches that enforce determinism. Key categories:

### Pawn Position

`DrawPosPatch` returns the root position (not tweened) during ticking, preventing position interpolation from affecting game logic.

### Sort Stability

`FixApparelSort` breaks ties in apparel sorting by `thingIDNumber`, ensuring identical sort order across clients.

### Cell Shuffling

`CellsShufflePatchShared` only shuffles cells when `Multiplayer.Ticking == true`, preventing UI-triggered shuffles from affecting game state.

### Cache Isolation

`PawnCapacitiesHandlerGetLevelPatch` maintains separate cache validity for interface vs. ticking contexts, preventing UI reads from polluting tick-time caches.

### Time Independence

`MapBrightnessLerpPatch` replaces `Time.deltaTime` with a constant `1/60f`, ensuring brightness calculations are frame-rate independent.

## Hash Code Patches

`HashCodes.cs` intercepts `System.HashCode.Combine()` (2-8 argument overloads) and replaces them with `DeterministicHash.HashCombineInt()`. This prevents .NET's randomized hash codes from causing non-deterministic dictionary iteration order.

Special cases:
- `Map.GetHashCode()` → uses `map.uniqueID` instead of memory address
- `GlobalTargetInfo` / `TargetInfo` → combines with `map?.uniqueID ?? -1`
- `GlowGrid.litGlowers` → custom equality using `parent.thingIDNumber`

## Common Desync Causes for Contributors

When adding new sync handlers or patches, watch for:

1. **Unseeded Rand calls**: Any `Rand.Value` or `Rand.Int` called during ticking without proper context will diverge. Always ensure Rand calls happen within `PreContext/PostContext` pairs.

2. **Dictionary iteration**: Never rely on `Dictionary<K,V>` iteration order for game logic. Use sorted alternatives or iterate by a deterministic key.

3. **Static mutable state**: Avoid modifying static fields from UI code that also runs during ticking. Use context flags to guard.

4. **Missing patches**: If a vanilla or mod method uses `Rand` internally and you're calling it from a new code path, ensure it's within a proper RNG context.

5. **Hash code instability**: If you're using objects as dictionary keys or in hash sets during ticking, ensure their hash codes are deterministic (not based on memory addresses).

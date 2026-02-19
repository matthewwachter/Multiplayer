# Architecture

## Project Structure

```
Source/
├── Client/          (.NET Framework 4.8)  Main mod loaded by RimWorld
├── Common/          (.NET Framework 4.8)  Shared library for client and server
├── Server/          (.NET 8.0)            Standalone headless server
├── MultiplayerLoader/ (.NET Framework 4.8) Early mod loader via Zetrith.Prepatcher
└── Tests/           (.NET 8.0)            NUnit tests with Verify snapshot testing
```

**Client** contains Harmony patches, UI, networking client, sync system, and async time management. Entry point: `Multiplayer.cs` &rarr; `InitMultiplayer()`.

**Common** provides the networking protocol (LiteNetLib/Steam/in-memory), packet serialization (`ByteReader`/`ByteWriter`), version info, and sync type helpers. Used by both Client and Server.

**Server** is a standalone headless server that can be containerized via Dockerfile. Targets .NET 8.0 so it can run without RimWorld installed.

**MultiplayerLoader** uses Zetrith.Prepatcher for pre-initialization patches that must run before the game's assemblies load.

**Tests** uses NUnit and Verify for snapshot testing. Test data lives in `packet-serializations/`.

### Target Frameworks

Client and Common target .NET Framework 4.8 because RimWorld runs on Unity's Mono runtime. Server and Tests target .NET 8.0 since they run independently of the game.

## Code Organization

### Patches Split

The original monolithic `Patches.cs` has been split into domain-specific files under `Source/Client/Patches/`:

| File | Domain |
|------|--------|
| `Dialogs.cs` | Dialog and window patches |
| `GameFlow.cs` | Game startup, loading, saving flow |
| `Input.cs` | Input handling and key bindings |
| `Jobs.cs` | Job and work-related patches |
| `PawnSetup.cs` | Pawn creation and initialization |
| `Saving.cs` | Save/load system patches |
| `SyncOptimization.cs` | Performance optimizations for sync |

The original `Patches.cs` still exists with remaining general-purpose patches.

### SyncDict Split

`SyncDictRimWorld` is a partial class split across 5 domain-specific files in `Source/Client/Syncing/Dict/`:

- `SyncDictCore.cs` — Core game types
- `SyncDictPawns.cs` — Pawn-related types
- `SyncDictWorld.cs` — World and map types
- `SyncDictUI.cs` — UI and dialog types
- `SyncDictBuildings.cs` — Building and facility types

The main `SyncDictRimWorld.cs` merges all trees via `SyncWorkerDictionaryTree.Merge()`.

### Utility Types in Common

Several utility types live in `Source/Common/` for shared use by both Client and Server: `CollectionExtensions`, `DeterministicHash`, `Container`, `UniqueList`, `IdentityComparer`, `Utf8StringWriter`, `FixedSizeQueue`.

## Key Statics

The `Multiplayer` static class (`Source/Client/Multiplayer.cs`) is the central state holder:

| Property | Type | Purpose |
|----------|------|---------|
| `session` | `MultiplayerSession` | Current multiplayer session (null when not in MP) |
| `game` | `MultiplayerGame` | Current game state |
| `Client` | `ConnectionBase` | Connection to server (shortcut to `session?.client`) |
| `LocalServer` | `MultiplayerServer` | Local server instance (host only) |
| `settings` | `MpSettings` | Mod settings |

### Context Flags

These flags determine what execution phase the mod is in:

```csharp
// True when in the UI thread and should intercept actions for sync
bool ShouldSync => InInterface && !dontSync;

// True when a player is interacting (not ticking, not executing commands)
bool InInterface =>
    Client != null
    && !Ticking
    && !ExecutingCmds
    && !reloading
    && Current.ProgramState == ProgramState.Playing
    && LongEventHandler.currentEvent == null;

// True during game tick execution
bool Ticking => AsyncWorldTimeComp.tickingWorld
    || AsyncTimeComp.tickingMap != null
    || ConstantTicker.ticking;

// True while executing a synced command
bool ExecutingCmds => TickPatch.currentExecutingCmdType != null;
```

## Initialization Flow

```
InitMultiplayer(ModContentPack)
│
├── Native.EarlyInit()                    Platform-specific setup
├── TypeCache.CacheTypeHierarchy()        Build type hierarchy for sync
├── TypeCache.CacheTypeByName()           Build type name lookup
│
├── EarlyInit.ProcessEnvironment()        Check restart/reconnect env vars
├── EarlyInit.EarlyPatches(harmony)       Apply [EarlyPatch] Harmony patches
│                                         + Rand determinism patches
│
├── EarlyInit.InitSync()
│   ├── RwSerialization.Init()            Initialize serialization system
│   ├── SyncDict.Init()                   Register all SyncWorkers
│   ├── SyncGame.Init()                   Register SyncFields, SyncMethods, etc.
│   ├── Sync.RegisterAllAttributes()      Scan for [SyncMethod] and similar
│   └── Sync.ValidateAll()               Validate all handlers, compute HandlerHash
│
└── EarlyInit.LatePatches()               Final patches after all mods load
    └── (prints SyncDict structure in debug)
```

## Subsystem Connections

```
┌─────────────┐     ┌───────────────┐     ┌──────────────┐
│  Sync System │────▶│ Serialization │────▶│  Networking   │
│ (handlers)   │     │ (SyncWorkers) │     │  (packets)   │
└──────┬──────┘     └───────────────┘     └──────┬───────┘
       │                                          │
       │    ┌──────────────┐                     │
       └───▶│  Async Time  │◀────────────────────┘
            │  (ticking)   │
            └──────┬───────┘
                   │
            ┌──────▼──────────┐
            │ Desync Detection │
            │  (RNG tracking)  │
            └─────────────────┘
```

- **Sync System** intercepts player actions and serializes them using **Serialization** (SyncWorkers).
- Serialized commands are sent over the **Networking** layer to the server, which broadcasts them.
- **Async Time** executes commands at the correct tick, managing per-map and world ticking.
- **Desync Detection** records RNG state after every tick and command, comparing opinions across clients.

### Additional Subsystems

- **Factions** (`Source/Client/Factions/`) — Multi-faction support, faction sidebar UI, blueprint ownership, and faction context switching.
- **Sessions** (`Source/Client/Session/`) — Multiplayer session management, rejoin logic, autosaving, and player info tracking.
- **Persistent** (`Source/Client/Persistent/`) — Long-lived UI sessions for trading, caravan forming/splitting, and gravship travel.
- **Settings** (`Source/Client/Settings/`) — `MpSettings` configuration and settings UI.

## Key Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Lib.Harmony | 2.4.1 | Runtime IL patching |
| LiteNetLib | 1.3.1 | UDP networking |
| Krafs.Rimworld.Ref | 1.6.4566 | RimWorld API references (no game install needed) |
| Krafs.Publicizer | 2.3.0 | Exposes private game types at build time |
| RimWorld.MultiplayerAPI | 0.6.0 | Public API for mod compatibility |

## Version & Protocol

Defined in `Source/Common/Version.cs`:

```csharp
public const string SimpleVersion = "0.11.0";
public const int Protocol = 53;
```

Protocol version must be bumped when network-incompatible changes are made. Clients and server compare protocol versions during the join handshake.

In addition to protocol version, a **sync handler hash** (`Sync.HandlerHash`) is computed over all sync handler definitions and compared during the handshake. This catches cases where the mod version matches but sync handler registrations differ (e.g., different mod compatibility patches loaded).

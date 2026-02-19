# Async Time

Async time allows each map to tick independently, so maps at different speeds don't block each other. The world and each map are separate **tickables**, each with their own command queue, tick counter, and RNG state.

## ITickable Interface

```csharp
public interface ITickable
{
    int TickableId { get; }                          // Map uniqueID or -1 for world
    Queue<ScheduledCommand> Cmds { get; }            // Commands queued for execution
    float TimeToTickThrough { get; set; }            // Time accumulator
    TimeSpeed DesiredTimeSpeed { get; set; }          // Current speed setting
    float TickRateMultiplier(TimeSpeed speed);        // Speed → multiplier
    void Tick();                                      // Execute one game tick
    void ExecuteCmd(ScheduledCommand cmd);            // Execute a synced command
}
```

Two implementations exist:
- `AsyncTimeComp` — per-map tickable (one per loaded map)
- `AsyncWorldTimeComp` — world-level tickable (singleton)

## TickPatch: The Orchestrator

`TickPatch` (`Source/Client/Patches/TickPatch.cs`) is a Harmony patch on `TickManager.TickManagerUpdate()`. It replaces RimWorld's tick loop with one that supports async time.

### Execution Flow

```
TickPatch.Prefix()
│
├── Calculate ticksToRun from frame time + server time-per-tick
│
├── RunCmds()                          Execute commands at current Timer
│   └── For each ITickable:
│       └── While Cmds.Peek().ticks == Timer:
│           └── target.ExecuteCmd(cmd)
│
└── DoUpdate()                         Perform game ticks
    └── While ticksToRun > 0:
        ├── RunCmds()                  Check for more commands
        └── DoTick()
            ├── For each ITickable:
            │   └── tickable.Tick()    If speed > 0
            ├── ConstantTicker.Tick()  Things that tick regardless of speed
            └── Timer += 1
```

### Key Properties

| Property | Purpose |
|----------|---------|
| `Timer` | Global absolute tick counter (all clients stay in sync) |
| `ticksToRun` | How many ticks to run this frame |
| `tickUntil` | Remote tick limit from server (client can't go past this) |
| `Simulating` | True during automated tick runs (e.g., join point creation) |
| `currentExecutingCmdType` | `CommandType` of command being executed (null if not executing) |
| `currentExecutingCmdIssuedBySelf` | True if the current command came from the local player |

## AsyncTimeComp (Per-Map)

`AsyncTimeComp` (`Source/Client/AsyncTime/AsyncTimeComp.cs`) manages per-map ticking.

### Key Fields

```csharp
public Map map;
public int mapTicks;                          // Per-map tick counter
private TimeSpeed timeSpeedInt;               // Desired speed
public TimeSlower slower;                     // RimWorld's forced-normal-speed mechanism
public TickList tickListNormal, Rare, Long;   // Ticker lists by frequency
public ulong randState;                       // RNG state (compressed)
public Queue<ScheduledCommand> cmds;          // Command queue
public int CurrentPlayerCount;                // Players viewing this map
```

### PreContext / PostContext

Before and after each tick or command execution, the map saves and restores its context:

```csharp
public void PreContext()
{
    map.PushFaction(...);                     // Set faction context
    prevTime = TimeSnapshot.GetAndSetFromMap(map);  // Save/set TickManager state
    Rand.PushState();
    Rand.StateCompressed = randState;        // Restore this map's RNG
}

public void PostContext()
{
    prevTime?.Set();                         // Restore TickManager state
    randState = Rand.StateCompressed;        // Save updated RNG
    Rand.PopState();                         // Restore global RNG stack
    map.PopFaction();
}
```

This ensures each map has isolated RNG, faction context, and tick counts.

### Tick()

```
PreContext()
├── MapPreTick()
├── mapTicks++
├── tickListNormal / Rare / Long .Tick()
├── Storyteller.StorytellerTick()
├── QuestManagerTickAsyncTime()
├── MapPostTick()
├── UpdateManagers()        (regions, power, glow)
└── CacheNothingHappening() (for Superfast speed check)
PostContext()
└── SyncCoordinator.TryAddMapRandomState(map.uniqueID, randState)
```

### ExecuteCmd()

Handles `CommandType.Sync`, `DebugTools`, `MapTimeSpeed`, and `Designator`:

```csharp
public void ExecuteCmd(ScheduledCommand cmd)
{
    PreContext();
    map.PushFaction(cmd.GetFaction(), force: true);

    if (cmdType == CommandType.Sync)
        SyncUtil.HandleCmd(data);

    if (cmdType == CommandType.MapTimeSpeed)
        DesiredTimeSpeed = (TimeSpeed)data.ReadByte();

    // ... other command types

    PostContext();
    SyncCoordinator.TryAddCommandRandomState(randState);
}
```

### Speed Multipliers

```csharp
public float TickRateMultiplier(TimeSpeed speed)
{
    if (sessionPausing) return 0f;               // Session forces pause
    if (mapTicks < slower.forceNormalSpeedUntil)
        return speed == TimeSpeed.Paused ? 0 : 1; // Forced normal

    return speed switch
    {
        TimeSpeed.Paused    => 0f,
        TimeSpeed.Normal    => 1f,
        TimeSpeed.Fast      => 3f,
        TimeSpeed.Superfast => nothingHappeningCached ? 12f : 6f,
        TimeSpeed.Ultrafast => 15f,
    };
}
```

Superfast returns 12x when all colonists are idle (no humanlike colonists awake, no danger), otherwise 6x.

## AsyncWorldTimeComp (World-Level)

`AsyncWorldTimeComp` (`Source/Client/AsyncTime/AsyncWorldTimeComp.cs`) manages world-level ticking.

### World Speed Derivation

When maps exist, the world speed is derived from the **fastest non-paused map**:

```csharp
public TimeSpeed DesiredTimeSpeed
{
    get => !Find.Maps.Any()
        ? timeSpeedInt
        : Find.Maps.Select(m => m.AsyncTime())
            .Where(a => a.ActualRateMultiplier(a.DesiredTimeSpeed) != 0f)
            .Max(a => a?.DesiredTimeSpeed) ?? TimeSpeed.Paused;
}
```

### Global Commands

The world tickable handles commands that affect the entire game:

| CommandType | Action |
|-------------|--------|
| `GlobalTimeSpeed` | Set world time speed |
| `TimeSpeedVote` | Process a player's speed vote |
| `PauseAll` | Pause all maps and world |
| `CreateJoinPoint` | Create a save point for new player joins |
| `InitPlayerData` | Initialize player capabilities (dev mode) |
| `PlayerCount` | Track which map a player is viewing |

## ScheduledCommand

```csharp
public class ScheduledCommand(
    CommandType type,
    int ticks,        // Tick to execute at
    int factionId,    // Faction context (-1 for no faction)
    int mapId,        // Target map (-1 for global)
    int playerId,     // Player who issued the command
    byte[] data       // Command-specific payload
)
```

Commands are queued in `ITickable.Cmds` and dequeued by `TickPatch.RunCmds()` when `Timer` reaches their target tick.

## CommandType

```csharp
public enum CommandType : byte
{
    GlobalTimeSpeed,   TimeSpeedVote,    PauseAll,
    CreateJoinPoint,   InitPlayerData,
    Sync,              DebugTools,
    MapTimeSpeed,      Designator,       PlayerCount,
}
```

## Time Speed Voting

When the server uses `TimeControl.LowestWins`, each player votes for a speed per tickable. The actual speed is the **minimum** across all votes:

```csharp
public TimeSpeed GetLowestTimeVote(int tickableId)
{
    return (TimeSpeed)playerData.Values
        .SelectMany(p => p.AllTimeVotes.GetOrEmpty(tickableId))
        .DefaultIfEmpty(TimeVote.Paused)
        .Min();
}
```

### TimeVote Enum

```csharp
enum TimeVote : byte
{
    Paused, Normal, Fast, Superfast, Ultrafast,
    PlayerResetTickable,   // Player clears vote for one tickable
    PlayerResetGlobal,     // Player clears all votes
    ResetTickable,         // Host clears one tickable
    ResetGlobal            // Host clears all
}
```

### TimeControl Modes

```csharp
enum TimeControl
{
    EveryoneControls,  // Each player votes, lowest wins
    LowestWins,        // Same as EveryoneControls
    HostOnly           // Only host can change speed
}
```

## VTR (Votes-To-Run)

VTR controls the visual update rate for things like animations and projectiles:

```csharp
const int MaximumVtr = 15;  // Update every 15 ticks (no players viewing)
const int MinimumVtr = 1;   // Update every tick (players viewing)

public int VTR => CurrentPlayerCount > 0 ? MinimumVtr : MaximumVtr;
```

When players view a map, animations update every tick (smooth). When no one is looking, they update every 15 ticks (saves bandwidth). `PlayerCount` commands track which map each player is viewing.

## SetMapTime (TimeSnapshot)

`SetMapTime` (`Source/Client/AsyncTime/SetMapTime.cs`) patches UI methods to temporarily set `TickManager` state to the current map's time context. This ensures UI elements (alerts, tooltips, glow calculations) read the correct map's tick count and speed.

```csharp
public struct TimeSnapshot
{
    public int ticks;
    public TimeSpeed speed;
    public TimeSlower slower;
    public int gameStartAbsTick;
}
```

Patched methods include `MapInterface.MapInterfaceOnGUI_*`, `AlertsReadout`, `PawnTweener`, `SoundRoot.Update`, and more. Each gets a Harmony prefix that saves state and a finalizer that restores it.

## Critical Rule: RNG State Isolation

`Rand.PushState()` and `Rand.PopState()` **must always be paired**. Every `PreContext()` pushes a new RNG state, and every `PostContext()` pops it. Unpaired calls corrupt the RNG stack and cause desyncs.

The RNG state after each tick/command is recorded by the [desync detection](desync-detection) system for cross-client comparison.

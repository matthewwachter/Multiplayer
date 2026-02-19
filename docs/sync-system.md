# Sync System

The sync system is the core mechanism for synchronizing game state across players. RimWorld Multiplayer uses **deterministic lockstep**: all clients execute the same commands in the same order at the same tick, so their game states stay identical.

## How Sync Works

When a player performs an action (e.g., clicking a button in the UI), the mod intercepts it before it modifies game state. Instead of executing immediately, the action is:

1. Serialized into a binary command
2. Sent to the server
3. Broadcast to all clients (including the sender)
4. Executed by all clients at the same tick

This ensures every client processes the same actions in the same order.

## Handler Types

Four handler types cover different kinds of player actions:

### SyncField

Watches for changes to a specific field on a target object. When the field value changes in the UI, the new value is synced to all clients.

```csharp
// Registration
Sync.Field(typeof(Pawn_PlayerSettings), nameof(Pawn_PlayerSettings.AreaRestriction));
Sync.Field(typeof(Area), nameof(Area.label));
```

Fields can be **buffered** (`SetBufferChanges(true)`), accumulating changes until the player stops interacting, then syncing the final value. This is used for sliders and text inputs.

### SyncMethod

Intercepts a method call on a target type. The target instance and all method arguments are serialized and sent as a command.

```csharp
// Attribute-based registration
[SyncMethod]
public void SetAreaAllowed(Area area, bool allowed) { ... }

// Programmatic registration
Sync.Method(typeof(Pawn_TimetableTracker), nameof(Pawn_TimetableTracker.SetAssignment));
```

### SyncDelegate

A specialized SyncMethod for compiler-generated delegate (lambda/closure) types. Captures the delegate's closed-over fields.

```csharp
// Register the inner method of a lambda
Sync.RegisterSyncDelegate(
    typeof(SomeClass),
    "<>c__DisplayClass12_0",   // Compiler-generated closure type
    "<DoSomething>b__0"        // Compiler-generated method name
);
```

Key properties:
- `fieldPaths` / `fieldTypes` — which closure fields to serialize
- `cancelIfNull` — field paths that cancel execution if null on deserialization
- `DelegateThis` constant (`"<>4__this"`) — refers to the captured `this` reference

### SyncAction

Generalized handler for complex multi-object syncing. Given a function that enumerates targets and an action getter, it syncs the action for each target.

```csharp
public class SyncAction<T, A, B, C> : SyncHandler, ISyncAction
{
    private Func<A, B, C, IEnumerable<T>> func;        // Enumerate targets
    private ActionGetter<T> actionGetter;                // Get action for each target
    private ActionWrapper<T, A, B, C> actionWrapper;     // Optional wrapper
}
```

## Registration

### Attribute-Based

Decorate methods with `[SyncMethod]` and the mod automatically registers them during `Sync.RegisterAllAttributes()`:

```csharp
[SyncMethod]
public static void DoSomething(Pawn pawn, int value) { ... }
```

### Programmatic

Register explicitly in `SyncGame.Init()` (`Source/Client/Syncing/Game/`):

```csharp
// SyncMethods.cs
Sync.Method(typeof(Building_Bed), nameof(Building_Bed.Medical_Set));

// SyncDelegates.cs
Sync.RegisterSyncDelegate(typeof(FoodRestrictionDatabase), ...);

// SyncFields.cs (in SyncFieldUtil)
Sync.Field(typeof(Pawn_PlayerSettings), nameof(Pawn_PlayerSettings.AreaRestriction));
```

## Handler Lifecycle

```
Registration Phase
├── SyncGame.Init()                 Programmatic registrations
├── Sync.RegisterAllAttributes()    Scan assemblies for [SyncMethod] etc.
│
Finalization Phase (Sync.ValidateAll)
├── PostInitHandlers()              Sort handlers, assign syncId to each
├── ValidateAll()                   Check all handlers are properly configured
└── HandlerHash                     CRC32 of all handler definitions
                                    (must match across all clients)
```

Each handler gets a unique `syncId` (integer index). This ID is sent in network packets to identify which handler to invoke on the receiving side.

The **HandlerHash** is a CRC32 computed over all handler definitions. During the join handshake, clients compare HandlerHashes to ensure they have the same sync handlers in the same order. A mismatch means the mod versions are incompatible.

## Command Flow

```
Player clicks button
        │
        ▼
Harmony prefix intercepts the method call
        │
        ▼
Check Multiplayer.ShouldSync ──── false ──▶ Execute normally
        │ true
        ▼
Serialize: syncId + target + arguments
        │
        ▼
Send to server (Client_Command packet)
        │
        ▼
Server broadcasts to all clients (Server_Command)
        │
        ▼
Command queued in ITickable.Cmds at target tick
        │
        ▼
TickPatch.RunCmds() dequeues at correct tick
        │
        ▼
SyncUtil.HandleCmd(data)
        │
        ▼
Deserialize: look up handler by syncId
        │
        ▼
Reconstruct target + arguments via SyncWorkers
        │
        ▼
Invoke the original method
```

## How to Add a New Sync Handler

### Syncing a Method

1. Find the method you want to sync (usually a UI action that modifies game state).
2. Add `[SyncMethod]` to the method, or register it programmatically:

```csharp
// In SyncMethods.cs or SyncDelegates.cs
Sync.Method(typeof(MyClass), nameof(MyClass.MyMethod));
```

3. Verify the method's arguments are all serializable. If any argument type lacks a SyncWorker, add one (see [Serialization](serialization)).

### Syncing a Field

```csharp
// In SyncFieldUtil
Sync.Field(typeof(MyClass), nameof(MyClass.myField))
    .SetBufferChanges(true);  // Optional: buffer slider/text changes
```

### Syncing a Delegate

```csharp
// Find the compiler-generated type name via ILSpy/dnSpy
Sync.RegisterSyncDelegate(
    typeof(OuterClass),
    "<>c__DisplayClass5_0",
    "<MethodName>b__1"
).CancelIfAnyFieldNull();
```

## Common Pitfalls

- **HandlerHash mismatch**: Adding or removing a sync handler changes the hash. All clients must have the same mod version.
- **Context flags**: Always check if code runs while `Multiplayer.ShouldSync` is true. Actions during ticking or command execution must not be re-synced.
- **Buffered fields**: Fields using `SetBufferChanges(true)` don't sync immediately. Call `SyncFieldUtil.ClearAllBufferedChanges()` when appropriate.
- **Serialization**: Every type used as a target or argument must have a registered SyncWorker. Missing serializers cause runtime errors.
- **Determinism**: Synced methods must produce the same result on all clients given the same inputs. Avoid `DateTime.Now`, `System.Random`, dictionary iteration order, etc.

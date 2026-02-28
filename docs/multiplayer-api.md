# Multiplayer API

The Multiplayer API (`RimWorld.MultiplayerAPI` NuGet package, v0.6.0) is the public interface for third-party mods to integrate with RimWorld Multiplayer. It allows other mods to sync their custom actions, fields, and UI without depending on the internal mod code.

## Getting Started

Add the NuGet package to your mod's project:

```xml
<PackageReference Include="RimWorld.MultiplayerAPI" Version="0.6.0" />
```

Access the API at runtime through the static `MP` class:

```csharp
using Multiplayer.API;

if (MP.IsInMultiplayer)
{
    // Multiplayer-specific logic
}
```

The API is only available when the Multiplayer mod is active. Always null-check or gate on `MP.IsInMultiplayer`.

## Core Properties

| Property | Type | Description |
|----------|------|-------------|
| `MP.IsInMultiplayer` | `bool` | Whether a multiplayer session is active |
| `MP.IsHosting` | `bool` | Whether this client is hosting the server |
| `MP.PlayerName` | `string` | Current player's username |
| `MP.IsExecutingSyncCommand` | `bool` | True when inside a synced command execution |
| `MP.IsExecutingSyncCommandIssuedBySelf` | `bool` | True when executing a command this player issued |
| `MP.CanUseDevMode` | `bool` | Whether the current player has dev mode permission |
| `MP.InInterface` | `bool` | True during GUI/interface code (not simulation) |
| `MP.RealPlayerFaction` | `Faction` | The actual faction of the local player |

## Attribute-Based Registration

The simplest approach — decorate your types and call `RegisterAll` at mod startup:

```csharp
// In your mod's initialization
MP.RegisterAll(Assembly.GetExecutingAssembly());
```

### `[SyncMethod]`

Syncs a method call across all players. When any player calls it, the call is replayed on every client deterministically.

```csharp
[SyncMethod]
public void DoAction(int param)
{
    // Executed on all clients
}
```

Parameters: `syncContext` (SyncContext flags), `cancelIfAnyArgNull`, `debugOnly`, `exposeParameters` (int array of exposed parameter indices).

### `[SyncField]`

Syncs a field value change. Typically used with `WatchBegin()`/`WatchEnd()` for UI-driven changes.

```csharp
[SyncField]
public bool optionEnabled;
```

Parameters: `bufferChanges`, `inGameLoop`, `cancelIfValueNull`, `version`.

### `[SyncWorker]`

Defines custom serialization for a type:

```csharp
[SyncWorker]
public static void SyncMyThing(SyncWorker sync, ref MyThing thing)
{
    if (sync.isWriting)
        sync.Write(thing.id);
    else
        thing = MyThing.FindById(sync.Read<int>());
}
```

Parameters: `isImplicit` (applies to subtypes), `shouldConstruct` (construct instance before calling worker).

### `[SyncDialogNodeTree]`

Marks a method whose dialog node tree should be synced.

### `[PauseLock]`

Registers a delegate that can prevent the game from being paused:

```csharp
[PauseLock]
public static bool MyPauseLock(Map map) => someCondition;
```

## Programmatic Registration

For more control, register sync handlers manually instead of using attributes.

### Fields

```csharp
ISyncField field = MP.RegisterSyncField(typeof(MyClass), "myField");
field.SetBufferChanges();  // Buffer changes until tick
field.SetVersion(2);       // Version for compatibility
```

### Methods

```csharp
ISyncMethod method = MP.RegisterSyncMethod(typeof(MyClass), "MyMethod");
method.SetContext(SyncContext.MapSelected);
method.CancelIfAnyArgNull();
method.TransformArgument(0, Serializer.New<Thing, int>(t => t.thingIDNumber, Find.World.GetThingById));
```

### Lambda and Local Function Variants

```csharp
// Sync a lambda inside a parent method (by ordinal position)
MP.RegisterSyncMethodLambda(typeof(MyClass), "ParentMethod", lambdaOrdinal: 0);
MP.RegisterSyncMethodLambdaInGetter(typeof(MyClass), "PropertyName", lambdaOrdinal: 0);

// Sync a delegate (closure)
MP.RegisterSyncDelegate(typeof(MyClass), "NestedType", "MethodName");
MP.RegisterSyncDelegateLambda(typeof(MyClass), "ParentMethod", lambdaOrdinal: 0);
MP.RegisterSyncDelegateLocalFunc(typeof(MyClass), "ParentMethod", "LocalFuncName");
```

### Delegates (Closures)

```csharp
ISyncDelegate del = MP.RegisterSyncDelegate(typeof(MyClass), "<>c__DisplayClass", "Method", new[] { "capturedField" });
del.CancelIfAnyFieldNull();
del.TransformField("fieldName", Serializer.New<Thing, int>(t => t.thingIDNumber, Find.World.GetThingById));
```

### SyncWorkers

```csharp
MP.RegisterSyncWorker<MyThing>(SyncMyThing, isImplicit: false, shouldConstruct: false);
```

### Dialog Node Trees

```csharp
MP.RegisterDialogNodeTree(typeof(MyClass), "ShowDialog");
MP.RegisterDialogNodeTree(methodInfo);
```

### Pause Locks

```csharp
MP.RegisterPauseLock(map => someCondition);
```

## Field Watching

SyncField changes are tracked using a watch pattern — bracket UI code that modifies synced fields with `WatchBegin()` / `WatchEnd()`:

```csharp
MP.WatchBegin();
// UI code that may change synced fields
Widgets.CheckboxLabeled(rect, "Option", ref myObj.optionEnabled);
MP.WatchEnd();
```

When a watched field changes during the watch window, the change is captured and sent to all clients.

## Custom Serialization

For types that the sync system doesn't know how to serialize, register a `SyncWorker`:

```csharp
MP.RegisterSyncWorker<MyThing>((SyncWorker sync, ref MyThing thing) =>
{
    if (sync.isWriting)
    {
        sync.Write(thing.id);
    }
    else
    {
        int id = sync.Read<int>();
        thing = MyThingDatabase.Get(id);
    }
});
```

The `SyncWorker` class provides:

- `Bind(ref T value)` — Read or write a value depending on direction
- `Read<T>()` / `Write<T>(T value)` — Explicit read/write
- `isWriting` — Whether this is a serialize (true) or deserialize (false) pass

Types can also implement `ISynchronizable` directly:

```csharp
public class MyThing : ISynchronizable
{
    public void Sync(SyncWorker sync)
    {
        sync.Bind(ref id);
        sync.Bind(ref name);
    }
}
```

## Session Management

Access multiplayer session managers for global or per-map sessions:

```csharp
// Global (world-level) sessions
ISessionManager global = MP.GetGlobalSessionManager();

// Per-map sessions
ISessionManager local = MP.GetLocalSessionManager(map);
```

## Player Info

Query connected players:

```csharp
IReadOnlyList<IPlayerInfo> players = MP.GetPlayers();
IPlayerInfo player = MP.GetPlayerById(playerId);
```

`IPlayerInfo` provides player metadata (name, id, faction, etc.).

## Utility Methods

The API provides additional utility methods for advanced integration:

### Thing Lookup

```csharp
// Look up a Thing by its multiplayer-assigned ID
Thing thing = MP.GetThingById(int id);

// Try-pattern variant (returns false if not found)
if (MP.TryGetThingById(id, out Thing thing)) { ... }
```

### Session Context

```csharp
// Set the active session context for transferable sync (e.g., trade dialogs)
MP.SetCurrentSessionWithTransferables(ISessionWithTransferables session);

// Set context for ThingFilter sync (e.g., storage/bill filters)
MP.SetThingFilterContext(ThingFilterContext context);
```

### Letter Auto-Dismiss

```csharp
// Register a handler to auto-dismiss expired letters (e.g., timed quest offers)
MP.RegisterDefaultLetterChoice(MethodInfo method, Type letterType = null);
```

## Config Sync Exclusions

By default, the multiplayer mod synchronizes mod config files between host and joining clients to ensure determinism. Some mods should be excluded from this — for example, mods that store user-specific data (usernames, API keys), mods with purely cosmetic settings, or mods that handle their own settings sync.

### About.xml Declaration

The simplest way to exclude your mod from config sync is to add an element to your mod's `About/About.xml`:

```xml
<ModMetaData>
    <name>My Mod</name>
    <packageId>author.mymod</packageId>
    <!-- ... other fields ... -->
    <mpDoNotSyncConfig>true</mpDoNotSyncConfig>
</ModMetaData>
```

This is scanned at startup and requires no code changes.

### Programmatic Registration

You can also exclude a mod from config sync at runtime:

```csharp
JoinData.AddIgnoredConfigMod("author.mymod");
```

The package ID is case-insensitive. This is useful when a mod needs to decide at runtime whether its config should be synced.

## Session Lifecycle

Sessions represent ongoing multiplayer interactions like trading, caravan forming, or rituals. The mod provides several base classes:

| Base Class | Persistence | Use Case |
|------------|-------------|----------|
| `Session` | Saved with game | Long-running sessions that survive save/load (e.g., trade dialogs) |
| `SemiPersistentSession` | Not saved | Temporary sessions that are lost on save/load |
| `ITickingSession` | Interface | Add to either session type to receive `Tick()` calls each game tick |

Session constructors must be parameterless (for deserialization). Use `PostAddSession()` for initialization that requires arguments. Sessions are managed by `SessionManager` — one global (world-level) and one per map. Access them via `MP.GetGlobalSessionManager()` and `MP.GetLocalSessionManager(map)`.

## SyncContext Values

`SyncContext` flags control what additional context is captured and restored when a synced method executes on remote clients:

| Flag | Effect |
|------|--------|
| `None` | No additional context (default) |
| `CurrentMap` | Captures and restores `Find.CurrentMap` |
| `MapMouseCell` | Captures mouse cell position; implies `CurrentMap` |
| `MapSelected` | Captures selected objects on the map |
| `WorldSelected` | Captures selected world objects |
| `QueueOrder_Down` | Captures whether the queue-order key (Shift) is held |

Use `SetContext()` when your synced method depends on UI state that won't be available on the receiving client:

```csharp
MP.RegisterSyncMethod(typeof(MyClass), "DoAction")
    .SetContext(SyncContext.MapSelected);
```

## Lambda Ordinals and Game Updates

When syncing lambdas with `RegisterSyncMethodLambda` or `RegisterSyncDelegateLambda`, the `lambdaOrdinal` parameter identifies which lambda in the parent method to target. This is the zero-based index of the lambda in the compiler-generated closure classes.

**Fragility warning:** Lambda ordinals can change when RimWorld updates if Ludeon adds, removes, or reorders lambdas in the parent method. When this happens, your sync handler will target the wrong code or fail to find the lambda entirely.

Best practices:
- Use `SetVersion(n)` to mark handlers that have been updated for a game version change. This prevents protocol mismatches between players on different mod versions.
- Prefer `RegisterSyncDelegateLocalFunc` (local functions with stable names) over lambda ordinals when possible.
- After a RimWorld update, verify lambda ordinals by inspecting the decompiled game code (e.g., with ILSpy/dnSpy).

## HandlerHash and Protocol Compatibility

When a client connects, the server compares sync handler hashes (`Sync.HandlerHash`) to verify both sides have identical sync handler registrations. If they differ, the connection is rejected with a "Sync handler hash mismatch" error.

The handler hash changes when:
- Sync handlers are added, removed, or reordered
- Handler types or parameters change
- The mod list differs (different mods register different handlers)

All players in a session must run identical mod lists with identical versions to ensure matching handler hashes.

## Implementation

The API is implemented by `MultiplayerAPIBridge` (`Source/Client/MultiplayerAPIBridge.cs`), which delegates to the internal sync system. The bridge is discovered by the API assembly at runtime via the type name `Multiplayer.Common.MultiplayerAPIBridge`.

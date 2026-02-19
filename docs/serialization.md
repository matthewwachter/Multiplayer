# Serialization

The serialization system converts game objects to and from binary data for network transmission. It centers on **SyncWorkers** — type-specific handlers that know how to write an object to a `ByteWriter` and reconstruct it from a `ByteReader`.

## SyncWorkerDictionaryTree

The `SyncWorkerDictionaryTree` (`Source/Common/Syncing/Worker/SyncWorkerDictionaryTree.cs`) stores all registered SyncWorkers in a three-tier lookup:

```
1. Explicit entries   (Dictionary<Type, SyncWorkerEntry>)  — exact type match
2. Implicit entries   (List<SyncWorkerEntry> with tree)    — type hierarchy match
3. Interface entries  (List<SyncWorkerEntry>)              — interface match
```

When resolving a type, the tree checks **explicit first**, then **implicit** (walking the type hierarchy), then **interface** as a last resort. This prioritization ensures exact matches are fastest while still supporting polymorphism.

## SyncWorkerEntry

Each `SyncWorkerEntry` (`Source/Common/Syncing/Worker/SyncWorkerEntry.cs`) represents a type with:

- A list of `SyncWorkerDelegate` handlers
- Optional parent/child relationships for implicit entries
- A `shouldConstruct` flag

### Handler Chains

For implicit entries, handlers form an inheritance chain. When invoking, the **parent handler executes first**, then children:

```csharp
public bool Invoke(SyncWorker worker, ref object? obj)
{
    parent?.Invoke(worker, ref obj);  // Parent first

    for (int i = 0; i < syncWorkers.Count; i++)
    {
        if (syncWorkers[i](worker, ref obj))
            return true;  // Stop chain
    }
    return false;
}
```

For example, syncing a `Designator_Paint`:
1. Base `Designator` handler runs (empty, does nothing)
2. `Designator_Place` handler runs (syncs `placingRot`)
3. `Designator_Paint` handler runs (syncs `colorDef`)

### shouldConstruct

When `true`, the system calls `Activator.CreateInstance(type)` before invoking handlers, providing a fresh object for handlers to populate. Value types are always constructed. When `false`, the handler is responsible for creating or locating the object.

## Writing a SyncWorker

### Pattern A: Writer/Reader Functions

Used in the SyncDict files. Provide separate write and read functions:

```csharp
{
    (ByteWriter data, MyClass obj) =>
    {
        data.WriteInt32(obj.id);
        WriteSync(data, obj.name);
    },
    (ByteReader data) =>
    {
        int id = data.ReadInt32();
        string name = ReadSync<string>(data);
        return MyClass.FindById(id);
    }
}
```

### Pattern B: SyncWorker Delegate

Used for attribute-based or API registration. A single bidirectional method checks `isWriting`:

```csharp
(SyncWorker sync, ref MyClass obj) =>
{
    if (sync.isWriting)
    {
        sync.Write(obj.id);
        sync.Write(obj.name);
    }
    else
    {
        int id = sync.Read<int>();
        string name = sync.Read<string>();
        obj = MyClass.FindById(id);
    }
}
```

### ByteReader/ByteWriter API

```csharp
// Writing
data.WriteByte(b);
data.WriteInt32(i);
data.WriteFloat(f);
data.WriteString(s);           // UTF-8 with int32 length prefix
data.WritePrefixedBytes(bytes);
data.WriteBool(b);

// Reading
byte b = data.ReadByte();
int i = data.ReadInt32();
float f = data.ReadFloat();
string s = data.ReadStringNullable();
byte[] bytes = data.ReadPrefixedBytes();
bool b = data.ReadBool();
```

## Dict Organization

SyncWorkers are organized across multiple files merged at startup:

| File | Contents |
|------|----------|
| `SyncDictCore.cs` | Defs, Things, ThingComps, Maps — fundamental game objects |
| `SyncDictPawns.cs` | Pawn sub-components: jobs, health, needs, skills, apparel |
| `SyncDictBuildings.cs` | Building-specific components, Areas, Zones |
| `SyncDictWorld.cs` | WorldObjects, Lords, Caravans, Quests |
| `SyncDictUI.cs` | ITabs, Commands, Designators — UI state objects |
| `SyncDictDlc.cs` | Royalty/Ideology DLC-specific types |
| `SyncDictMultiplayer.cs` | Multiplayer-mod-specific objects: sessions, transferables |
| `SyncDictMisc.cs` | Primitives, Unity types (Vector, Color), Ranges, Names |

### Initialization

```csharp
// SyncDict.cs
public static void Init()
{
    syncWorkers = SyncWorkerDictionaryTree.Merge(
        SyncDictMisc.syncWorkers,
        SyncDictRimWorld.syncWorkers,     // Merges Core+Pawns+World+UI+Buildings
        SyncDictDlc.syncWorkers,
        SyncDictMultiplayer.syncWorkers
    );
    Multiplayer.serialization.syncTree = syncWorkers;
}
```

## SyncSerialization Entry Point

`SyncSerialization` (`Source/Common/Syncing/SyncSerialization.cs`) is the main serialization entry point. `ReadSyncObject` and `WriteSyncObject` follow this resolution order:

1. Null check
2. Primitives (`bool`, `int`, `float`, `string`, etc.)
3. Enums (serialized as underlying type)
4. Arrays
5. Generic types (`List<T>`, `Nullable<T>`, `Dictionary<K,V>`, `HashSet<T>`, `ValueTuple<>`)
6. Serialization hooks (plugin extension points)
7. `ISynchronizable` implementations
8. **SyncWorkerDictionaryTree** lookup — where registered SyncWorkers execute

## How to Add a New Type Serializer

1. **Identify the category**: Is it a RimWorld type, a multiplayer type, or a utility type? Choose the appropriate SyncDict file.

2. **Determine explicit vs. implicit**:
   - **Explicit** (default): One concrete type, no subclasses need polymorphic handling.
   - **Implicit**: A base class with subclasses that should inherit serialization behavior.

3. **Determine shouldConstruct**: Set to `true` if the deserializer needs a fresh instance created before handlers run. Set to `false` (default) if the handler creates or locates the object itself.

4. **Write the handler**:

```csharp
// In the appropriate BuildXxxWorkers() method
{
    (ByteWriter data, MyNewType obj) =>
    {
        data.WriteInt32(obj.uniqueId);
    },
    (ByteReader data) =>
    {
        int id = data.ReadInt32();
        return MyNewType.FindById(id);
    }
    // Add 'true' as third parameter for implicit
    // Add 'true, true' for implicit + shouldConstruct
}
```

5. **Test**: Run `dotnet test Source/ --no-restore` to verify snapshot tests still pass. The HandlerHash may change, requiring updated test snapshots.

## Explicit vs. Implicit: Summary

| Aspect | Explicit | Implicit |
|--------|----------|----------|
| Type matching | Exact type only | Type hierarchy (assignable) |
| Handler chain | Single handler | Parent-to-child inheritance |
| Polymorphism | Not supported | Supported via tree |
| Use case | Concrete final types | Base classes, abstract types |
| Multiple handlers | Warning if > 1 | Expected (one per level) |

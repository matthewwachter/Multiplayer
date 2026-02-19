# Phase 2 Audit - Remaining Issues (2026-02-18)

## Summary

After Phase 1 fixes (ByteReader bounds checking, ActionQueue isolation, SyncDictRimWorld fixes, TypeCache deterministic ordering, protocol bump), this audit identified remaining issues across 5 categories.

---

## HIGH PRIORITY - Desync Risks

### H1. TickPatch.TickTickable Exception Swallowing (Desync)
**File**: `Source/Client/Patches/TickPatch.cs:250-258`
**Issue**: When `tickable.Tick()` throws an exception, it is logged but the tick continues. This means one client may have a map in a partially-ticked state while another processes the same tick cleanly (or fails at a different point). The divergent state causes inevitable desyncs.
**Fix**: Consider halting the tick loop and triggering a desync/rejoin rather than continuing with corrupted state. At minimum, record the exception in the sync opinion so both clients detect the divergence.

### H2. SyncCoordinator.TryAddInfoForDesyncLog Uses Non-Deterministic string.GetHashCode()
**File**: `Source/Client/Desyncs/SyncCoordinator.cs:203`
**Code**: `int hash = Gen.HashCombineInt(info1.GetHashCode(), info2.GetHashCode());`
**Issue**: `string.GetHashCode()` is non-deterministic across processes on .NET Core / .NET 5+ (hash randomization). While RimWorld currently targets .NET Framework 4.8 (where string hashing is deterministic per-process), this is a latent risk if the runtime ever changes. This hash feeds into `desyncStackTraceHashes` which is used for desync comparison. If hashes differ between clients due to runtime differences, desync detection will produce false positives or miss real desyncs.
**Fix**: Use `GenText.StableStringHash()` instead of `string.GetHashCode()`.

### H3. Dictionary<,> Serialization Iteration Order
**File**: `Source/Common/Syncing/SyncSerialization.cs:419-440`
**Issue**: When writing `Dictionary<K,V>`, the code calls `dictionary.Keys.CopyTo()` and `dictionary.Values.CopyTo()`. Dictionary enumeration order in .NET is not guaranteed across different runtime implementations or even different insertions orders. If two clients serialize the same dictionary with keys inserted in different orders (which can happen during game load or mod initialization), the byte representation differs.
**Context**: Phase 1 fixed the `TypeCache` ordering. This issue affects any `Dictionary<K,V>` that goes through the generic sync serialization path. The risk is highest for dictionaries with non-primitive keys where insertion order may vary. For `int`-keyed dictionaries used by `playerData`, this is likely safe in practice since insertion order is consistent, but it is architecturally fragile.
**Fix**: Sort keys before serialization when the key type implements `IComparable`.

### H4. ServerLoadingState.SendWorldData Dictionary Iteration
**File**: `Source/Common/Networking/State/ServerLoadingState.cs:38,55`
**Issue**: `Server.worldData.mapCmds` and `Server.worldData.mapData` are `Dictionary<int, ...>` iterated with `foreach`. While `int` keys have deterministic ordering for a given insertion sequence, if maps are created/removed in different orders across sessions, the iteration order could theoretically differ between a fresh server and one that has been running. This is lower risk since it only affects the join data sent to clients, but could cause confusing join failures.
**Fix**: Sort by key before iterating: `foreach (var kv in Server.worldData.mapCmds.OrderBy(x => x.Key))`.

### H5. SyncWorkerEntry.Invoke Has No Exception Recovery
**File**: `Source/Common/Syncing/Worker/SyncWorkerEntry.cs:75-91`
**Issue**: If a sync worker throws an exception during `Invoke()`, the ByteReader/ByteWriter stream position is in an indeterminate state. The parent caller has no way to know how many bytes were consumed/written. This corrupts the entire sync packet, causing all subsequent reads to be misaligned. On the reading side this causes crashes or silent data corruption; on both sides it can cause desyncs.
**Fix**: Wrap individual sync worker invocations in try-catch, and on exception either rethrow with context or reset the stream position. Consider recording the stream position before invoking and restoring on failure.

---

## HIGH PRIORITY - Crash/Exception Risks

### H6. Remaining Comp Type Array Accesses Without Bounds Checks
**Files & Lines**:
- `Source/Client/Syncing/Dict/SyncDictRimWorld.cs:357` - `abilityCompTypes[index]`
- `Source/Client/Syncing/Dict/SyncDictRimWorld.cs:961` - `mapCompTypes[index]`
- `Source/Client/Syncing/Dict/SyncDictRimWorld.cs:1005` - `worldObjectCompTypes[index]`
- `Source/Client/Syncing/Dict/SyncDictRimWorld.cs:1025` - `worldCompTypes[index]`
- `Source/Client/Syncing/Dict/SyncDictRimWorld.cs:1081` - `gameCompTypes[index]`
**Issue**: Phase 1 fixed `hediffCompTypes` and `thingCompTypes` bounds checks, but these 5 other comp type arrays still have unchecked index access. A malformed or mismatched packet could cause `IndexOutOfRangeException`.
**Fix**: Apply the same bounds-check pattern used in the Phase 1 fix for hediffCompTypes/thingCompTypes.

### H7. QuestPart Index No Bounds Check
**File**: `Source/Client/Syncing/Dict/SyncDictRimWorld.cs:507`
**Code**: `return quest.parts[index];`
**Issue**: No bounds check on `index` against `quest.parts.Count`. If the quest's parts list changed between write and read (e.g., quest state progressed), this throws `ArgumentOutOfRangeException`.
**Fix**: Add bounds check, return null if out of range with `Log.Error`.

### H8. PlanetLayer Reader No Bounds Check
**File**: `Source/Client/Syncing/Dict/SyncDictRimWorld.cs:1061`
**Code**: `return Find.WorldGrid.PlanetLayers[layerId];`
**Issue**: No bounds check on `layerId` against `PlanetLayers.Count`. Malformed data causes crash.
**Fix**: Add bounds check.

### H9. MechanitorControlGroup Reader No Bounds Check
**File**: `Source/Client/Syncing/Dict/SyncDictDlc.cs:274`
**Code**: `return mechanitor.controlGroups[index];`
**Issue**: No bounds check. If a mechanitor's control groups changed between write and read, this crashes.
**Fix**: Add bounds check.

### H10. ReadPrefixedUInts / ReadPrefixedULongs Missing Negative Length Validation
**File**: `Source/Common/ByteReader.cs:111-128`
**Issue**: `ReadPrefixedUInts()` and `ReadPrefixedULongs()` read a length with `ReadInt32()` but do not check for negative values. A negative length would create a 0-length or negative-length array (the latter throws). `ReadPrefixedInts()` at line 99 already validates `len < 0`.
**Fix**: Add negative length checks matching the pattern in `ReadPrefixedInts()`.

### H11. DelegateSerialization.ReadDelegate - AccessTools.Method Returns Null
**File**: `Source/Client/Syncing/DelegateSerialization.cs:96`
**Code**: `CheckMethodAllowed(AccessTools.Method(type, methodName))`
**Issue**: `AccessTools.Method(type, methodName)` can return null if the method doesn't exist (e.g., mod was unloaded/updated). `CheckMethodAllowed` would then throw `NullReferenceException` on `method.DeclaringType`. The null check and error message would be unhelpful.
**Fix**: Add null check for the method before calling `CheckMethodAllowed`.

---

## MEDIUM PRIORITY - Desync Risks

### M1. DelegateSerialization Method Name Ambiguity
**File**: `Source/Client/Syncing/DelegateSerialization.cs:22`
**Code**: `SyncSerialization.WriteSync(writer, del.Method.Name); // todo Handle the signature for ambiguous methods`
**Issue**: Only the method name is synced, not the full signature. If a type has overloaded methods, `AccessTools.Method(type, methodName)` picks the first one found, which may not be the correct overload. This is a known TODO.
**Risk**: Low probability in practice since most delegate targets are lambdas/compiler-generated, but could cause desyncs with mods that create delegates to overloaded methods.

### M2. SyncAction Hash Collision Risk
**File**: `Source/Client/Syncing/Handler/SyncAction.cs:99-108`
**Issue**: Actions are matched by `StableStringHash` of `Method.MethodDesc()` and `Target.GetType().FullDescription()`. Hash collisions would cause the wrong action to be invoked. `StableStringHash` is a 32-bit hash, so collision probability is low but non-zero.
**Risk**: Very unlikely in practice.

### M3. TickPatch.AllTickables Iteration Order
**File**: `Source/Client/Patches/TickPatch.cs:46-56`
**Issue**: `AllTickables` yields the world time first, then maps in reverse order. If `Find.Maps` order differs between clients (which shouldn't happen in normal gameplay but could in edge cases with mod-created maps), tick order would differ. The existing code iterates `Find.Maps` which is a managed list, so ordering should be consistent.
**Risk**: Very low, but worth noting.

### M4. MultiplayerGameComp.GetLowestTimeVote Uses Dictionary.Values
**File**: `Source/Client/Comp/Game/MultiplayerGameComp.cs:62`
**Code**: `playerData.Values.SelectMany(...).Min()`
**Issue**: `playerData.Values` iteration order is non-deterministic for Dictionary. However, since this computes a `Min()` over all values, the result is the same regardless of iteration order. This is safe.
**Status**: Not a real issue - Min() is order-independent.

---

## MEDIUM PRIORITY - Robustness

### M5. SyncDictDlc.cs Null Dereference Chains
**File**: `Source/Client/Syncing/Dict/SyncDictDlc.cs`
**Lines**: Multiple readers dereference without null checks:
- Line 28: `sync.Read<Pawn>().royalty` - if pawn is null
- Line 36: `sync.Read<RoyalTitlePermitDef>().Worker` - if def is null
- Line 135: `ReadSync<CompShuttle>(data).shipParent` - if comp is null
- Line 238: `pawn.genes.GetGene(geneDef)` - if pawn or genes is null
- Line 272: `ReadSync<Pawn>(data).mechanitor` - if pawn is null
**Fix**: Add null checks before dereference.

### M6. AsyncTimeComp.ExecuteCmd Exception Handling
**File**: `Source/Client/AsyncTime/AsyncTimeComp.cs:309`
**Issue**: The catch block catches all exceptions during command execution and logs them, but continues. The `UpdateManagers()` call at line 307 happens before the catch, so if the command itself throws, `UpdateManagers()` already ran. But the command's side effects may be partially applied, leading to inconsistent state.
**Note**: This is similar to H1 but for commands rather than ticks. The same concern about divergent state applies.

### M7. Fragmented Packet MemoryStream Buffer
**File**: `Source/Common/Networking/ConnectionBase.cs:245`
**Code**: `new ByteReader(fragPacket.Data.GetBuffer())`
**Issue**: `MemoryStream.GetBuffer()` returns the underlying buffer which may be larger than the data written. The ByteReader will have `Length` equal to the full buffer capacity, not the actual data length. This means the "packet was not fully consumed" check at line 167-168 could fail to detect issues, and reads past the actual data could return garbage bytes from the buffer padding.
**Fix**: Use `fragPacket.Data.ToArray()` instead, or construct ByteReader with explicit length.

---

## LOW PRIORITY

### L1. TradeableHashCode Prefix Skips Original Entirely
**File**: `Source/Client/Patches/HashCodes.cs:89`
**Code**: `static bool Prefix() => false;`
**Issue**: The comment says "todo does this cause issues?" - it replaces Tradeable's GetHashCode with RuntimeHelpers.GetHashCode, which is identity-based. This changes Dictionary behavior for any code using Tradeable as a dictionary key. While this fixes non-determinism, it could cause functional issues if game code relies on value equality of Tradeables.

### L2. Transpiler Pattern Match Failures Silent
**Files**: Multiple transpilers in `Source/Client/Patches/Determinism.cs`
**Issue**: Some transpilers (e.g., `DrawTrackerTickPatch`, `PawnCapacitiesHandlerGetLevelPatch`) do not log when they fail to find their target instructions. If a RimWorld update changes the IL, the patch silently does nothing and the non-deterministic behavior persists, causing desyncs. Some transpilers like `MapBrightnessLerpPatch` DO have validation logging.
**Fix**: Add validation logging to transpilers that lack it.

### L3. SyncFieldUtil.FieldWatchPostfix Exception Handling
**File**: `Source/Client/Syncing/SyncFieldUtil.cs:27`
**Code**: `// todo what happens on exceptions?`
**Issue**: If an exception occurs between `FieldWatchPrefix` (which pushes a null marker) and `FieldWatchPostfix`, the watchedStack will have entries from the failed invocation. The next `FieldWatchPostfix` call could pop entries belonging to a different caller, causing mismatched field sync.

### L4. PacketAwaitable.IsCompleted Incorrect for Null Results
**File**: `Source/Common/Networking/AsyncConnectionState.cs:170`
**Code**: `public bool IsCompleted => result != null;`
**Issue**: For `PacketOrNull` and `TypedPacketOrNull`, the result CAN be null (when the player disconnects). But `IsCompleted` checks `result != null`, which means it will never report as completed for null results. The `OnCompleted` continuation mechanism still works via `SetResult`, but anything polling `IsCompleted` would hang.

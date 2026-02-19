using Multiplayer.API;
using Multiplayer.Common;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Linq;
using Verse;
using Verse.AI;
using static Multiplayer.Client.CompSerialization;
using static Multiplayer.Client.SyncSerialization;

namespace Multiplayer.Client
{
    public static partial class SyncDictRimWorld
    {
        static SyncWorkerDictionaryTree BuildCoreWorkers() => new SyncWorkerDictionaryTree()
        {
            #region Defs
            {
                (ByteWriter data, Def def) =>
                {
                    if (def == null)
                    {
                        data.WriteUShort(ushort.MaxValue);
                        return;
                    }

                    var defTypeIndex = Array.IndexOf(DefSerialization.DefTypes, def.GetType());
                    if (defTypeIndex == -1)
                        throw new SerializationException($"Unknown def type {def.GetType()}");

                    data.WriteUShort((ushort)defTypeIndex);
                    data.WriteUShort(def.shortHash);
                },
                (ByteReader data) => {
                    ushort defTypeIndex = data.ReadUShort();
                    if (defTypeIndex == ushort.MaxValue)
                        return null;

                    ushort shortHash = data.ReadUShort();

                    var defType = DefSerialization.DefTypes[defTypeIndex];
                    var def = DefSerialization.GetDef(defType, shortHash);

                    if (def == null)
                        throw new SerializationException($"Couldn't find {defType} with short hash {shortHash}");

                    return def;
                },
                true // Implicit
            },
            #endregion

            #region Jobs
            {
                (ByteWriter data, WorkGiver workGiver) => {
                    WriteSync(data, workGiver.def);
                },
                (ByteReader data) => {
                    WorkGiverDef def = ReadSync<WorkGiverDef>(data);
                    return def?.Worker;
                }, true
            },
            {
                (ByteWriter data, BillStack obj) => {
                    Thing billGiver = obj?.billGiver as Thing;
                    WriteSync(data, billGiver);
                },
                (ByteReader data) => {
                    Thing thing = ReadSync<Thing>(data);
                    if (thing is IBillGiver billGiver)
                        return billGiver.BillStack;
                    return null;
                }
            },
            {
                (ByteWriter data, Bill bill) => {
                    WriteSync(data, bill.billStack);
                    data.WriteInt32(bill.loadID);
                },
                (ByteReader data) => {
                    BillStack billStack = ReadSync<BillStack>(data);

                    if (billStack == null)
                        return null;

                    int id = data.ReadInt32();

                    return billStack.Bills.Find(bill => bill.loadID == id);
                }, true
            },
            #endregion

            #region Things
            {
                (ByteWriter data, Thing thing) => {
                    if (thing == null)
                    {
                        data.WriteInt32(-1);
                        return;
                    }

                    MpContext context = data.MpContext();

                    if (thing.Spawned)
                        context.map = thing.Map;

                    data.WriteInt32(thing.thingIDNumber);

                    if (!context.syncingThingParent)
                    {
                        object holder = null;

                        if (thing.Spawned)
                            holder = thing.Map;
                        else if (thing.ParentHolder is ThingComp thingComp)
                            holder = thingComp;
                        else if (ThingOwnerUtility.GetFirstSpawnedParentThing(thing) is { } parentThing)
                            holder = parentThing;
                        else if (RwSerialization.GetAnyParent<WorldObject>(thing) is { } worldObj)
                            holder = worldObj;
                        else if (RwSerialization.GetAnyParent<WorldObjectComp>(thing) is { } worldObjComp)
                            holder = worldObjComp;

                        RwSerialization.GetImpl(holder, RwSerialization.supportedThingHolders, out Type implType, out int index);
                        if (index == -1)
                        {
                            data.WriteByte(byte.MaxValue);
                            Log.Error($"Thing {RwSerialization.ThingHolderString(thing)} is inaccessible");
                            return;
                        }

                        data.WriteByte((byte)index);

                        if (implType != typeof(Map))
                        {
                            context.syncingThingParent = true;
                            WriteSyncObject(data, holder, implType);
                            context.syncingThingParent = false;
                        }
                    }
                },
                (ByteReader data) => {
                    int thingId = data.ReadInt32();
                    if (thingId == -1)
                        return null;

                    var context = data.MpContext();

                    if (!context.syncingThingParent)
                    {
                        byte implIndex = data.ReadByte();
                        if (implIndex == byte.MaxValue)
                            return null;

                        Type implType = RwSerialization.supportedThingHolders[implIndex];

                        if (implType != typeof(Map))
                        {
                            context.syncingThingParent = true;
                            IThingHolder parent = (IThingHolder)ReadSyncObject(data, implType);
                            context.syncingThingParent = false;

                            if (parent != null)
                                return ThingOwnerUtility.GetAllThingsRecursively(parent).Find(t => t.thingIDNumber == thingId);
                            return null;
                        }
                    }

                    return Multiplayer.ThingsById.GetValueSafe(thingId);
                }, true
            },
            {
                (SyncWorker data, ref ThingComp comp) => {
                    if (data.isWriting) {
                        if (comp != null) {
                            ushort index = (ushort)Array.IndexOf(thingCompTypes, comp.GetType());
                            data.Write(index);
                            data.Write(comp.parent);
                            var tempComp = comp;
                            var compIndex = comp.parent.AllComps.Where(x => x.props.compClass == tempComp.props.compClass).FirstIndexOf(x => x == tempComp);
                            data.Write((ushort)compIndex);
                        } else {
                            data.Write(ushort.MaxValue);
                        }
                    } else {
                        ushort index = data.Read<ushort>();
                        if (index == ushort.MaxValue) {
                            return;
                        }
                        ThingWithComps parent = data.Read<ThingWithComps>();
                        if (parent == null) {
                            return;
                        }
                        Type compType = thingCompTypes[index];
                        var compIndex = data.Read<ushort>();
                        if (compIndex == 0)
                        {
                            comp = parent.AllComps.Find(c => c.props.compClass == compType);
                        }
                        else
                        {
                            var matching = parent.AllComps.Where(c => c.props.compClass == compType).ToList();
                            comp = compIndex < matching.Count ? matching[compIndex] : null;
                            if (comp == null)
                                Log.Error($"Multiplayer :: ThingComp index {compIndex} out of range (count={matching.Count})");
                        }
                    }
                }, true // implicit
            },
            {
                (SyncWorker sync, ref ThingDefCount thingDefCount) =>
                {
                    if (sync.isWriting)
                    {
                        sync.Write(thingDefCount.ThingDef);
                        sync.Write(thingDefCount.Count);
                    }
                    else
                    {
                        var def = sync.Read<ThingDef>();
                        var count = sync.Read<int>();

                        thingDefCount = new ThingDefCount(def, count);
                    }
                }
            },
            #endregion

            #region Databases

            { (SyncWorker data, ref OutfitDatabase db) => db = Current.Game.outfitDatabase },
            { (SyncWorker data, ref DrugPolicyDatabase db) => db = Current.Game.drugPolicyDatabase },
            { (SyncWorker data, ref FoodRestrictionDatabase db) => db = Current.Game.foodRestrictionDatabase },
            { (SyncWorker data, ref ReadingPolicyDatabase db) => db = Current.Game.readingPolicyDatabase },

            #endregion

            #region Maps
            {
                (ByteWriter data, Map map) => data.MpContext().map = map,
                (ByteReader data) => (data.MpContext().map)
            },
            {
                (ByteWriter data, AreaManager areas) => data.MpContext().map = areas.map,
                (ByteReader data) => (data.MpContext().map).areaManager
            },
            {
                (ByteWriter data, AutoSlaughterManager autoSlaughter) => data.MpContext().map = autoSlaughter.map,
                (ByteReader data) => (data.MpContext().map).autoSlaughterManager
            },
            {
                (ByteWriter data, MultiplayerMapComp comp) => data.MpContext().map = comp.map,
                (ByteReader data) => (data.MpContext().map).MpComp()
            },
            {
                (SyncWorker data, ref MapComponent comp) => {
                    if (data.isWriting) {
                        if (comp != null) {
                            ushort index = (ushort)Array.IndexOf(mapCompTypes, comp.GetType());
                            data.Write(index);
                            data.Write(comp.map);
                        } else {
                            data.Write(ushort.MaxValue);
                        }
                    } else {
                        ushort index = data.Read<ushort>();
                        if (index == ushort.MaxValue) {
                            return;
                        }
                        Map map = data.Read<Map>();
                        if (map == null) {
                            return;
                        }
                        Type compType = mapCompTypes[index];
                        comp = map.GetComponent(compType);
                    }
                }, true  // implicit
            },
            #endregion

            #region Game
            {
                (SyncWorker data, ref GameComponent comp) => {
                    if (data.isWriting) {
                        if (comp != null) {
                            ushort index = (ushort)Array.IndexOf(gameCompTypes, comp.GetType());
                            data.Write(index);
                        } else {
                            data.Write(ushort.MaxValue);
                        }
                    } else {
                        ushort index = data.Read<ushort>();
                        if (index == ushort.MaxValue) {
                            return;
                        }
                        Type compType = gameCompTypes[index];
                        comp = Current.Game.GetComponent(compType);
                    }
                }, true // implicit
            },
            {
                (ByteWriter _, ResearchManager _) => { },
                (ByteReader _) => Find.ResearchManager
            },
            #endregion

            #region Globals

            { (SyncWorker data, ref WorldSelector selector) => selector = Find.WorldSelector },
            { (SyncWorker data, ref Storyteller storyteller) => storyteller = Find.Storyteller },

            #endregion

            #region Targets
            {
                (ByteWriter data, LocalTargetInfo info) => {
                    data.WriteBool(info.HasThing);
                    if (info.HasThing)
                        WriteSync(data, info.Thing);
                    else
                        WriteSync(data, info.Cell);
                },
                (ByteReader data) => {
                    bool hasThing = data.ReadBool();
                    if (hasThing)
                        return new LocalTargetInfo(ReadSync<Thing>(data));
                    else
                        return new LocalTargetInfo(ReadSync<IntVec3>(data));
                }
            },
            {
                (ByteWriter data, TargetInfo info) => {
                    data.WriteBool(info.HasThing);
                    if (info.HasThing) {
                        WriteSync(data, info.Thing);
                    }
                    else {
                        WriteSync(data, info.Cell);
                        WriteSync(data, info.Map);
                    }
                },
                (ByteReader data) => {
                    bool hasThing = data.ReadBool();
                    if (hasThing)
                        return new TargetInfo(ReadSync<Thing>(data));
                    else
                        return new TargetInfo(ReadSync<IntVec3>(data), ReadSync<Map>(data), true); // True to prevent errors/warnings if synced map was null
                }
            },
            {
                (ByteWriter data, GlobalTargetInfo info) => {
                    if (info.HasThing) {
                        data.WriteByte(0);
                        WriteSync(data, info.Thing);
                    }
                    else if (info.Cell.IsValid) {
                        data.WriteByte(1);
                        WriteSync(data, info.Cell);
                        WriteSync(data, info.Map);
                    }
                    else if (info.HasWorldObject) {
                        data.WriteByte(2);
                        WriteSync(data, info.WorldObject);
                    }
                    else {
                        data.WriteByte(3);
                        WriteSync(data, info.Tile);
                    }
                },
                (ByteReader data) =>
                {
                    return data.ReadByte() switch
                    {
                        0 => new GlobalTargetInfo(ReadSync<Thing>(data)),
                        1 => new GlobalTargetInfo(ReadSync<IntVec3>(data), ReadSync<Map>(data),
                            true) // True to prevent errors/warnings if synced map was null
                        ,
                        2 => new GlobalTargetInfo(ReadSync<WorldObject>(data)),
                        3 => new GlobalTargetInfo(data.ReadInt32()),
                        _ => GlobalTargetInfo.Invalid
                    };
                }
            },
            #endregion
        };
    }
}

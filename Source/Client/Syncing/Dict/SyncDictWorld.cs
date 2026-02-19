using Multiplayer.API;
using Multiplayer.Common;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Linq;
using Verse;
using Verse.AI.Group;
using static Multiplayer.Client.CompSerialization;
using static Multiplayer.Client.SyncSerialization;

namespace Multiplayer.Client
{
    public static partial class SyncDictRimWorld
    {
        static SyncWorkerDictionaryTree BuildWorldWorkers() => new SyncWorkerDictionaryTree()
        {
            #region AI
            {
                (ByteWriter data, Lord lord) => {
                    if (lord == null) {
                        data.WriteInt32(int.MaxValue);
                    }
                    else {
                        data.WriteInt32(lord.loadID);
                        MpContext context = data.MpContext();
                        context.map = lord.Map;
                    }
                },
                (ByteReader data) => {
                    int lordId = data.ReadInt32();
                    if (lordId == int.MaxValue)
                        return null;
                    var map = data.MpContext().map;
                    return map.lordManager.lords.Find(l => l.loadID == lordId);
                }
            },
            {
                (ByteWriter data, LordJob job) => {
                    WriteSync(data, job?.lord);
                },
                (ByteReader data) => {
                    var lord = ReadSync<Lord>(data);
                    return lord?.LordJob;
                }, true // Implicit
            },
            {
                (ByteWriter data, LordToil toil) => {
                    WriteSync(data, toil?.lord);
                },
                (ByteReader data) => {
                    var lord = ReadSync<Lord>(data);
                    return lord?.curLordToil;
                }, true // Implicit
            },
            #endregion

            #region Caravans
            {
                (ByteWriter data, Caravan_PathFollower follower) => WriteSync(data, follower.caravan),
                (ByteReader data) => ReadSync<Caravan>(data)?.pather
            },
            {
                (ByteWriter data, WITab_Caravan_Gear tab) => {
                    data.WriteBool(tab.draggedItem != null);
                    if (tab.draggedItem != null) {
                        WriteSync(data, tab.draggedItem);
                    }},
                (ByteReader data) => {
                    bool hasThing = data.ReadBool();
                    Thing thing = null;
                    if (hasThing) {
                        thing = ReadSync<Thing>(data);
                        if (thing == null)
                            return null;
                    }
                    var tab = new WITab_Caravan_Gear{
                        draggedItem = thing
                    };
                    return tab;
                 }
            },
            {
                (ByteWriter data, PawnColumnWorker worker) => WriteSync(data, worker.def),
                (ByteReader data) => {
                    PawnColumnDef def = ReadSync<PawnColumnDef>(data);
                    return def.Worker;
                }, true
            },
            #endregion

            #region Quests
            {
                (ByteWriter data, Quest quest) => {
                    data.WriteInt32(quest.id);
                },
                (ByteReader data) => {
                    int questId = data.ReadInt32();
                    return Find.QuestManager.QuestsListForReading.FirstOrDefault(possibleQuest => possibleQuest.id == questId);
                },
                true
            },
            {
                (ByteWriter data, QuestPart part) => {
                    WriteSync(data, part.quest);
                    WriteSync(data, part.Index);
                },
                (ByteReader data) => {
                    var quest = ReadSync<Quest>(data);
                    int index = ReadSync<int>(data);

                    return quest.parts[index];
                },
                true
            },
            #endregion

            #region Factions
            {
                (ByteWriter data, Faction faction) => {
                    data.WriteInt32(faction?.loadID ?? -1);
                },
                (ByteReader data) => {
                    int loadID = data.ReadInt32();
                    return Find.FactionManager.AllFactions.FirstOrDefault(possibleFaction => possibleFaction.loadID == loadID);
                },
                true
            },
            #endregion

            #region World
            {
                (ByteWriter data, World world) => { },
                (ByteReader data) => Find.World
            },
            {
                (ByteWriter data, WorldObject worldObj) => {
                    data.WriteInt32(worldObj?.ID ?? -1);
                },
                (ByteReader data) => {
                    int objId = data.ReadInt32();
                    if (objId == -1)
                        return null;

                    return Find.World.worldObjects.AllWorldObjects.Find(w => w.ID == objId) ??
                           Find.World.pocketMaps.Find(p => p.ID == objId);
                }, true // Implicit
            },
            {
                (SyncWorker data, ref WorldObjectComp comp) => {
                    if (data.isWriting) {
                        if (comp != null) {
                            ushort index = (ushort)Array.IndexOf(worldObjectCompTypes, comp.GetType());
                            data.Write(index);
                            data.Write(comp.parent);
                        } else {
                            data.Write(ushort.MaxValue);
                        }
                    } else {
                        ushort index = data.Read<ushort>();
                        if (index == ushort.MaxValue) {
                            return;
                        }
                        WorldObject parent = data.Read<WorldObject>();
                        if (parent == null) {
                            return;
                        }
                        Type compType = worldObjectCompTypes[index];
                        comp = parent.GetComponent(compType);
                    }
                }, true // implicit
            },
            {
                (SyncWorker data, ref WorldComponent comp) => {
                    if (data.isWriting) {
                        if (comp != null) {
                            ushort index = (ushort)Array.IndexOf(worldCompTypes, comp.GetType());
                            data.Write(index);
                            data.Write(comp.world);
                        } else {
                            data.Write(ushort.MaxValue);
                        }
                    } else {
                        ushort index = data.Read<ushort>();
                        if (index == ushort.MaxValue) {
                            return;
                        }
                        Type compType = worldCompTypes[index];
                        World world = data.Read<World>();
                        if (world == null) {
                            return;
                        }
                        comp = world.GetComponent(compType);
                    }
                }, true // implicit
            },
            {
                (ByteWriter data, Caravan_ForageTracker tracker) => WriteSync(data, tracker?.caravan),
                (ByteReader data) => ReadSync<Caravan>(data)?.forage
            },
            {
                // Consider using int16 rather than int32 to minimize network traffic
                // if tiles/layers are small enough.
                (ByteWriter data, PlanetTile tile) =>
                {
                    data.WriteInt32(tile.tileId);
                    data.WriteInt32(tile.layerId);
                },
                (ByteReader data) => new PlanetTile(data.ReadInt32(), data.ReadInt32()), true // Implicit
            },
            {
                (ByteWriter data, PlanetLayer workGiver) =>
                {
                    int layerId = workGiver?.LayerID ?? -1;
                    WriteSync(data, layerId);
                },
                (ByteReader data) => {

                    var layerId = ReadSync<int>(data);

                    if(layerId == -1)
                        return null;

                    return Find.WorldGrid.PlanetLayers[layerId];
                }, true
            },
            {
                (ByteWriter data, Tile tile) =>
                {
                    var map = Find.Maps.Find(m => m.pocketTileInfo == tile);

                    // Handle pocket map
                    if (map != null)
                    {
                        data.WriteInt32(map.uniqueID);
                    }
                    // Handle normal tile
                    else
                    {
                        data.WriteInt32(-1);
                        WriteSync(data, tile.tile);
                    }
                },
                (ByteReader data) =>
                {
                    var pocketMapId = data.ReadInt32();

                    if (pocketMapId >= 0)
                        return Find.Maps.Find(m => m.uniqueID == pocketMapId)?.pocketTileInfo;
                    return ReadSync<PlanetTile>(data).Tile;
                }
            },
            #endregion

            #region Letters
            {
                (ByteWriter data, Letter letter) => {
                    WriteSync(data, letter.ID);
                },
                (ByteReader data) =>
                {
                    var id = data.ReadInt32();
                    return (Letter)Find.Archive.ArchivablesListForReading.Find(a => a is Letter l && l.ID == id);
                }, true
            },
            #endregion

            #region PassingShip
            {
                (ByteWriter data, PassingShip ship) =>
                {
                    WriteSync(data, ship.Map);
                    if (ship.Map != null) data.WriteInt32(ship.loadID);
                },
                (ByteReader data) =>
                {
                    var map = ReadSync<Map>(data);
                    if (map == null) return null;

                    var id = data.ReadInt32();
                    return map.passingShipManager.passingShips.FirstOrDefault(s => s.loadID == id);
                }, true // Implicit
            },
            #endregion
        };
    }
}

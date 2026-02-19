using Multiplayer.API;
using Multiplayer.Common;
using RimWorld;
using System;
using System.Linq;
using Verse;
using static Multiplayer.Client.CompSerialization;
using static Multiplayer.Client.SyncSerialization;

namespace Multiplayer.Client
{
    public static partial class SyncDictRimWorld
    {
        static SyncWorkerDictionaryTree BuildBuildingWorkers() => new SyncWorkerDictionaryTree()
        {
            #region ThingComps
            {
                (ByteWriter data, CompChangeableProjectile comp) => {
                    if (comp == null)
                    {
                        WriteSync<Thing>(data, null);
                        return;
                    }

                    CompEquippable compEquippable = comp.parent.TryGetComp<CompEquippable>();

                    if (compEquippable.AllVerbs.Any())
                    {
                        Building_TurretGun turretGun = compEquippable.AllVerbs.Select(v => v.caster).OfType<Building_TurretGun>().FirstOrDefault();
                        if (turretGun != null)
                        {
                            WriteSync<Thing>(data, turretGun);
                            return;
                        }
                    }

                    throw new SerializationException("Couldn't save CompChangeableProjectile for thing " + comp.parent);
                },
                (ByteReader data) => {
                    if (ReadSync<Thing>(data) is not Building_TurretGun parent)
                        return null;

                    return (parent.gun as ThingWithComps).TryGetComp<CompChangeableProjectile>();
                }
            },
            {
                (ByteWriter writer, ReadingOutcomeDoer doer) =>
                {
                    WriteSync(writer, doer.Readable);
                    writer.WriteInt32(doer.Readable.doers.IndexOf(doer));
                },
                (ByteReader reader) =>
                {
                    var parent = ReadSync<CompReadable>(reader);
                    var index = reader.ReadInt32();

                    // Make sure we have a valid doer
                    if (parent == null || index < 0 || index >= parent.doers.Count)
                        return null;

                    return parent.doers[index];
                }, true // implicit
            },
            #endregion

            #region Areas
            {
                (ByteWriter data, Area area) => {
                    if (area == null) {
                        data.WriteInt32(-1);
                    } else {
                        data.MpContext().map = area.Map;
                        data.WriteInt32(area.ID);
                    }
                },
                (ByteReader data) => {
                    int areaId = data.ReadInt32();
                    if (areaId == -1)
                        return null;
                    return data.MpContext().map.areaManager.AllAreas.Find(a => a.ID == areaId);
                }, true
            },
            {
                (ByteWriter data, Zone zone) => {
                    if (zone == null) {
                        data.WriteInt32(-1);
                    } else {
                        data.MpContext().map = zone.Map;
                        data.WriteInt32(zone.ID);
                    }

                },
                (ByteReader data) => {
                    int zoneId = data.ReadInt32();
                    if (zoneId == -1)
                        return null;
                    return data.MpContext().map.zoneManager.AllZones.Find(zone => zone.ID == zoneId);
                }, true
            },
            {
                (ByteWriter data, Room room) => {
                    if(room == null){
                        data.WriteInt32(-1);
                    } else {
                        data.MpContext().map = room.Map;
                        data.WriteInt32(room.ID);
                    }
                },
                (ByteReader data) => {
                    int roomId = data.ReadInt32();
                    if (roomId == -1)
                        return null;
                    return data.MpContext().map.regionGrid.allRooms.Find(r => r.ID == roomId);
                }, true
            },
            {
                (ByteWriter data, Plan plan) =>
                {
                    if (plan == null)
                    {
                        data.WriteInt32(-1);
                    }
                    else
                    {
                        data.MpContext().map = plan.Map;
                        data.WriteInt32(plan.ID);
                    }
                },
                (ByteReader data) => {
                    int zoneId = data.ReadInt32();
                    if (zoneId == -1)
                        return null;
                    return data.MpContext().map.planManager.AllPlans.Find(p => p.ID == zoneId);
                }, true
            },
            #endregion

            #region Storage
            {
                (ByteWriter data, SlotGroup obj) => {
                    WriteSync(data, obj.parent);
                },
                (ByteReader data) =>
                {
                    var parent = ReadSync<ISlotGroupParent>(data);
                    return parent.GetSlotGroup();
                }
            },
            {
                (ByteWriter data, StorageGroup obj) =>
                {
                    data.MpContext().map = obj.Map;
                    WriteSync(data, obj.loadID);
                },
                (ByteReader data) =>
                {
                    var loadId = data.ReadInt32();
                    return data.MpContext().map.storageGroups.groups.Find(g => g.loadID == loadId);
                }
            },

            {
                (ByteWriter data, StorageSettings storage) => {
                    WriteSync(data, storage.owner);
                },
                (ByteReader data) => {
                    IStoreSettingsParent parent = ReadSync<IStoreSettingsParent>(data);
                    return parent?.GetStoreSettings();
                }
            },
            #endregion
        };
    }
}

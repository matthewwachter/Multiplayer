using Multiplayer.API;
using Multiplayer.Common;
using RimWorld;
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
        static SyncWorkerDictionaryTree BuildPawnWorkers() => new SyncWorkerDictionaryTree()
        {
            #region Pawns
            {
                (ByteWriter data, PriorityWork work) => WriteSync(data, work.pawn),
                (ByteReader data) => ReadSync<Pawn>(data)?.mindState?.priorityWork
            },
            {
                (ByteWriter data, Pawn_PlayerSettings comp) => WriteSync(data, comp.pawn),
                (ByteReader data) => ReadSync<Pawn>(data)?.playerSettings
            },
            {
                (ByteWriter data, Pawn_TimetableTracker comp) => WriteSync(data, comp.pawn),
                (ByteReader data) => ReadSync<Pawn>(data)?.timetable
            },
            {
                (ByteWriter data, Pawn_DraftController comp) => WriteSync(data, comp.pawn),
                (ByteReader data) => ReadSync<Pawn>(data)?.drafter
            },
            {
                (ByteWriter data, Pawn_WorkSettings comp) => WriteSync(data, comp.pawn),
                (ByteReader data) => ReadSync<Pawn>(data)?.workSettings
            },
            {
                (ByteWriter data, Pawn_JobTracker comp) => WriteSync(data, comp.pawn),
                (ByteReader data) => ReadSync<Pawn>(data)?.jobs
            },
            {
                (ByteWriter data, Pawn_OutfitTracker comp) => WriteSync(data, comp.pawn),
                (ByteReader data) => ReadSync<Pawn>(data)?.outfits
            },
            {
                (ByteWriter data, Pawn_DrugPolicyTracker comp) => WriteSync(data, comp.pawn),
                (ByteReader data) => ReadSync<Pawn>(data)?.drugs
            },
            {
                (ByteWriter data, Pawn_FoodRestrictionTracker comp) => WriteSync(data, comp.pawn),
                (ByteReader data) => ReadSync<Pawn>(data)?.foodRestriction
            },
            {
                (ByteWriter data, Pawn_ReadingTracker comp) => WriteSync(data, comp.pawn),
                (ByteReader data) => ReadSync<Pawn>(data)?.reading
            },
            {
                (ByteWriter data, Pawn_TrainingTracker comp) => WriteSync(data, comp.pawn),
                (ByteReader data) => ReadSync<Pawn>(data)?.training
            },
            {
                (ByteWriter data, Pawn_StoryTracker comp) => WriteSync(data, comp.pawn),
                (ByteReader data) => ReadSync<Pawn>(data)?.story
            },
            {
                (ByteWriter data, OutfitForcedHandler comp) => WriteSync(data, comp.forcedAps.Select(a => a.Wearer).FirstOrDefault()),
                (ByteReader data) => ReadSync<Pawn>(data)?.outfits?.forcedHandler
            },
            {
                // We assume that the currently open tab holds the table, as it seems to only be used together with MainTabWindow_PawnTable and its subclasses
                (ByteWriter data, PawnTable table) => WriteSync(data, Find.MainTabsRoot.OpenTab),
                (ByteReader data) =>
                {
                    Rand.PushState();
                    try
                    {
                        var tab = (MainTabWindow_PawnTable)ReadSync<MainButtonDef>(data).TabWindow;
                        return tab.CreateTable();
                    }
                    finally
                    {
                        Rand.PopState();
                    }
                }, true
            },
            {
                (ByteWriter data, Pawn_InventoryStockTracker inventoryTracker) => WriteSync(data, inventoryTracker.pawn),
                (ByteReader data) => ReadSync<Pawn>(data)?.inventoryStock
            },
            {
                (ByteWriter data, Pawn_ConnectionsTracker connectionTracker) => WriteSync(data, connectionTracker.pawn),
                (ByteReader data) => ReadSync<Pawn>(data)?.connections
            },
            {
                (SyncWorker sync, ref Hediff hediff) =>
                {
                    if (sync.isWriting)
                    {
                        if (hediff != null)
                        {
                            sync.Write(hediff.loadID);
                            sync.Write(hediff.pawn);
                        }
                        else
                            sync.Write(int.MaxValue);
                    }
                    else
                    {
                        var id = sync.Read<int>();

                        if (id == int.MaxValue)
                            return;

                        var pawn = sync.Read<Pawn>();

                        if (pawn == null)
                        {
                            Log.Error($"Multiplayer :: SyncDictionary.Hediff: pawn is null");
                            return;
                        }

                        hediff = pawn.health.hediffSet.hediffs.FirstOrDefault(x => x.loadID == id);

                        if (hediff == null)
                        {
                            Log.Error($"Multiplayer :: SyncDictionary.Hediff: Unknown hediff {id}");
                        }
                    }
                }, true // implicit
            },
            {
                (SyncWorker data, ref HediffComp hediffComp) => {
                    if (data.isWriting) {
                        if (hediffComp != null) {
                            ushort index = (ushort)Array.IndexOf(hediffCompTypes, hediffComp.GetType());
                            data.Write(index);
                            data.Write(hediffComp.parent);
                            var tempComp = hediffComp;
                            var compIndex = hediffComp.parent.comps.Where(x => x.props.compClass == tempComp.props.compClass).FirstIndexOf(x => x == tempComp);
                            data.Write((ushort)compIndex);
                        } else {
                            data.Write(ushort.MaxValue);
                        }
                    } else {
                        ushort index = data.Read<ushort>();
                        if (index == ushort.MaxValue) {
                            return;
                        }
                        HediffWithComps parent = data.Read<HediffWithComps>();
                        if (parent == null) {
                            return;
                        }
                        Type compType = hediffCompTypes[index];
                        var compIndex = data.Read<ushort>();
                        if (compIndex == 0)
                        {
                            hediffComp = parent.comps.Find(c => c.props.compClass == compType);
                        }
                        else
                        {
                            var matching = parent.comps.Where(c => c.props.compClass == compType).ToList();
                            hediffComp = compIndex < matching.Count ? matching[compIndex] : null;
                            if (hediffComp == null)
                                Log.Error($"Multiplayer :: HediffComp index {compIndex} out of range (count={matching.Count})");
                        }
                    }
                }, true // implicit
            },
            {
                (ByteWriter data, Need need) =>
                {
                    WriteSync(data, need.pawn);
                    WriteSync(data, need.def);
                },
                (ByteReader data) =>
                {
                    var pawn = ReadSync<Pawn>(data);
                    return pawn?.needs.TryGetNeed(ReadSync<NeedDef>(data));
                }, true // implicit
            },
            {
                (ByteWriter data, Pawn_MindState mindState) => WriteSync(data, mindState.pawn),
                (ByteReader data) => ReadSync<Pawn>(data)?.mindState
            },
            {
                (ByteWriter data, Pawn_CreepJoinerTracker joinerTracker) => WriteSync(data, joinerTracker?.Pawn),
                (ByteReader data) => ReadSync<Pawn>(data)?.creepjoiner
            },
            {
                (ByteWriter data, Pawn_NeedsTracker joinerTracker) => WriteSync(data, joinerTracker?.pawn),
                (ByteReader data) => ReadSync<Pawn>(data)?.needs
            },
            {
                (ByteWriter data, Pawn_GuestTracker guestTracker) => WriteSync(data, guestTracker?.pawn),
                (ByteReader data) => ReadSync<Pawn>(data)?.guest
            },
            #endregion

            #region Policies
            {
                (ByteWriter data, ApparelPolicy policy) => {
                    data.WriteInt32(policy.id);
                },
                (ByteReader data) => {
                    int id = data.ReadInt32();
                    return Current.Game.outfitDatabase.AllOutfits.Find(o => o.id == id);
                }
            },
            {
                (ByteWriter data, DrugPolicy policy) => {
                    data.WriteInt32(policy.id);
                },
                (ByteReader data) => {
                    int id = data.ReadInt32();
                    return Current.Game.drugPolicyDatabase.AllPolicies.Find(o => o.id == id);
                }
            },
            {
                (ByteWriter data, FoodPolicy policy) => {
                    data.WriteInt32(policy.id);
                },
                (ByteReader data) => {
                    int id = data.ReadInt32();
                    return Current.Game.foodRestrictionDatabase.AllFoodRestrictions.Find(o => o.id == id);
                }
            },
            {
                (ByteWriter data, ReadingPolicy policy) => {
                    data.WriteInt32(policy.id);
                },
                (ByteReader data) => {
                    int id = data.ReadInt32();
                    return Current.Game.readingPolicyDatabase.AllReadingPolicies.Find(o => o.id == id);
                }
            },
            #endregion

            #region Abilities
            {
                (ByteWriter data, Ability ability) => {
                    WriteSync(data, ability.pawn);
                    WriteSync(data, ability.Id);
                },
                (ByteReader data) => {
                    var pawn = ReadSync<Pawn>(data);
                    var abilityId = data.ReadInt32();

                    // Note there exist temporary abilities which might get removed by the time this data is read
                    // The returned ability can be null
                    return pawn.abilities.allAbilitiesCached.Find(ab => ab.Id == abilityId);
                }, true
            },
            {
                (SyncWorker data, ref AbilityComp comp) => {
                    if (data.isWriting) {
                        if (comp != null) {
                            ushort index = (ushort)Array.IndexOf(abilityCompTypes, comp.GetType());
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
                        Ability parent = data.Read<Ability>();
                        if (parent == null) {
                            return;
                        }
                        Type compType = abilityCompTypes[index];
                        comp = parent.comps.Find(c => c.props.compClass == compType);
                    }
                }, true // implicit
            },
            #endregion

            #region Verb
            {
                (SyncWorker sync, ref Verb verb)  => {
                    if (sync.isWriting) {

                        sync.Write(verb.DirectOwner);
                        if (verb.DirectOwner != null)
                            sync.Write(verb.loadID);
                    }
                    else
                    {
                        var owner = sync.Read<IVerbOwner>();
                        if (owner == null)
                            return;
                        var loadID = sync.Read<string>();

                        verb = owner.VerbTracker.AllVerbs.Find(ve => ve.loadID == loadID);

                        if (verb == null) {
                            Log.Error($"Multiplayer :: SyncDictionary.Verb: Unknown verb {loadID}");
                        }
                    }
                }, true // implicit
            },
            #endregion

            #region Records
            {
                (ByteWriter data, BodyPartRecord part) => {
                    if (part == null) {
                        data.WriteUShort(ushort.MaxValue);
                        return;
                    }

                    BodyDef body = part.body;

                    data.WriteUShort((ushort)body.GetIndexOfPart(part));
                    WriteSync(data, body);
                },
                (ByteReader data) => {
                    ushort partIndex = data.ReadUShort();
                    if (partIndex == ushort.MaxValue) return null;

                    BodyDef body = ReadSync<BodyDef>(data);
                    return body.GetPartAtIndex(partIndex);
                }
            },
            #endregion
        };
    }
}

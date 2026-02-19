using Multiplayer.API;
using Multiplayer.Common;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using static Multiplayer.Client.SyncSerialization;

namespace Multiplayer.Client
{
    public static partial class SyncDictRimWorld
    {
        static SyncWorkerDictionaryTree BuildUIWorkers() => new SyncWorkerDictionaryTree()
        {
            #region Tabs
            {
                (ByteWriter data, ITab_Bills tab) => { },
                (ByteReader data) => new ITab_Bills()
            },
            {
                (ByteWriter data, ITab_Pawn_Gear tab) => { },
                (ByteReader data) => new ITab_Pawn_Gear()
            },
            {
                (ByteWriter data, ITab_ContentsBase tab) => WriteSync(data, tab.GetType()),
                (ByteReader data) => (ITab_ContentsBase)Activator.CreateInstance(ReadSync<Type>(data)),
                true // Implicit
            },
            {
                (ByteWriter data, ITab_Pawn_Guest tab) => { },
                (ByteReader data) => new ITab_Pawn_Guest()
            },
            {
                (ByteWriter data, ITab_Pawn_Prisoner tab) => { },
                (ByteReader data) => new ITab_Pawn_Prisoner()
            },
            {
                (ByteWriter data, ITab_Pawn_Slave tab) => { },
                (ByteReader data) => new ITab_Pawn_Slave()
            },
            {
                (ByteWriter data, ITab_Pawn_Visitor tab) => { },
                (ByteReader data) => new Dummy_ITab_Pawn_Visitor()
            },
            #endregion

            #region Commands
            {
                (ByteWriter data, Command_SetPlantToGrow command) => {
                    WriteSync(data, command.settable);
                    WriteSync(data, command.settables);
                },
                (ByteReader data) => {
                    var settable = ReadSync<IPlantToGrowSettable>(data);

                    if (settable == null)
                        return null;

                    var settables = ReadSync<List<IPlantToGrowSettable>>(data);
                    settables.RemoveAll(s => s == null);

                    var command = MpUtil.NewObjectNoCtor<Command_SetPlantToGrow>();
                    command.settable = settable;
                    command.settables = settables;

                    return command;
                }
            },
            {
                (ByteWriter data, Command_SetTargetFuelLevel command) => {
                    WriteSync(data, command.refuelables);
                },
                (ByteReader data) => {
                    List<CompRefuelable> refuelables = ReadSync<List<CompRefuelable>>(data);
                    refuelables.RemoveAll(r => r == null);

                    Command_SetTargetFuelLevel command = new Command_SetTargetFuelLevel();
                    command.refuelables = refuelables;

                    return command;
                }
            },
            {
                (ByteWriter data, Command_LoadToTransporter command) => {
                    WriteSync(data, command.transComp);
                    WriteSync(data, command.transporters ?? new List<CompTransporter>());
                },
                (ByteReader data) => {
                    CompTransporter transporter = ReadSync<CompTransporter>(data);
                    if (transporter == null)
                       return null;

                    List<CompTransporter> transporters = ReadSync<List<CompTransporter>>(data);
                    transporters.RemoveAll(r => r == null);

                    Command_LoadToTransporter command = new Command_LoadToTransporter{
                        transComp = transporter,
                        transporters = transporters
                    };

                    return command;
                }
            },
            {
                (ByteWriter data, Command_Ability command) => {
                    WriteSync(data, command.ability);
                    WriteSync(data, command.Pawn);
                },
                (ByteReader data) => {
                    Ability ability = ReadSync<Ability>(data);
                    Pawn pawn = ReadSync<Pawn>(data);

                    return new Command_Ability(ability, pawn);
                }
            },
            #endregion

            #region Designators
            {
                // Catch all for all Designators, merely signals to construct them
                // We can't construct them here because we need to signal ReadSyncObject
                // to change the type, which is not possible from a SyncWorker.
                (SyncWorker sync, ref Designator designator) => {

                }, true, true // <- Implicit ShouldConstruct
            },
            {
                (SyncWorker sync, ref Designator_Place place) => {
                    if (sync.isWriting) {
                        sync.Write(place.placingRot);
                    } else {
                        place.placingRot = sync.Read<Rot4>();
                    }
                }, true, true // <- Implicit ShouldConstruct
            },
            {
                (SyncWorker sync, ref Designator_Paint paint) => {
                    if (sync.isWriting) {
                        sync.Write(paint.colorDef);
                    } else {
                        paint.colorDef = sync.Read<ColorDef>();
                    }
                }, true, true // <- Implicit ShouldConstruct
            },
            {
                // Designator_Build is a Designator_Place but we aren't using Implicit
                // We can't take part of the implicit tree because Designator_Build ctor has an argument
                // So we need to implement placingRot here too, until we separate instancing from decorating.
                (SyncWorker sync, ref Designator_Build build) => {
                    if (sync.isWriting) {
                        sync.Write(build.PlacingDef);
                        sync.Write(build.placingRot);
                        if (build.PlacingDef.MadeFromStuff) {
                            sync.Write(build.stuffDef);
                        }
                        sync.Write(build.sourcePrecept);
                    } else {
                        var def = sync.Read<BuildableDef>();
                        build = new Designator_Build(def);
                        build.placingRot = sync.Read<Rot4>();
                        if (build.PlacingDef.MadeFromStuff) {
                            build.stuffDef = sync.Read<ThingDef>();
                        }
                        build.sourcePrecept = sync.Read<Precept_Building>();
                    }
                }
            },
            {
                (SyncWorker sync, ref Designator_MoveGravship moveGravship) => {
                    if (sync.isWriting)
                        sync.Write(moveGravship.marker.GravshipRotation);
                    else
                    {
                        var rot = sync.Read<Rot4>();

                        var gravController = Find.GravshipController;
                        var marker = gravController.landingMarker;

                        if (marker != null)
                            marker.GravshipRotation = rot;

                        moveGravship = gravController.moveDesignator;
                        moveGravship.deselectedRotation = moveGravship.marker.GravshipRotation;
                    }
                }, true, false
            },
            {
                (ByteWriter data, DesignationManager manager) =>
                {
                    var isNull = manager?.map == null;
                    data.WriteBool(isNull);
                    if (!isNull)
                        data.MpContext().map = manager.map;
                },
                (ByteReader data) =>
                {
                    if (data.ReadBool())
                        return null;
                    return data.MpContext().map.designationManager;
                }
            },
            {
                (SyncWorker sync, ref Designation designation) =>
                {
                    if (sync.isWriting)
                    {
                        sync.Write(designation?.designationManager);

                        if (designation?.designationManager != null)
                        {
                            sync.Write(designation.target);
                            sync.Write(designation.def);
                        }
                    }
                    else
                    {
                        var manager = sync.Read<DesignationManager>();

                        if (manager != null)
                        {
                            var target = sync.Read<LocalTargetInfo>();
                            var def = sync.Read<DesignationDef>();

                            // If the target has Thing, read designation by def for it.
                            if (target.HasThing)
                                designation = manager.DesignationOn(target.Thing, def);
                            // If the target doesn't have a Thing then it must have a cell,
                            // get the designation by def for that specific cell.
                            else
                                designation = manager.DesignationAt(target.Cell, def);
                        }
                    }
                }, true // implicit
            },
            #endregion
        };

        class Dummy_ITab_Pawn_Visitor : ITab_Pawn_Visitor { }
    }
}

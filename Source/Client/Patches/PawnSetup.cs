using HarmonyLib;
using Multiplayer.Common;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Multiplayer.Client
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
    static class PawnSpawnSetupMarker
    {
        public static bool currentlyRespawningAfterLoad;

        static void Prefix(bool respawningAfterLoad)
        {
            currentlyRespawningAfterLoad = respawningAfterLoad;
        }

        static void Finalizer()
        {
            currentlyRespawningAfterLoad = false;
        }
    }

    [HarmonyPatch(typeof(Pawn_PathFollower), nameof(Pawn_PathFollower.ResetToCurrentPosition))]
    static class PatherResetPatch
    {
        static bool Prefix() => !PawnSpawnSetupMarker.currentlyRespawningAfterLoad;
    }

    [HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), typeof(PawnGenerationRequest))]
    static class CancelSyncDuringPawnGeneration
    {
        static void Prefix() => Multiplayer.dontSync = true;
        static void Finalizer() => Multiplayer.dontSync = false;
    }

    [HarmonyPatch(typeof(PawnTextureAtlas), MethodType.Constructor)]
    static class PawnTextureAtlasCtorPatch
    {
        static void Postfix(PawnTextureAtlas __instance)
        {
            // Pawn ids can change during deserialization when fixing local (negative) ids in CrossRefHandler_Clear_Patch
            __instance.frameAssignments = new Dictionary<Pawn, PawnTextureAtlasFrameSet>(
                IdentityComparer<Pawn>.Instance
            );
        }
    }
}

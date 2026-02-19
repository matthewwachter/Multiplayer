using HarmonyLib;
using RimWorld;
using Verse;

namespace Multiplayer.Client
{
    [HarmonyPatch(typeof(Pawn_WorkSettings), nameof(Pawn_WorkSettings.SetPriority))]
    static class WorkPrioritySameValue
    {
        [HarmonyPriority(MpPriority.MpFirst + 1)]
        static bool Prefix(Pawn_WorkSettings __instance, WorkTypeDef w, int priority) => __instance.GetPriority(w) != priority;
    }

    [HarmonyPatch(typeof(Pawn_PlayerSettings), nameof(Pawn_PlayerSettings.AreaRestrictionInPawnCurrentMap), MethodType.Setter)]
    static class AreaRestrictionSameValue
    {
        [HarmonyPriority(MpPriority.MpFirst + 1)]
        static bool Prefix(Pawn_PlayerSettings __instance, Area value) => __instance.AreaRestrictionInPawnCurrentMap != value;
    }
}

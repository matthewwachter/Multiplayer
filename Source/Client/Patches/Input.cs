using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Multiplayer.Client
{
    [HarmonyPatch(typeof(UI), nameof(UI.MouseCell))]
    public static class MouseCellPatch
    {
        public static IntVec3? result;

        static void Postfix(ref IntVec3 __result)
        {
            if (result.HasValue)
                __result = result.Value;
        }
    }

    [HarmonyPatch(typeof(GenWorld), nameof(GenWorld.MouseTile))]
    public static class MouseTilePatch
    {
        public static PlanetTile? result;

        static void Postfix(ref PlanetTile __result)
        {
            if (result.HasValue)
                __result = result.Value;
        }
    }

    [HarmonyPatch(typeof(KeyBindingDef), nameof(KeyBindingDef.IsDownEvent), MethodType.Getter)]
    public static class KeyIsDownPatch
    {
        public static bool? shouldQueue;

        static bool Prefix(KeyBindingDef __instance) => !(__instance == KeyBindingDefOf.QueueOrder && shouldQueue.HasValue);

        static void Postfix(KeyBindingDef __instance, ref bool __result)
        {
            if (__instance == KeyBindingDefOf.QueueOrder && shouldQueue.HasValue)
                __result = shouldQueue.Value;
        }
    }

    [HarmonyPatch(typeof(DesignationDragger), nameof(DesignationDragger.UpdateDragCellsIfNeeded))]
    static class CancelUpdateDragCellsIfNeeded
    {
        static bool Prefix() => !Multiplayer.ExecutingCmds;
    }
}

using HarmonyLib;
using Multiplayer.Client.Patches;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace Multiplayer.Client
{
    [HarmonyPatch(typeof(WorldRoutePlanner), nameof(WorldRoutePlanner.ShouldStop), MethodType.Getter)]
    static class RoutePlanner_ShouldStop_Patch
    {
        static void Postfix(WorldRoutePlanner __instance, ref bool __result)
        {
            if (Multiplayer.Client == null) return;

            // Ignore unpausing
            if (__result && __instance.active && WorldRendererUtility.WorldSelected)
                __result = false;
        }
    }

    [HarmonyPatch]
    static class SetGodModePatch
    {
        static IEnumerable<MethodInfo> TargetMethods()
        {
            yield return AccessTools.Method(typeof(DebugWindowsOpener), nameof(DebugWindowsOpener.DrawButtons));
            yield return AccessTools.Method(typeof(DebugWindowsOpener), nameof(DebugWindowsOpener.DevToolStarterOnGUI));
            yield return AccessTools.PropertySetter(typeof(Prefs), nameof(Prefs.DevMode));
        }

        static void Prefix(ref bool __state)
        {
            __state = DebugSettings.godMode;
        }

        static void Postfix(bool __state)
        {
            if (Multiplayer.Client != null && __state != DebugSettings.godMode)
                Multiplayer.GameComp.SetGodMode(Multiplayer.session.playerId, DebugSettings.godMode);
        }
    }

    [HarmonyPatch]
    public static class StoragesKeepsTheirOwners
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CompBiosculpterPod), nameof(CompBiosculpterPod.PostExposeData))]
        static void PostCompBiosculpterPod(CompBiosculpterPod __instance)
            => FixStorage(__instance, __instance.allowedNutritionSettings);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CompChangeableProjectile), nameof(CompChangeableProjectile.PostExposeData))]
        static void PostCompChangeableProjectile(CompChangeableProjectile __instance)
            => FixStorage(__instance, __instance.allowedShellsSettings);

        // Fix syncing of copy/paste due to null StorageSettings.owner by assigning the parent
        // in ExposeData. The patched types omit passing/assigning self as the owner by passing
        // Array.Empty<object>() as the argument to expose data on StorageSetting.
        static void FixStorage(IStoreSettingsParent __instance, StorageSettings ___allowedNutritionSettings)
        {
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                ___allowedNutritionSettings.owner ??= __instance;
        }
    }

    [HarmonyPatch(typeof(MoteAttachLink), nameof(MoteAttachLink.UpdateDrawPos))]
    static class MoteAttachLinkUsesTruePosition
    {
        static void Prefix() => DrawPosPatch.returnTruePosition = true;

        static void Finalizer() => DrawPosPatch.returnTruePosition = false;
    }

    [HarmonyPatch(typeof(DebugWindowsOpener), nameof(DebugWindowsOpener.TryOpenOrClosePalette))]
    static class DebugWindowsOpenerReloadingPatch
    {
        static bool Prefix()
        {
            // During multiplayer reload, Find.World can be null when UIRoot_Play.Init() calls TryOpenOrClosePalette()
            // This prevents the Dialog_DevPalette from being opened, which would cause a null reference exception
            // in Window.PreOpen() when it tries to access Find.WorldSelector
            if (Multiplayer.reloading)
            {
                return false; // Skip opening/closing the dev palette during multiplayer reload
            }

            return true; // Execute normally
        }
    }

    [HarmonyPatch(typeof(ScreenFader), nameof(ScreenFader.SetColor))]
    static class DisableScreenFade1
    {
        static bool Prefix() => LongEventHandler.eventQueue.All(e => e.eventTextKey == "MpLoading");
    }

    [HarmonyPatch(typeof(ScreenFader), nameof(ScreenFader.StartFade), typeof(Color), typeof(float), typeof(float))]
    static class DisableScreenFade2
    {
        static bool Prefix() => LongEventHandler.eventQueue.All(e => e.eventTextKey == "MpLoading");
    }

    [HarmonyPatch(typeof(IncidentDef), nameof(IncidentDef.TargetAllowed))]
    static class GameConditionIncidentTargetPatch
    {
        static void Postfix(IncidentDef __instance, IIncidentTarget target, ref bool __result)
        {
            if (Multiplayer.Client == null) return;

            if (__instance.workerClass == typeof(IncidentWorker_MakeGameCondition) || __instance.workerClass == typeof(IncidentWorker_Aurora))
                __result = target.IncidentTargetTags().Contains(IncidentTargetTagDefOf.Map_PlayerHome);
        }
    }

    [HarmonyPatch(typeof(IncidentWorker_Aurora), nameof(IncidentWorker_Aurora.AuroraWillEndSoon))]
    static class IncidentWorkerAuroraPatch
    {
        static void Postfix(Map map, ref bool __result)
        {
            if (Multiplayer.Client == null) return;

            if (map != Multiplayer.MapContext)
                __result = false;
        }
    }
}

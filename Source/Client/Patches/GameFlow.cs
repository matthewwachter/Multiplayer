using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace Multiplayer.Client
{
    [HarmonyPatch(typeof(GameEnder))]
    [HarmonyPatch(nameof(GameEnder.CheckOrUpdateGameOver))]
    public static class GameEnderPatch
    {
        static bool Prefix() => Multiplayer.Client == null;
    }

    [HarmonyPatch(typeof(Game), nameof(Game.LoadGame))]
    static class LoadGameMarker
    {
        public static bool loading;

        static void Prefix() => loading = true;
        static void Finalizer() => loading = false;
    }

    [HarmonyPatch(typeof(Root_Play), nameof(Root_Play.Start))]
    static class RootPlayStartMarker
    {
        public static bool starting;

        static void Prefix() => starting = true;
        static void Finalizer() => starting = false;
    }

    [HarmonyPatch(typeof(LongEventHandler), nameof(LongEventHandler.QueueLongEvent),
        [typeof(Action), typeof(string), typeof(bool), typeof(Action<Exception>), typeof(bool), typeof(bool), typeof(Action)])]
    static class CancelRootPlayStartLongEvents
    {
        public static bool cancel;

        static bool Prefix()
        {
            if (RootPlayStartMarker.starting && cancel) return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(TickManager), nameof(TickManager.Pause))]
    static class TickManagerPausePatch
    {
        static bool Prefix() => Multiplayer.Client == null;
    }
}

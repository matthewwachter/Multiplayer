using HarmonyLib;
using Multiplayer.API;
using Multiplayer.Client.Patches;
using RimWorld;
using RimWorld.Planet;
using System.Linq;
using System.Reflection;
using Verse;

namespace Multiplayer.Client
{
    [HarmonyPatch(typeof(Dialog_BillConfig), MethodType.Constructor)]
    [HarmonyPatch(new[] {typeof(Bill_Production), typeof(IntVec3)})]
    public static class DialogPatch
    {
        static void Postfix(Dialog_BillConfig __instance)
        {
            __instance.absorbInputAroundWindow = false;
        }
    }

    [HarmonyPatch(typeof(WindowStack))]
    [HarmonyPatch(nameof(WindowStack.WindowsForcePause), MethodType.Getter)]
    public static class WindowsPausePatch
    {
        static void Postfix(ref bool __result)
        {
            if (Multiplayer.Client != null)
                __result = false;
        }
    }

    [HarmonyPatch]
    static class PatchQuestChoices
    {
        // It's the only method with a Choice
        static MethodBase TargetMethod()
        {
            return AccessTools.FirstMethod(
                AccessTools.FirstInner(typeof(MainTabWindow_Quests),
                    t => t.GetFields().Any(f => f.Name == "localChoice")
                ), m => !m.IsConstructor);
        }

        static bool Prefix(QuestPart_Choice.Choice ___localChoice)
        {
            if (Multiplayer.Client == null) return true;

            foreach(var part in Find.QuestManager.QuestsListForReading.SelectMany(q => q.parts).OfType<QuestPart_Choice>()) {
                int index = part.choices.IndexOf(___localChoice);

                if (index >= 0) {
                    Choose(part, index);
                    return false;
                }
            }

            // Should not happen!
            Log.Error("Multiplayer :: Choice without QuestPart_Choice... you're about to desync.");
            return true;
        }

        // Registered in SyncMethods.cs
        internal static void Choose(QuestPart_Choice part, int index)
        {
            part.Choose(part.choices[index]);
        }
    }

    [HarmonyPatch(typeof(DiaOption), nameof(DiaOption.Activate))]
    static class NodeTreeDialogSync
    {
        static bool Prefix(DiaOption __instance)
        {
            if (Multiplayer.session == null || !SyncUtil.isDialogNodeTreeOpen || !(__instance.dialog is Dialog_NodeTree dialog))
            {
                SyncUtil.isDialogNodeTreeOpen = false;
                return true;
            }

            // Get the current node, find the index of the option on it, and call a (synced) method
            var currentNode = dialog.curNode;
            int index = currentNode.options.FindIndex(x => x == __instance);
            if (index >= 0)
                SyncDialogOptionByIndex(index);

            return false;
        }

        [SyncMethod]
        internal static void SyncDialogOptionByIndex(int position)
        {

            // Make sure we have the correct dialog and data
            if (position >= 0)
            {
                var dialog = Find.WindowStack.WindowOfType<Dialog_NodeTree>();

                if (dialog != null && position < dialog.curNode.options.Count)
                {
                    SyncUtil.isDialogNodeTreeOpen = false; // Prevents infinite loop, otherwise PreSyncDialog would call this method over and over again
                    var option = dialog.curNode.options[position]; // Get the correct DiaOption
                    option.Activate(); // Call the Activate method to actually "press" the button

                    if (!option.resolveTree) SyncUtil.isDialogNodeTreeOpen = true; // In case dialog is still open, we mark it as such

                    // Try opening the trading menu if the picked option was supposed to do so (caravan meeting, trading option)
                    if (Multiplayer.Client != null && Multiplayer.WorldComp.trading.Any(t => t.trader is Caravan))
                        Find.WindowStack.Add(new TradingWindow());
                }
                else SyncUtil.isDialogNodeTreeOpen = false;
            }
            else SyncUtil.isDialogNodeTreeOpen = false;
        }
    }

    [HarmonyPatch(typeof(Dialog_NodeTree), nameof(Dialog_NodeTree.PostClose))]
    static class NodeTreeDialogMarkClosed
    {
        // Set the dialog as closed in here as well just in case
        static void Prefix() => SyncUtil.isDialogNodeTreeOpen = false;
    }

    [HarmonyPatch(typeof(NamePlayerFactionAndSettlementUtility), nameof(NamePlayerFactionAndSettlementUtility.CanNameAnythingNow))]
    static class NoNamingInMultiplayer
    {
        static bool Prefix() => Multiplayer.Client == null;
    }
}

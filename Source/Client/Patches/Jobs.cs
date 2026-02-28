using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.AI;

namespace Multiplayer.Client
{
    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.StartJob))]
    static class JobTrackerStartFixFrames
    {
        static FieldInfo FrameCountField = AccessTools.Field(typeof(RealTime), nameof(RealTime.frameCount));
        static MethodInfo FrameCountReplacementMethod = AccessTools.Method(typeof(JobTrackerStartFixFrames), nameof(FrameCountReplacement));

        // Transpilers should be a last resort but there's no better way to patch this
        // and handling this case properly might prevent some crashes and help with debugging
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> insts)
        {
            var patchCount = 0;

            foreach (var inst in insts)
            {
                yield return inst;

                if (inst.operand as FieldInfo == FrameCountField)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, FrameCountReplacementMethod);
                    patchCount++;
                }
            }

            const int expectedPatches = 2;
            if (patchCount != expectedPatches)
                Log.Error($"Patching {nameof(JobTrackerStartFixFrames)} failed (expected: {expectedPatches}, patched: {patchCount}). Was the original method changed?");
        }

        static int FrameCountReplacement(int frameCount, Pawn_JobTracker tracker)
        {
            return tracker.pawn.Map.AsyncTime()?.eventCount ?? frameCount;
        }
    }
}

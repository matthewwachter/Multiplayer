using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml.Linq;
using Verse;

namespace Multiplayer.Client
{
    public static class ValueSavePatch
    {
        public static bool DoubleSave_Prefix(string label, ref double value)
        {
            if (Scribe.mode != LoadSaveMode.Saving) return true;
            Scribe.saver.WriteElement(label, value.ToString("G17"));
            return false;
        }

        public static bool FloatSave_Prefix(string label, ref float value)
        {
            if (Scribe.mode != LoadSaveMode.Saving) return true;
            Scribe.saver.WriteElement(label, value.ToString("G9"));
            return false;
        }
    }

    [HarmonyPatch(typeof(DirectXmlSaver), nameof(DirectXmlSaver.XElementFromObject), typeof(object), typeof(Type), typeof(string), typeof(FieldInfo), typeof(bool))]
    static class ExtendDirectXmlSaver
    {
        public static bool extend;

        static bool Prefix(object obj, Type expectedType, string nodeName, FieldInfo owningField, ref XElement __result)
        {
            if (!extend) return true;
            if (obj == null) return true;

            if (obj is Array arr)
            {
                var elementType = arr.GetType().GetElementType();
                var listType = typeof(List<>).MakeGenericType(elementType);
                __result = DirectXmlSaver.XElementFromObject(Activator.CreateInstance(listType, arr), listType, nodeName, owningField);
                return false;
            }

            string content = null;

            if (obj is Type type)
                content = type.FullName;
            else if (obj is MethodBase method)
                content = method.MethodDesc();
            else if (obj is Delegate del)
                content = del.Method.MethodDesc();

            if (content != null)
            {
                __result = new XElement(nodeName, content);
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Game), nameof(Game.LoadGame))]
    static class AllowCurrentMapNullWhenLoading
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> insts)
        {
            var list = insts.ToList();

            var strIndex = list.FirstIndexOf(i =>
                "Current map is null after loading but there are maps available. Setting current map to [0].".Equals(i.operand)
            );

            if (strIndex < 0)
            {
                Log.Error($"Patching {nameof(AllowCurrentMapNullWhenLoading)} failed (target string not found). Was the original method changed?");
                return list;
            }

            // Remove Log.Error(str) call and setting value=0
            list.RemoveAt(strIndex);
            list.RemoveAt(strIndex);
            list.RemoveAt(strIndex);
            list.RemoveAt(strIndex);

            return list;
        }
    }

    [HarmonyPatch(typeof(ThingGrid), nameof(ThingGrid.Register))]
    static class DontEnlistNonSaveableThings
    {
        static bool Prefix(Thing t) => t.def.isSaveable;
    }
}

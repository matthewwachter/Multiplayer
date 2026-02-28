using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace Multiplayer.Client.Desyncs;

public static class ModAssemblyLookup
{
    private static Dictionary<Assembly, string> cache;

    public static void Build()
    {
        cache = new Dictionary<Assembly, string>();
        foreach (var mod in LoadedModManager.RunningMods)
            foreach (var asm in mod.assemblies.loadedAssemblies)
                cache.TryAdd(asm, mod.Name);
    }

    public static string GetModName(Assembly asm)
        => cache != null && cache.TryGetValue(asm, out var name) ? name : null;
}

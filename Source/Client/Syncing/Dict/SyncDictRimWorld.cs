// ReSharper disable RedundantLambdaParameterType

namespace Multiplayer.Client
{
    public static partial class SyncDictRimWorld
    {
        internal static SyncWorkerDictionaryTree syncWorkers = SyncWorkerDictionaryTree.Merge(
            BuildCoreWorkers(),
            BuildPawnWorkers(),
            BuildWorldWorkers(),
            BuildUIWorkers(),
            BuildBuildingWorkers()
        );
    }
}

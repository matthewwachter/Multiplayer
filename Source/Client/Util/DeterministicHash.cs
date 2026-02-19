using Multiplayer.Common;

namespace Multiplayer.Client.Util
{
    // Forwarding class — implementation moved to Multiplayer.Common.DeterministicHash
    public static class DeterministicHash
    {
        public const int DefaultSeed = Common.DeterministicHash.DefaultSeed;
        public const int DefaultSeed2 = Common.DeterministicHash.DefaultSeed2;

        public static int HashCombineInt(int v1, int v2, int v3)
            => Common.DeterministicHash.HashCombineInt(v1, v2, v3);

        public static int HashCombineInt(int v1, int v2, int v3, int v4, int v5)
            => Common.DeterministicHash.HashCombineInt(v1, v2, v3, v4, v5);

        public static int HashCombineInt(int v1, int v2, int v3, int v4, int v5, int v6)
            => Common.DeterministicHash.HashCombineInt(v1, v2, v3, v4, v5, v6);

        public static int HashCombineInt(int v1, int v2, int v3, int v4, int v5, int v6, int v7)
            => Common.DeterministicHash.HashCombineInt(v1, v2, v3, v4, v5, v6, v7);

        public static int HashCombineInt(int v1, int v2, int v3, int v4, int v5, int v6, int v7, int v8)
            => Common.DeterministicHash.HashCombineInt(v1, v2, v3, v4, v5, v6, v7, v8);
    }
}

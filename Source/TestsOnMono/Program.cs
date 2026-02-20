using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Multiplayer.Client;
using Multiplayer.Client.Desyncs;
using Multiplayer.Common;

// These only work on Windows Mono
namespace TestsOnMono;

static class Program
{
    // Test DeferredStackTracing
    public static void Main1(string[] args)
    {
        Native.mini_parse_debug_option("disable_omit_fp");
        Native.InitLmfPtr(Native.NativeOS.Dummy);
        Native.EarlyInit(Native.NativeOS.Dummy);

        TestClass<int>.Test1<int>();
        TestClass<int>.Test();
    }

    // Test rounding modes
    public static void Main2(string[] args)
    {
        void Print()
        {
            Console.WriteLine(ExternMethods.GetRound());
            Console.WriteLine(RoundMode.GetCurrentRoundMode());
        }

        Print();
        ExternMethods.SetRound(RoundModeEnum.Upward);
        Print();
        ExternMethods.SetRound(RoundModeEnum.Downward);
        Print();
        ExternMethods.SetRound(RoundModeEnum.TowardZero);
        Print();
    }

    // Test patching generic methods with Harmony
    public static void Main(string[] args)
    {
        TestClassForPatches<SomeClassDerived> test = new()
        {
            field = new SomeClassDerived { a = 2, b = 1 }
        };

        Console.WriteLine(test.GetField().b);

        new Harmony("test").Patch(
            typeof(TestClassForPatches<SomeClass>).GetMethod("GetField"),
            postfix: new HarmonyMethod(typeof(Program).GetMethod("GenericPostfix"))
        );

        Console.WriteLine(test.GetField().b);
    }

#pragma warning disable CS1717 // Assignment made to same variable
    public static void GenericPostfix(ref SomeClass __result)
    {
        __result = __result;
    }
#pragma warning restore CS1717

    class TestClass<S>
    {
        public static void Test()
        {
            int hash = 0;
            long[] trace = new long[8];
            DeferredStackTracingImpl.TraceImpl(trace, ref hash);
            Console.WriteLine(hash);

            foreach (long l in trace)
                Console.WriteLine(Native.MethodNameFromAddr(l, false));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.NoInlining)]
        public static void Test1<T>()
        {
            Test();
        }
    }

    public class SomeClass
    {
        public int a;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public int GetA() => a;
    }

    public class SomeClassDerived : SomeClass
    {
        public int b;
    }

#pragma warning disable CS0649 // Field is never assigned to
    public class SomeClass2
    {
        public int a;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public int GetA() => a;
    }
#pragma warning restore CS0649

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value
    class TestClassForPatches<T>
    {
        public T field;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public T GetField() => field;
    }
#pragma warning restore CS8618
}

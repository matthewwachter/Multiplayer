using Multiplayer.Client;
using Multiplayer.Common;

namespace Tests;

public class SyncSerializationTest
{
    private SyncSerialization ser;

    [SetUp]
    public void SetUp()
    {
        ser = new SyncSerialization(new TestTypeHelper());
    }

    private T? Roundtrip<T>(T? obj)
    {
        var writer = new ByteWriter();
        ser.WriteSync(writer, obj);
        return ser.ReadSync<T>(new ByteReader(writer.ToArray()));
    }

    private object? RoundtripObj(object? obj, Type type)
    {
        var writer = new ByteWriter();
        ser.WriteSyncObject(writer, obj, type);
        return ser.ReadSyncObject(new ByteReader(writer.ToArray()), type);
    }

    // Primitives

    [Test]
    public void Int_Roundtrip()
    {
        Assert.That(Roundtrip(42), Is.EqualTo(42));
    }

    [Test]
    public void Bool_Roundtrip()
    {
        Assert.That(Roundtrip(true), Is.EqualTo(true));
        Assert.That(Roundtrip(false), Is.EqualTo(false));
    }

    [Test]
    public void Float_Roundtrip()
    {
        Assert.That(Roundtrip(3.14f), Is.EqualTo(3.14f));
    }

    [Test]
    public void Double_Roundtrip()
    {
        Assert.That(Roundtrip(2.718281828d), Is.EqualTo(2.718281828d));
    }

    [Test]
    public void Byte_Roundtrip()
    {
        Assert.That(Roundtrip((byte)0xFF), Is.EqualTo((byte)0xFF));
    }

    [Test]
    public void Long_Roundtrip()
    {
        Assert.That(Roundtrip(long.MaxValue), Is.EqualTo(long.MaxValue));
    }

    // Enum

    [Test]
    public void Enum_Roundtrip()
    {
        Assert.That(RoundtripObj(TestEnum.B, typeof(TestEnum)), Is.EqualTo(TestEnum.B));
    }

    // Array

    [Test]
    public void IntArray_Roundtrip()
    {
        var input = new[] { 1, 2, 3 };
        Assert.That(RoundtripObj(input, typeof(int[])), Is.EqualTo(input));
    }

    [Test]
    public void NullArray_Roundtrip()
    {
        Assert.That(RoundtripObj(null, typeof(int[])), Is.Null);
    }

    [Test]
    public void EmptyArray_Roundtrip()
    {
        var input = Array.Empty<int>();
        var result = (int[]?)RoundtripObj(input, typeof(int[]));
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }

    // List null

    [Test]
    public void NullList_Roundtrip()
    {
        Assert.That(RoundtripObj(null, typeof(List<int>)), Is.Null);
    }

    // Nullable<T>

    [Test]
    public void Nullable_WithValue_Roundtrip()
    {
        Assert.That(RoundtripObj((int?)42, typeof(int?)), Is.EqualTo(42));
    }

    [Test]
    public void Nullable_Null_Roundtrip()
    {
        Assert.That(RoundtripObj(null, typeof(int?)), Is.Null);
    }

    // Dictionary

    [Test]
    public void Dictionary_Roundtrip()
    {
        var input = new Dictionary<string, int> { { "a", 1 }, { "b", 2 } };
        var result = (Dictionary<string, int>?)RoundtripObj(input, typeof(Dictionary<string, int>));
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["a"], Is.EqualTo(1));
        Assert.That(result["b"], Is.EqualTo(2));
    }

    [Test]
    public void NullDictionary_Roundtrip()
    {
        Assert.That(RoundtripObj(null, typeof(Dictionary<string, int>)), Is.Null);
    }

    // HashSet

    [Test]
    public void HashSet_Roundtrip()
    {
        var input = new HashSet<int> { 10, 20, 30 };
        var result = (HashSet<int>?)RoundtripObj(input, typeof(HashSet<int>));
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.SetEquals(input), Is.True);
    }

    [Test]
    public void NullHashSet_Roundtrip()
    {
        Assert.That(RoundtripObj(null, typeof(HashSet<int>)), Is.Null);
    }

    // ValueTuple<T1, T2>

    [Test]
    public void BinaryValueTuple_Roundtrip()
    {
        var input = (42, "hello");
        var result = RoundtripObj(input, typeof((int, string)));
        Assert.That(result, Is.EqualTo(input));
    }

    // Tuple<T1, T2, T3>

    [Test]
    public void Tuple3_Roundtrip()
    {
        var input = Tuple.Create(1, "two", true);
        var result = RoundtripObj(input, typeof(Tuple<int, string, bool>));
        Assert.That(result, Is.EqualTo(input));
    }

    // CanHandle

    [Test]
    public void CanHandle_SupportedTypes()
    {
        Assert.That(ser.CanHandle(typeof(int)), Is.True);
        Assert.That(ser.CanHandle(typeof(string)), Is.True);
        Assert.That(ser.CanHandle(typeof(int[])), Is.True);
        Assert.That(ser.CanHandle(typeof(List<int>)), Is.True);
        Assert.That(ser.CanHandle(typeof(Dictionary<string, int>)), Is.True);
        Assert.That(ser.CanHandle(typeof(int?)), Is.True);
        Assert.That(ser.CanHandle(typeof(TestEnum)), Is.True);
    }

    [Test]
    public void CanHandle_UnregisteredClass_ReturnsFalse()
    {
        Assert.That(ser.CanHandle(typeof(UnregisteredClass)), Is.False);
    }

    // No writer throws

    [Test]
    public void WriteSyncObject_UnknownType_Throws()
    {
        var writer = new ByteWriter();
        Assert.Throws<SerializationException>(
            () => ser.WriteSyncObject(writer, new UnregisteredClass(), typeof(UnregisteredClass))
        );
    }

    private class TestTypeHelper : SyncTypeHelper
    {
        public override List<Type> GetImplementations(Type baseType) => [];
    }

    private enum TestEnum : int
    {
        A = 0,
        B = 1,
        C = 2,
    }

    private class UnregisteredClass { }
}

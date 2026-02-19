using Multiplayer.Common;

namespace Tests;

public class ExtensionsTest
{
    [Test]
    public void GetDefaultValue_ValueType_ReturnsDefault()
    {
        Assert.That(typeof(int).GetDefaultValue(), Is.EqualTo(0));
    }

    [Test]
    public void GetDefaultValue_ReferenceType_ReturnsNull()
    {
        Assert.That(typeof(string).GetDefaultValue(), Is.Null);
    }

    [Test]
    public void GetDefaultValue_Struct_ReturnsDefault()
    {
        var result = typeof(TestStruct).GetDefaultValue();
        Assert.That(result, Is.EqualTo(default(TestStruct)));
    }

    [Test]
    public void GetOrAdd_KeyMissing_CallsFactory()
    {
        var dict = new Dictionary<string, int>();
        var result = dict.GetOrAdd("key", k => 42);
        Assert.That(result, Is.EqualTo(42));
        Assert.That(dict["key"], Is.EqualTo(42));
    }

    [Test]
    public void GetOrAdd_KeyPresent_ReturnsExisting()
    {
        var dict = new Dictionary<string, int> { { "key", 10 } };
        var result = dict.GetOrAdd("key", k => 42);
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void GetOrAddNew_CreatesNewInstance()
    {
        var dict = new Dictionary<string, List<int>>();
        var result = dict.GetOrAddNew<string, List<int>>("key");
        Assert.That(result, Is.Not.Null);
        Assert.That(dict.ContainsKey("key"), Is.True);
    }

    [Test]
    public void ToEnumerable_SingleItem_YieldsOne()
    {
        var result = 42.ToEnumerable().ToList();
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo(42));
    }

    [Test]
    public void Combine_TwoInts_ProducesConsistentResult()
    {
        int a = 123, b = 456;
        int result1 = a.Combine(b);
        int result2 = a.Combine(b);
        Assert.That(result1, Is.EqualTo(result2));
    }

    [Test]
    public void Combine_OrderMatters()
    {
        int a = 123, b = 456;
        Assert.That(a.Combine(b), Is.Not.EqualTo(b.Combine(a)));
    }

    [Test]
    public void Append_TwoArrays_Concatenated()
    {
        var arr1 = new[] { 1, 2 };
        var arr2 = new[] { 3, 4 };
        Assert.That(arr1.Append(arr2), Is.EqualTo(new[] { 1, 2, 3, 4 }));
    }

    [Test]
    public void Append_ToEmpty()
    {
        var empty = Array.Empty<int>();
        Assert.That(empty.Append(1, 2), Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void Append_EmptySecond()
    {
        var arr = new[] { 1, 2 };
        Assert.That(arr.Append(), Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void SubArray_MiddleSlice()
    {
        var arr = new[] { 10, 20, 30, 40, 50 };
        Assert.That(arr.SubArray(1, 3), Is.EqualTo(new[] { 20, 30, 40 }));
    }

    [Test]
    public void SubArray_FromIndexToEnd()
    {
        var arr = new[] { 10, 20, 30, 40, 50 };
        Assert.That(arr.SubArray(3), Is.EqualTo(new[] { 40, 50 }));
    }

    [Test]
    public void SubArray_FullArray()
    {
        var arr = new[] { 1, 2, 3 };
        Assert.That(arr.SubArray(0, 3), Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void HasFlag_SingleFlagSet()
    {
        var value = TestFlags.A;
        Assert.That(value.HasFlag(TestFlags.A), Is.True);
    }

    [Test]
    public void HasFlag_CombinedFlags()
    {
        var value = TestFlags.A | TestFlags.B;
        Assert.That(value.HasFlag(TestFlags.A), Is.True);
        Assert.That(value.HasFlag(TestFlags.B), Is.True);
    }

    [Test]
    public void HasFlag_FlagNotSet()
    {
        var value = TestFlags.A;
        Assert.That(value.HasFlag(TestFlags.B), Is.False);
    }

    [Test]
    public void FindIndex_Found()
    {
        var arr = new[] { "a", "b", "c" };
        Assert.That(arr.FindIndex("b"), Is.EqualTo(1));
    }

    [Test]
    public void FindIndex_NotFound()
    {
        var arr = new[] { "a", "b", "c" };
        Assert.That(arr.FindIndex("z"), Is.EqualTo(-1));
    }

    [Test]
    public void ToHexString_KnownBytes()
    {
        var bytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        Assert.That(bytes.ToHexString(), Is.EqualTo("deadbeef"));
    }

    [Test]
    public void ToHexString_EmptyArray()
    {
        Assert.That(Array.Empty<byte>().ToHexString(), Is.EqualTo(""));
    }

    [Test]
    public void MaxOrZero_NonEmpty()
    {
        var items = new[] { 1, 5, 3 };
        Assert.That(items.MaxOrZero(x => (float)x), Is.EqualTo(5f));
    }

    [Test]
    public void MaxOrZero_Empty()
    {
        var items = Array.Empty<int>();
        Assert.That(items.MaxOrZero(x => (float)x), Is.EqualTo(0f));
    }

    [Test]
    public void RemovePrefix_MatchingPrefix()
    {
        Assert.That("HelloWorld".RemovePrefix("Hello"), Is.EqualTo("World"));
    }

    [Test]
    public void RemovePrefix_NonMatching()
    {
        Assert.That("HelloWorld".RemovePrefix("Bye"), Is.EqualTo("HelloWorld"));
    }

    [Test]
    public void RemovePrefix_NullPrefix()
    {
        Assert.That("HelloWorld".RemovePrefix(null), Is.EqualTo("HelloWorld"));
    }

    [Flags]
    private enum TestFlags : uint
    {
        A = 1,
        B = 2,
        C = 4,
    }

    private struct TestStruct
    {
        public int X;
    }
}

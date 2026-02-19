using Multiplayer.Common.Util;

namespace Tests;

public class CollectionExtensionsTest
{
    [Test]
    public void IndexNullable_Found()
    {
        var list = new List<int> { 10, 20, 30 };
        Assert.That(list.IndexNullable(x => x == 20), Is.EqualTo(1));
    }

    [Test]
    public void IndexNullable_NotFound_ReturnsNull()
    {
        var list = new List<int> { 10, 20, 30 };
        Assert.That(list.IndexNullable(x => x == 99), Is.Null);
    }

    [Test]
    public void IndexNullable_EmptyCollection_ReturnsNull()
    {
        var list = new List<int>();
        Assert.That(list.IndexNullable(x => x == 1), Is.Null);
    }

    [Test]
    public void RemoveNulls_RemovesNullEntries()
    {
        var list = new List<string?> { "a", null, "b", null, "c" };
        ((System.Collections.IList)list).RemoveNulls();
        Assert.That(list, Does.Not.Contain(null));
        Assert.That(list, Does.Contain("a"));
    }

    [Test]
    public void AllNotNull_FiltersNulls()
    {
        var list = new List<string?> { "a", null, "b", null };
        var result = list.AllNotNull().ToList();
        Assert.That(result, Is.EqualTo(new List<string> { "a", "b" }));
    }

    [Test]
    public void EqualAsSets_SameElements_DifferentOrder()
    {
        var a = new List<int> { 1, 2, 3 };
        var b = new List<int> { 3, 1, 2 };
        Assert.That(a.EqualAsSets(b), Is.True);
    }

    [Test]
    public void EqualAsSets_DifferentElements()
    {
        var a = new List<int> { 1, 2, 3 };
        var b = new List<int> { 1, 2, 4 };
        Assert.That(a.EqualAsSets(b), Is.False);
    }

    [Test]
    public void EqualAsSets_DifferentSizes()
    {
        var a = new List<int> { 1, 2 };
        var b = new List<int> { 1, 2, 3 };
        Assert.That(a.EqualAsSets(b), Is.False);
    }

    [Test]
    public void ToDictionaryPermissive_OverridesDuplicateKeys()
    {
        var items = new[] { ("a", 1), ("b", 2), ("a", 3) };
        var dict = items.ToDictionaryPermissive(x => x.Item1, x => x.Item2);
        Assert.That(dict["a"], Is.EqualTo(3));
        Assert.That(dict.Count, Is.EqualTo(2));
    }

    [Test]
    public void ToDictionaryConsistent_AllowsEqualDuplicates()
    {
        var items = new[] { ("a", 1), ("b", 2), ("a", 1) };
        var dict = items.ToDictionaryConsistent(x => x.Item1, x => x.Item2);
        Assert.That(dict["a"], Is.EqualTo(1));
    }

    [Test]
    public void ToDictionaryConsistent_ThrowsOnConflictingDuplicates()
    {
        var items = new[] { ("a", 1), ("a", 2) };
        Assert.Throws<ArgumentException>(() =>
            items.ToDictionaryConsistent(x => x.Item1, x => x.Item2));
    }

    [Test]
    public void GetOrEmpty_KeyExists_ReturnsSingleElement()
    {
        var dict = new Dictionary<string, int> { { "a", 1 } };
        Assert.That(dict.GetOrEmpty("a").ToList(), Is.EqualTo(new List<int> { 1 }));
    }

    [Test]
    public void GetOrEmpty_KeyMissing_ReturnsEmpty()
    {
        var dict = new Dictionary<string, int> { { "a", 1 } };
        Assert.That(dict.GetOrEmpty("b").ToList(), Is.Empty);
    }

    [Test]
    public void RemoveFirst_ReturnsAndRemovesFirstElement()
    {
        var list = new List<int> { 10, 20, 30 };
        var first = list.RemoveFirst();
        Assert.That(first, Is.EqualTo(10));
        Assert.That(list, Is.EqualTo(new List<int> { 20, 30 }));
    }

    [Test]
    public void AddRange_Dictionary()
    {
        var dest = new Dictionary<string, int> { { "a", 1 } };
        var source = new Dictionary<string, int> { { "b", 2 }, { "c", 3 } };
        dest.AddRange(source);
        Assert.That(dest.Count, Is.EqualTo(3));
        Assert.That(dest["b"], Is.EqualTo(2));
    }
}

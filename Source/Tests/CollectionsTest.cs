using Multiplayer.Common;

namespace Tests;

public class UniqueListTest
{
    [Test]
    public void Add_UniqueItems_AllAdded()
    {
        var list = new UniqueList<int>();
        Assert.That(list.Add(1), Is.True);
        Assert.That(list.Add(2), Is.True);
        Assert.That(list.Add(3), Is.True);
        Assert.That(list.Count, Is.EqualTo(3));
    }

    [Test]
    public void Add_DuplicateItem_Rejected()
    {
        var list = new UniqueList<int>();
        Assert.That(list.Add(1), Is.True);
        Assert.That(list.Add(1), Is.False);
        Assert.That(list.Count, Is.EqualTo(1));
    }

    [Test]
    public void Indexer_ReturnsCorrectItem()
    {
        var list = new UniqueList<string>();
        list.Add("a");
        list.Add("b");
        list.Add("c");
        Assert.That(list[0], Is.EqualTo("a"));
        Assert.That(list[1], Is.EqualTo("b"));
        Assert.That(list[2], Is.EqualTo("c"));
    }

    [Test]
    public void PreservesInsertionOrder()
    {
        var list = new UniqueList<int>();
        list.Add(3);
        list.Add(1);
        list.Add(2);
        Assert.That(list.ToArray(), Is.EqualTo(new[] { 3, 1, 2 }));
    }

    [Test]
    public void Contains_ReturnsTrueForExistingItem()
    {
        var list = new UniqueList<int>();
        list.Add(42);
        Assert.That(list.Contains(42), Is.True);
        Assert.That(list.Contains(99), Is.False);
    }

    [Test]
    public void IndexOf_ReturnsCorrectIndex()
    {
        var list = new UniqueList<string>();
        list.Add("x");
        list.Add("y");
        Assert.That(list.IndexOf("y"), Is.EqualTo(1));
        Assert.That(list.IndexOf("z"), Is.EqualTo(-1));
    }

    [Test]
    public void Enumeration_MatchesInsertionOrder()
    {
        var list = new UniqueList<int>();
        list.Add(10);
        list.Add(20);
        list.Add(30);
        Assert.That(list.ToList(), Is.EqualTo(new List<int> { 10, 20, 30 }));
    }
}

public class FixedSizeQueueTest
{
    [Test]
    public void Enqueue_WithinLimit_AllItemsRetained()
    {
        var q = new FixedSizeQueue<int> { Limit = 3 };
        q.Enqueue(1);
        q.Enqueue(2);
        q.Enqueue(3);
        Assert.That(q.ToList(), Is.EqualTo(new List<int> { 1, 2, 3 }));
    }

    [Test]
    public void Enqueue_ExceedsLimit_OldestDropped()
    {
        var q = new FixedSizeQueue<int> { Limit = 3 };
        q.Enqueue(1);
        q.Enqueue(2);
        q.Enqueue(3);
        q.Enqueue(4);
        Assert.That(q.ToList(), Is.EqualTo(new List<int> { 2, 3, 4 }));
    }

    [Test]
    public void Enqueue_FarExceedsLimit_OnlyRecentRetained()
    {
        var q = new FixedSizeQueue<int> { Limit = 2 };
        for (int i = 0; i < 100; i++)
            q.Enqueue(i);
        Assert.That(q.ToList(), Is.EqualTo(new List<int> { 98, 99 }));
    }

    [Test]
    public void ZeroLimit_AlwaysEmpty()
    {
        var q = new FixedSizeQueue<int> { Limit = 0 };
        q.Enqueue(1);
        q.Enqueue(2);
        Assert.That(q.ToList(), Is.Empty);
    }

    [Test]
    public void FIFOOrder_Maintained()
    {
        var q = new FixedSizeQueue<string> { Limit = 5 };
        q.Enqueue("a");
        q.Enqueue("b");
        q.Enqueue("c");
        var items = q.ToList();
        Assert.That(items[0], Is.EqualTo("a"));
        Assert.That(items[2], Is.EqualTo("c"));
    }
}

public class ContainerTest
{
    [Test]
    public void Inner_ReturnsWrappedValue()
    {
        var c = new Container<int>(42);
        Assert.That(c.Inner, Is.EqualTo(42));
    }

    [Test]
    public void ImplicitConversion_Works()
    {
        Container<string> c = "hello";
        Assert.That(c.Inner, Is.EqualTo("hello"));
    }
}

public class IdentityComparerTest
{
    [Test]
    public void Equals_SameReference_ReturnsTrue()
    {
        var obj = new object();
        Assert.That(IdentityComparer<object>.Instance.Equals(obj, obj), Is.True);
    }

    [Test]
    public void GetHashCode_UsesRuntimeIdentity()
    {
        var obj = new object();
        var hash = IdentityComparer<object>.Instance.GetHashCode(obj);
        Assert.That(hash, Is.EqualTo(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj)));
    }
}

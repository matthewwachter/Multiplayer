using Multiplayer.Common;

namespace Tests;

public class DeterministicHashTest
{
    [Test]
    public void HashCombineInt3_Deterministic()
    {
        var result1 = DeterministicHash.HashCombineInt(1, 2, 3);
        var result2 = DeterministicHash.HashCombineInt(1, 2, 3);
        Assert.That(result1, Is.EqualTo(result2));
    }

    [Test]
    public void HashCombineInt3_DifferentInputsDifferentOutput()
    {
        var result1 = DeterministicHash.HashCombineInt(1, 2, 3);
        var result2 = DeterministicHash.HashCombineInt(3, 2, 1);
        Assert.That(result1, Is.Not.EqualTo(result2));
    }

    [Test]
    public void HashCombineInt3_OrderMatters()
    {
        var result1 = DeterministicHash.HashCombineInt(1, 2, 3);
        var result2 = DeterministicHash.HashCombineInt(1, 3, 2);
        Assert.That(result1, Is.Not.EqualTo(result2));
    }

    [Test]
    public void HashCombineInt5_Deterministic()
    {
        var result1 = DeterministicHash.HashCombineInt(1, 2, 3, 4, 5);
        var result2 = DeterministicHash.HashCombineInt(1, 2, 3, 4, 5);
        Assert.That(result1, Is.EqualTo(result2));
    }

    [Test]
    public void HashCombineInt5_DifferentInputsDifferentOutput()
    {
        var result1 = DeterministicHash.HashCombineInt(1, 2, 3, 4, 5);
        var result2 = DeterministicHash.HashCombineInt(5, 4, 3, 2, 1);
        Assert.That(result1, Is.Not.EqualTo(result2));
    }

    [Test]
    public void HashCombineInt6_Deterministic()
    {
        var result1 = DeterministicHash.HashCombineInt(1, 2, 3, 4, 5, 6);
        var result2 = DeterministicHash.HashCombineInt(1, 2, 3, 4, 5, 6);
        Assert.That(result1, Is.EqualTo(result2));
    }

    [Test]
    public void HashCombineInt7_Deterministic()
    {
        var result1 = DeterministicHash.HashCombineInt(1, 2, 3, 4, 5, 6, 7);
        var result2 = DeterministicHash.HashCombineInt(1, 2, 3, 4, 5, 6, 7);
        Assert.That(result1, Is.EqualTo(result2));
    }

    [Test]
    public void HashCombineInt8_Deterministic()
    {
        var result1 = DeterministicHash.HashCombineInt(1, 2, 3, 4, 5, 6, 7, 8);
        var result2 = DeterministicHash.HashCombineInt(1, 2, 3, 4, 5, 6, 7, 8);
        Assert.That(result1, Is.EqualTo(result2));
    }

    [Test]
    public void HashCombineInt8_DifferentInputsDifferentOutput()
    {
        var result1 = DeterministicHash.HashCombineInt(1, 2, 3, 4, 5, 6, 7, 8);
        var result2 = DeterministicHash.HashCombineInt(8, 7, 6, 5, 4, 3, 2, 1);
        Assert.That(result1, Is.Not.EqualTo(result2));
    }

    [Test]
    public void DifferentOverloads_ProduceDifferentResults()
    {
        // Same first 3 values, but different overloads should produce different results
        var result3 = DeterministicHash.HashCombineInt(1, 2, 3);
        var result5 = DeterministicHash.HashCombineInt(1, 2, 3, 0, 0);
        Assert.That(result3, Is.Not.EqualTo(result5));
    }

    [Test]
    public void ZeroInputs_DoNotProduceZero()
    {
        var result = DeterministicHash.HashCombineInt(0, 0, 0);
        Assert.That(result, Is.Not.EqualTo(0));
    }
}

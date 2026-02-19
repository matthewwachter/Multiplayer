using Multiplayer.Common;

namespace Tests;

public class RoundModeTest
{
    [Test]
    public void GetCurrentRoundMode_ReturnsDefinedValue()
    {
        var mode = RoundMode.GetCurrentRoundMode();
        Assert.That(Enum.IsDefined(typeof(RoundModeEnum), mode), Is.True);
    }

    [Test]
    public void GetCurrentRoundMode_DefaultIsToNearest()
    {
        Assert.That(RoundMode.GetCurrentRoundMode(), Is.EqualTo(RoundModeEnum.ToNearest));
    }

    [Test]
    public void GetCurrentRoundMode_Deterministic()
    {
        var first = RoundMode.GetCurrentRoundMode();
        var second = RoundMode.GetCurrentRoundMode();
        Assert.That(first, Is.EqualTo(second));
    }
}

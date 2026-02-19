using System.Text.RegularExpressions;
using Multiplayer.Common;

namespace Tests;

public class VersionTest
{
    [Test]
    public void SimpleVersion_MatchesSemverFormat()
    {
        Assert.That(MpVersion.SimpleVersion, Does.Match(@"^\d+\.\d+\.\d+$"));
    }

    [Test]
    public void Protocol_IsPositive()
    {
        Assert.That(MpVersion.Protocol, Is.GreaterThan(0));
    }

    [Test]
    public void Version_StartsWithSimpleVersion()
    {
        Assert.That(MpVersion.Version, Does.StartWith(MpVersion.SimpleVersion));
    }

    [Test]
    public void ApiAssemblyName_IsCorrect()
    {
        Assert.That(MpVersion.ApiAssemblyName, Is.EqualTo("0MultiplayerAPI"));
    }

    [Test]
    public void GitHash_NullOrHexString()
    {
        if (MpVersion.GitHash != null)
            Assert.That(MpVersion.GitHash, Does.Match(@"^[0-9a-fA-F]+$"));
        else
            Assert.Pass("GitHash is null (expected in test environment)");
    }
}

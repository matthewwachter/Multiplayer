using System.Net;
using Multiplayer.Common.Util;

namespace Tests;

public class EndpointsTest
{
    [Test]
    public void IPv4_WithPort()
    {
        Assert.That(Endpoints.TryParse("127.0.0.1:8000", 0, out var ep), Is.True);
        Assert.That(ep!.Address, Is.EqualTo(IPAddress.Loopback));
        Assert.That(ep.Port, Is.EqualTo(8000));
    }

    [Test]
    public void IPv4_DefaultPort()
    {
        Assert.That(Endpoints.TryParse("127.0.0.1", 9999, out var ep), Is.True);
        Assert.That(ep!.Address, Is.EqualTo(IPAddress.Loopback));
        Assert.That(ep.Port, Is.EqualTo(9999));
    }

    [Test]
    public void IPv4_ZeroPort()
    {
        Assert.That(Endpoints.TryParse("0.0.0.0:0", 0, out var ep), Is.True);
        Assert.That(ep!.Address, Is.EqualTo(IPAddress.Any));
        Assert.That(ep.Port, Is.EqualTo(0));
    }

    [Test]
    public void IPv6_WithBracketsAndPort()
    {
        Assert.That(Endpoints.TryParse("[::1]:8000", 0, out var ep), Is.True);
        Assert.That(ep!.Address, Is.EqualTo(IPAddress.IPv6Loopback));
        Assert.That(ep.Port, Is.EqualTo(8000));
    }

    [Test]
    public void IPv6_Bare_DefaultPort()
    {
        Assert.That(Endpoints.TryParse("::1", 5555, out var ep), Is.True);
        Assert.That(ep!.Address, Is.EqualTo(IPAddress.IPv6Loopback));
        Assert.That(ep.Port, Is.EqualTo(5555));
    }

    [Test]
    public void Whitespace_Trimmed()
    {
        Assert.That(Endpoints.TryParse("  127.0.0.1:8000  ", 0, out var ep), Is.True);
        Assert.That(ep!.Address, Is.EqualTo(IPAddress.Loopback));
        Assert.That(ep.Port, Is.EqualTo(8000));
    }

    [Test]
    public void Invalid_Format()
    {
        Assert.That(Endpoints.TryParse("not-an-ip", 0, out _), Is.False);
    }

    [Test]
    public void Port_OutOfRange()
    {
        Assert.That(Endpoints.TryParse("127.0.0.1:99999", 0, out _), Is.False);
    }

    [Test]
    public void Empty_String()
    {
        Assert.That(Endpoints.TryParse("", 0, out _), Is.False);
    }
}

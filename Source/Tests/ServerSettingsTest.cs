using Multiplayer.Common;

namespace Tests;

public class ServerSettingsTest
{
    [Test]
    public void SingleEndpoint()
    {
        var settings = new ServerSettings { directAddress = "127.0.0.1:30502" };
        string? error = settings.TryParseEndpoints(out var endpoints);
        Assert.That(error, Is.Null);
        Assert.That(endpoints, Has.Length.EqualTo(1));
        Assert.That(endpoints[0].Port, Is.EqualTo(30502));
    }

    [Test]
    public void MultipleEndpoints()
    {
        var settings = new ServerSettings { directAddress = "127.0.0.1:30502&0.0.0.0:30503" };
        string? error = settings.TryParseEndpoints(out var endpoints);
        Assert.That(error, Is.Null);
        Assert.That(endpoints, Has.Length.EqualTo(2));
        Assert.That(endpoints[0].Port, Is.EqualTo(30502));
        Assert.That(endpoints[1].Port, Is.EqualTo(30503));
    }

    [Test]
    public void BadEndpoint_ReturnsError()
    {
        var settings = new ServerSettings { directAddress = "bad" };
        string? error = settings.TryParseEndpoints(out _);
        Assert.That(error, Is.EqualTo("bad"));
    }

    [Test]
    public void SecondEndpointBad_ReturnsError()
    {
        var settings = new ServerSettings { directAddress = "127.0.0.1:30502&bad" };
        string? error = settings.TryParseEndpoints(out var endpoints);
        Assert.That(error, Is.EqualTo("bad"));
        Assert.That(endpoints[0].Port, Is.EqualTo(30502));
    }

    [Test]
    public void DefaultAddress_ParsesSuccessfully()
    {
        var settings = new ServerSettings();
        string? error = settings.TryParseEndpoints(out var endpoints);
        Assert.That(error, Is.Null);
        Assert.That(endpoints, Has.Length.EqualTo(1));
        Assert.That(endpoints[0].Port, Is.EqualTo(MultiplayerServer.DefaultPort));
    }
}

using Multiplayer.Common;

namespace Tests;

public class ConnectionStateEnumTest
{
    [TestCase(ConnectionStateEnum.ClientJoining)]
    [TestCase(ConnectionStateEnum.ClientLoading)]
    [TestCase(ConnectionStateEnum.ClientPlaying)]
    [TestCase(ConnectionStateEnum.ClientSteam)]
    public void IsClient_True(ConnectionStateEnum state)
    {
        Assert.That(state.IsClient(), Is.True);
        Assert.That(state.IsServer(), Is.False);
    }

    [TestCase(ConnectionStateEnum.ServerJoining)]
    [TestCase(ConnectionStateEnum.ServerLoading)]
    [TestCase(ConnectionStateEnum.ServerPlaying)]
    [TestCase(ConnectionStateEnum.ServerSteam)]
    public void IsServer_True(ConnectionStateEnum state)
    {
        Assert.That(state.IsServer(), Is.True);
        Assert.That(state.IsClient(), Is.False);
    }

    [TestCase(ConnectionStateEnum.Count)]
    [TestCase(ConnectionStateEnum.Disconnected)]
    public void NeitherClientNorServer(ConnectionStateEnum state)
    {
        Assert.That(state.IsClient(), Is.False);
        Assert.That(state.IsServer(), Is.False);
    }
}

using Multiplayer.Common;

namespace Tests;

public class ScheduledCommandTest
{
    [Test]
    public void SingleCommand_Roundtrip()
    {
        var cmd = new ScheduledCommand(
            CommandType.Sync, 1000, 2, 3, 4, [0xAA, 0xBB]);

        byte[] serialized = ScheduledCommand.Serialize(cmd);
        var deserialized = ScheduledCommand.Deserialize(new ByteReader(serialized));

        Assert.That(deserialized.type, Is.EqualTo(CommandType.Sync));
        Assert.That(deserialized.ticks, Is.EqualTo(1000));
        Assert.That(deserialized.factionId, Is.EqualTo(2));
        Assert.That(deserialized.mapId, Is.EqualTo(3));
        Assert.That(deserialized.playerId, Is.EqualTo(4));
        Assert.That(deserialized.data, Is.EqualTo(new byte[] { 0xAA, 0xBB }));
    }

    [Test]
    public void SentinelValues()
    {
        var cmd = new ScheduledCommand(
            CommandType.GlobalTimeSpeed,
            0,
            ScheduledCommand.NoFaction,
            ScheduledCommand.Global,
            ScheduledCommand.NoPlayer,
            []);

        byte[] serialized = ScheduledCommand.Serialize(cmd);
        var deserialized = ScheduledCommand.Deserialize(new ByteReader(serialized));

        Assert.That(deserialized.factionId, Is.EqualTo(-1));
        Assert.That(deserialized.mapId, Is.EqualTo(-1));
        Assert.That(deserialized.playerId, Is.EqualTo(-1));
        Assert.That(deserialized.data, Is.Empty);
    }

    [Test]
    public void BoundaryValues()
    {
        var cmd = new ScheduledCommand(
            CommandType.MapTimeSpeed,
            int.MaxValue,
            int.MinValue,
            int.MaxValue,
            int.MinValue,
            []);

        byte[] serialized = ScheduledCommand.Serialize(cmd);
        var deserialized = ScheduledCommand.Deserialize(new ByteReader(serialized));

        Assert.That(deserialized.ticks, Is.EqualTo(int.MaxValue));
        Assert.That(deserialized.factionId, Is.EqualTo(int.MinValue));
        Assert.That(deserialized.mapId, Is.EqualTo(int.MaxValue));
        Assert.That(deserialized.playerId, Is.EqualTo(int.MinValue));
    }

    [Test]
    public void BatchRoundtrip()
    {
        var cmds = new List<ScheduledCommand>
        {
            new(CommandType.Sync, 100, 1, 2, 3, [0x01]),
            new(CommandType.Designator, 200, 4, 5, 6, [0x02, 0x03]),
            new(CommandType.PauseAll, 300, -1, -1, -1, []),
        };

        byte[] serialized = ScheduledCommand.SerializeCmds(cmds);
        var deserialized = ScheduledCommand.DeserializeCmds(serialized);

        Assert.That(deserialized, Has.Count.EqualTo(3));

        Assert.That(deserialized[0].type, Is.EqualTo(CommandType.Sync));
        Assert.That(deserialized[0].ticks, Is.EqualTo(100));
        Assert.That(deserialized[0].data, Is.EqualTo(new byte[] { 0x01 }));

        Assert.That(deserialized[1].type, Is.EqualTo(CommandType.Designator));
        Assert.That(deserialized[1].ticks, Is.EqualTo(200));
        Assert.That(deserialized[1].factionId, Is.EqualTo(4));

        Assert.That(deserialized[2].type, Is.EqualTo(CommandType.PauseAll));
        Assert.That(deserialized[2].data, Is.Empty);
    }

    [Test]
    public void BatchEmpty()
    {
        byte[] serialized = ScheduledCommand.SerializeCmds([]);
        var deserialized = ScheduledCommand.DeserializeCmds(serialized);
        Assert.That(deserialized, Is.Empty);
    }

    [Test]
    public void BatchSingle()
    {
        var cmds = new List<ScheduledCommand>
        {
            new(CommandType.DebugTools, 42, 1, 2, 3, [0xFF]),
        };

        byte[] serialized = ScheduledCommand.SerializeCmds(cmds);
        var deserialized = ScheduledCommand.DeserializeCmds(serialized);

        Assert.That(deserialized, Has.Count.EqualTo(1));
        Assert.That(deserialized[0].type, Is.EqualTo(CommandType.DebugTools));
        Assert.That(deserialized[0].ticks, Is.EqualTo(42));
    }

    [Test]
    public void AllCommandTypes_Roundtrip()
    {
        foreach (CommandType cmdType in Enum.GetValues<CommandType>())
        {
            var cmd = new ScheduledCommand(cmdType, 1, 2, 3, 4, []);
            byte[] serialized = ScheduledCommand.Serialize(cmd);
            var deserialized = ScheduledCommand.Deserialize(new ByteReader(serialized));
            Assert.That(deserialized.type, Is.EqualTo(cmdType), $"Failed for {cmdType}");
        }
    }
}

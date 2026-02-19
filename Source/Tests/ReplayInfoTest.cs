using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Multiplayer.Common;

namespace Tests;

public class ReplayInfoTest
{
    [Test]
    public void Name_Roundtrip()
    {
        var replayInfo = new ReplayInfo
        {
            name = "test"
        };

        var xml = ReplayInfo.Write(replayInfo);
        Console.WriteLine(Encoding.UTF8.GetString(xml));

        var readInfo = ReplayInfo.Read(xml);
        Assert.That(readInfo.name, Is.EqualTo(replayInfo.name));
    }

    [Test]
    public void AllScalarFields_Roundtrip()
    {
        var info = new ReplayInfo
        {
            name = "my_replay",
            protocol = 42,
            playerFaction = 7,
            spectatorFaction = 3,
            rwVersion = "1.6.4566",
            multifaction = true,
        };

        var read = ReplayInfo.Read(ReplayInfo.Write(info));

        Assert.That(read.name, Is.EqualTo("my_replay"));
        Assert.That(read.protocol, Is.EqualTo(42));
        Assert.That(read.playerFaction, Is.EqualTo(7));
        Assert.That(read.spectatorFaction, Is.EqualTo(3));
        Assert.That(read.rwVersion, Is.EqualTo("1.6.4566"));
        Assert.That(read.multifaction, Is.True);
    }

    [Test]
    public void Sections_Roundtrip()
    {
        var info = new ReplayInfo
        {
            name = "s",
            sections = new List<ReplaySection>
            {
                new(0, 100),
                new(100, 500),
            },
        };

        var read = ReplayInfo.Read(ReplayInfo.Write(info));

        Assert.That(read.sections, Has.Count.EqualTo(2));
        Assert.That(read.sections[0].start, Is.EqualTo(0));
        Assert.That(read.sections[0].end, Is.EqualTo(100));
        Assert.That(read.sections[1].start, Is.EqualTo(100));
        Assert.That(read.sections[1].end, Is.EqualTo(500));
    }

    [Test]
    public void ModLists_Roundtrip()
    {
        var info = new ReplayInfo
        {
            name = "m",
            modIds = new List<string> { "core", "multiplayer", "hugslib" },
            modNames = new List<string> { "Core", "Multiplayer", "HugsLib" },
        };

        var read = ReplayInfo.Read(ReplayInfo.Write(info));

        Assert.That(read.modIds, Is.EqualTo(new[] { "core", "multiplayer", "hugslib" }));
        Assert.That(read.modNames, Is.EqualTo(new[] { "Core", "Multiplayer", "HugsLib" }));
    }

    [Test]
    public void AsyncTime_Roundtrip()
    {
        var info = new ReplayInfo
        {
            name = "a",
            asyncTime = true,
        };

        var read = ReplayInfo.Read(ReplayInfo.Write(info));
        Assert.That((bool)read.asyncTime, Is.True);
    }

    [Test]
    public void AsyncTime_False_Roundtrip()
    {
        var info = new ReplayInfo
        {
            name = "a",
            asyncTime = false,
        };

        var read = ReplayInfo.Read(ReplayInfo.Write(info));
        Assert.That((bool)read.asyncTime, Is.False);
    }

    [TestCase("true", true)]
    [TestCase("True", true)]
    [TestCase("TRUE", true)]
    [TestCase("yes", true)]
    [TestCase("Yes", true)]
    [TestCase("y", true)]
    [TestCase("Y", true)]
    [TestCase("false", false)]
    [TestCase("False", false)]
    [TestCase("no", false)]
    [TestCase("anything-else", false)]
    public void XmlBool_CaseInsensitive(string xmlValue, bool expected)
    {
        // Build XML with the given value for asyncTime
        string xml = $"<ReplayInfo><name>x</name><asyncTime>{xmlValue}</asyncTime></ReplayInfo>";
        var read = ReplayInfo.Read(Encoding.UTF8.GetBytes(xml));
        Assert.That((bool)read.asyncTime, Is.EqualTo(expected));
    }

    [Test]
    public void Multifaction_Roundtrip()
    {
        var info = new ReplayInfo { name = "mf", multifaction = true };
        var read = ReplayInfo.Read(ReplayInfo.Write(info));
        Assert.That(read.multifaction, Is.True);

        info.multifaction = false;
        read = ReplayInfo.Read(ReplayInfo.Write(info));
        Assert.That(read.multifaction, Is.False);
    }

    [Test]
    public void RwVersion_Roundtrip()
    {
        var info = new ReplayInfo { name = "v", rwVersion = "1.5.9999" };
        var read = ReplayInfo.Read(ReplayInfo.Write(info));
        Assert.That(read.rwVersion, Is.EqualTo("1.5.9999"));
    }
}

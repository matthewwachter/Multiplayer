using System.IO.Compression;
using System.Text;
using Multiplayer.Common.Util;

namespace Tests;

public class ZipExtensionsTest
{
    [Test]
    public void AddEntry_Bytes_GetBytes_Roundtrip()
    {
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        using var ms = new MemoryStream();

        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            zip.AddEntry("test.bin", bytes);

        ms.Position = 0;
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Read))
            Assert.That(zip.GetBytes("test.bin"), Is.EqualTo(bytes));
    }

    [Test]
    public void AddEntry_String_GetString_Roundtrip()
    {
        using var ms = new MemoryStream();

        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            zip.AddEntry("test.txt", "hello world");

        ms.Position = 0;
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Read))
            Assert.That(zip.GetString("test.txt"), Is.EqualTo("hello world"));
    }

    [Test]
    public void MultipleEntries_RetrievedIndependently()
    {
        using var ms = new MemoryStream();

        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            zip.AddEntry("a.txt", "alpha");
            zip.AddEntry("b.txt", "beta");
            zip.AddEntry("c.txt", "gamma");
        }

        ms.Position = 0;
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Read))
        {
            Assert.That(zip.GetString("a.txt"), Is.EqualTo("alpha"));
            Assert.That(zip.GetString("b.txt"), Is.EqualTo("beta"));
            Assert.That(zip.GetString("c.txt"), Is.EqualTo("gamma"));
        }
    }

    [Test]
    public void GetEntries_WildcardPattern_MatchesCorrectEntries()
    {
        using var ms = new MemoryStream();

        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            zip.AddEntry("maps/map1.xml", "data1");
            zip.AddEntry("maps/map2.xml", "data2");
            zip.AddEntry("other/file.xml", "data3");
        }

        ms.Position = 0;
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Read))
        {
            var entries = zip.GetEntries("maps/*").Select(e => e.FullName).ToList();
            Assert.That(entries, Has.Count.EqualTo(2));
            Assert.That(entries, Does.Contain("maps/map1.xml"));
            Assert.That(entries, Does.Contain("maps/map2.xml"));
        }
    }

    [Test]
    public void GetEntries_NoMatch_ReturnsEmpty()
    {
        using var ms = new MemoryStream();

        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            zip.AddEntry("data.txt", "content");

        ms.Position = 0;
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Read))
        {
            var entries = zip.GetEntries("nonexistent/*").ToList();
            Assert.That(entries, Is.Empty);
        }
    }

    [Test]
    public void UnicodeString_Roundtrip()
    {
        var unicode = "\u00e4\u00f6\u00fc\u00df \u4e16\u754c \ud83c\udf0d";
        using var ms = new MemoryStream();

        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            zip.AddEntry("unicode.txt", unicode);

        ms.Position = 0;
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Read))
            Assert.That(zip.GetString("unicode.txt"), Is.EqualTo(unicode));
    }
}

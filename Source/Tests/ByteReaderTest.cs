using Multiplayer.Common;

namespace Tests;

public class ByteReaderTest
{
    private static ByteReader ReaderFrom(Action<ByteWriter> write)
    {
        var writer = new ByteWriter();
        write(writer);
        return new ByteReader(writer.ToArray());
    }

    [Test]
    public void RoundtripByte()
    {
        var reader = ReaderFrom(w => w.WriteByte(42));
        Assert.That(reader.ReadByte(), Is.EqualTo((byte)42));
    }

    [Test]
    public void RoundtripShort()
    {
        var reader = ReaderFrom(w => w.WriteShort(12345));
        Assert.That(reader.ReadShort(), Is.EqualTo((short)12345));
    }

    [Test]
    public void RoundtripUShort()
    {
        var reader = ReaderFrom(w => w.WriteUShort(60000));
        Assert.That(reader.ReadUShort(), Is.EqualTo((ushort)60000));
    }

    [Test]
    public void RoundtripInt()
    {
        var reader = ReaderFrom(w => w.WriteInt32(-123456));
        Assert.That(reader.ReadInt32(), Is.EqualTo(-123456));
    }

    [Test]
    public void RoundtripLong()
    {
        var reader = ReaderFrom(w => w.WriteLong(long.MaxValue));
        Assert.That(reader.ReadLong(), Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void RoundtripFloat()
    {
        var reader = ReaderFrom(w => w.WriteFloat(3.14f));
        Assert.That(reader.ReadFloat(), Is.EqualTo(3.14f));
    }

    [Test]
    public void RoundtripDouble()
    {
        var reader = ReaderFrom(w => w.WriteDouble(2.718281828));
        Assert.That(reader.ReadDouble(), Is.EqualTo(2.718281828));
    }

    [Test]
    public void RoundtripBool()
    {
        var reader = ReaderFrom(w => w.WriteBool(true));
        Assert.That(reader.ReadBool(), Is.True);
    }

    [Test]
    public void OutOfBounds_EmptyArray()
    {
        var reader = new ByteReader([]);
        var ex = Assert.Throws<ReaderException>(() => reader.ReadByte());
        Assert.That(ex!.Message, Does.Contain("position=0"));
        Assert.That(ex.Message, Does.Contain("size=1"));
        Assert.That(ex.Message, Does.Contain("length=0"));
    }

    [Test]
    public void OutOfBounds_TruncatedData()
    {
        // Write an int (4 bytes) but only provide 2 bytes
        var reader = new ByteReader([0x01, 0x02]);
        var ex = Assert.Throws<ReaderException>(() => reader.ReadInt32());
        Assert.That(ex!.Message, Does.Contain("position=0"));
        Assert.That(ex.Message, Does.Contain("size=4"));
        Assert.That(ex.Message, Does.Contain("length=2"));
    }

    [Test]
    public void OutOfBounds_SequentialReadsExceedBuffer()
    {
        // Write a single int (4 bytes), then try to read two
        var reader = ReaderFrom(w => w.WriteInt32(1));
        reader.ReadInt32(); // First read succeeds
        Assert.Throws<ReaderException>(() => reader.ReadInt32());
    }

    [Test]
    public void OutOfBounds_ReadLongFromShortBuffer()
    {
        // 4 bytes available, try to read a long (8 bytes)
        var reader = ReaderFrom(w => w.WriteInt32(1));
        Assert.Throws<ReaderException>(() => reader.ReadLong());
    }

    [Test]
    public void SequentialReads_AllSucceed()
    {
        var reader = ReaderFrom(w =>
        {
            w.WriteByte(1);
            w.WriteShort(2);
            w.WriteInt32(3);
        });

        Assert.That(reader.ReadByte(), Is.EqualTo((byte)1));
        Assert.That(reader.ReadShort(), Is.EqualTo((short)2));
        Assert.That(reader.ReadInt32(), Is.EqualTo(3));
    }
}

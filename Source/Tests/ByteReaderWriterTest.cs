using Multiplayer.Common;

namespace Tests;

public class ByteReaderWriterTest
{
    private static ByteReader ReaderFrom(Action<ByteWriter> write)
    {
        var writer = new ByteWriter();
        write(writer);
        return new ByteReader(writer.ToArray());
    }

    // --- String tests ---

    [Test]
    public void String_Roundtrip()
    {
        var reader = ReaderFrom(w => w.WriteString("hello"));
        Assert.That(reader.ReadString(), Is.EqualTo("hello"));
    }

    [Test]
    public void String_Empty()
    {
        var reader = ReaderFrom(w => w.WriteString(""));
        Assert.That(reader.ReadString(), Is.EqualTo(""));
    }

    [Test]
    public void String_Unicode()
    {
        var reader = ReaderFrom(w => w.WriteString("\u00e9\u00e0\u00fc\ud83d\ude00"));
        Assert.That(reader.ReadString(), Is.EqualTo("\u00e9\u00e0\u00fc\ud83d\ude00"));
    }

    [Test]
    public void StringNullable_Null()
    {
        var reader = ReaderFrom(w => w.WriteString(null));
        Assert.That(reader.ReadStringNullable(), Is.Null);
    }

    [Test]
    public void StringNullable_NonNull()
    {
        var reader = ReaderFrom(w => w.WriteString("abc"));
        Assert.That(reader.ReadStringNullable(), Is.EqualTo("abc"));
    }

    [Test]
    public void String_ExceedingMaxLen_Throws()
    {
        var reader = ReaderFrom(w => w.WriteString("abcdef"));
        Assert.Throws<ReaderException>(() => reader.ReadString(maxLen: 3));
    }

    [Test]
    public void String_NegativeByteLength_Throws()
    {
        // Manually write a negative length that isn't -1
        var reader = ReaderFrom(w => w.WriteInt32(-5));
        Assert.Throws<ReaderException>(() => reader.ReadString());
    }

    [Test]
    public void StringNullable_NegativeByteLength_Throws()
    {
        var reader = ReaderFrom(w => w.WriteInt32(-2));
        Assert.Throws<ReaderException>(() => reader.ReadStringNullable());
    }

    // --- PrefixedBytes tests ---

    [Test]
    public void PrefixedBytes_Roundtrip()
    {
        byte[] data = [1, 2, 3, 4, 5];
        var reader = ReaderFrom(w => w.WritePrefixedBytes(data));
        Assert.That(reader.ReadPrefixedBytes(), Is.EqualTo(data));
    }

    [Test]
    public void PrefixedBytes_Null()
    {
        var reader = ReaderFrom(w => w.WritePrefixedBytes(null));
        Assert.That(reader.ReadPrefixedBytes(), Is.Null);
    }

    [Test]
    public void PrefixedBytes_Empty()
    {
        var reader = ReaderFrom(w => w.WritePrefixedBytes([]));
        Assert.That(reader.ReadPrefixedBytes(), Is.EqualTo(Array.Empty<byte>()));
    }

    [Test]
    public void PrefixedBytes_DataIntegrity()
    {
        byte[] data = [0x00, 0xFF, 0x80, 0x7F, 0x01];
        var reader = ReaderFrom(w => w.WritePrefixedBytes(data));
        Assert.That(reader.ReadPrefixedBytes(), Is.EqualTo(data));
    }

    // --- PrefixedInts tests ---

    [Test]
    public void PrefixedInts_Roundtrip()
    {
        int[] data = [1, -1, 0, int.MaxValue, int.MinValue];
        var reader = ReaderFrom(w => w.WritePrefixedInts(data));
        Assert.That(reader.ReadPrefixedInts(), Is.EqualTo(data));
    }

    [Test]
    public void PrefixedInts_Empty()
    {
        var reader = ReaderFrom(w => w.WritePrefixedInts([]));
        Assert.That(reader.ReadPrefixedInts(), Is.EqualTo(Array.Empty<int>()));
    }

    [Test]
    public void PrefixedInts_NegativeLength_Throws()
    {
        var reader = ReaderFrom(w => w.WriteInt32(-1));
        Assert.Throws<ReaderException>(() => reader.ReadPrefixedInts());
    }

    // --- PrefixedUInts tests ---

    [Test]
    public void PrefixedUInts_Roundtrip()
    {
        uint[] data = [0, 1, uint.MaxValue, 42];
        var reader = ReaderFrom(w => w.WritePrefixedUInts(data));
        Assert.That(reader.ReadPrefixedUInts(), Is.EqualTo(data));
    }

    // --- PrefixedULongs tests ---

    [Test]
    public void PrefixedULongs_Roundtrip()
    {
        ulong[] data = [0, 1, ulong.MaxValue, 123456789UL];
        var reader = ReaderFrom(w =>
        {
            w.WriteInt32(data.Length);
            foreach (var v in data) w.WriteULong(v);
        });
        Assert.That(reader.ReadPrefixedULongs(), Is.EqualTo(data));
    }

    // --- PrefixedStrings tests ---

    [Test]
    public void PrefixedStrings_Roundtrip()
    {
        string[] data = ["alpha", "beta", "gamma"];
        var reader = ReaderFrom(w =>
        {
            w.WriteInt32(data.Length);
            foreach (var s in data) w.WriteString(s);
        });
        Assert.That(reader.ReadPrefixedStrings(), Is.EqualTo(data));
    }

    // --- Enum tests ---

    private enum TestIntEnum : int
    {
        A = 0,
        B = 100,
        C = -1
    }

    private enum TestShortEnum : short
    {
        X = -32768,
        Y = 0,
        Z = 32767
    }

    [Test]
    public void Enum_ByteBacked_Roundtrip()
    {
        var reader = ReaderFrom(w => w.WriteEnum(CommandType.Sync));
        Assert.That(reader.ReadEnum<CommandType>(), Is.EqualTo(CommandType.Sync));
    }

    [Test]
    public void Enum_ByteBacked_AllValues()
    {
        foreach (CommandType val in Enum.GetValues<CommandType>())
        {
            var reader = ReaderFrom(w => w.WriteEnum(val));
            Assert.That(reader.ReadEnum<CommandType>(), Is.EqualTo(val));
        }
    }

    [Test]
    public void Enum_IntBacked_Roundtrip()
    {
        var reader = ReaderFrom(w => w.WriteEnum(TestIntEnum.C));
        Assert.That(reader.ReadEnum<TestIntEnum>(), Is.EqualTo(TestIntEnum.C));
    }

    [Test]
    public void Enum_ShortBacked_Roundtrip()
    {
        var reader = ReaderFrom(w => w.WriteEnum(TestShortEnum.X));
        Assert.That(reader.ReadEnum<TestShortEnum>(), Is.EqualTo(TestShortEnum.X));
    }

    [Test]
    public void Enum_ConnectionState_Roundtrip()
    {
        var reader = ReaderFrom(w => w.WriteEnum(ConnectionStateEnum.ClientPlaying));
        Assert.That(reader.ReadEnum<ConnectionStateEnum>(), Is.EqualTo(ConnectionStateEnum.ClientPlaying));
    }

    // --- Position/Seek tests ---

    [Test]
    public void PeekByte_DoesNotAdvancePosition()
    {
        var reader = ReaderFrom(w => { w.WriteByte(42); w.WriteByte(99); });
        byte peeked = reader.PeekByte();
        Assert.That(peeked, Is.EqualTo((byte)42));
        Assert.That(reader.Position, Is.EqualTo(0));
        Assert.That(reader.ReadByte(), Is.EqualTo((byte)42));
    }

    [Test]
    public void Seek_Repositions()
    {
        var reader = ReaderFrom(w => { w.WriteInt32(111); w.WriteInt32(222); });
        reader.ReadInt32(); // skip first
        reader.Seek(0);
        Assert.That(reader.ReadInt32(), Is.EqualTo(111));
    }

    [Test]
    public void Left_Property()
    {
        var reader = ReaderFrom(w => { w.WriteByte(1); w.WriteByte(2); w.WriteByte(3); });
        Assert.That(reader.Left, Is.EqualTo(3));
        reader.ReadByte();
        Assert.That(reader.Left, Is.EqualTo(2));
        reader.ReadByte();
        Assert.That(reader.Left, Is.EqualTo(1));
        reader.ReadByte();
        Assert.That(reader.Left, Is.EqualTo(0));
    }

    // --- GetBytes tests ---

    [Test]
    public void GetBytes_MixedTypes()
    {
        byte[] result = ByteWriter.GetBytes(42, true, "hi", CommandType.Sync, new byte[] { 0xAA });

        var reader = new ByteReader(result);
        Assert.That(reader.ReadInt32(), Is.EqualTo(42));
        Assert.That(reader.ReadBool(), Is.True);
        Assert.That(reader.ReadString(), Is.EqualTo("hi"));
        Assert.That(reader.ReadEnum<CommandType>(), Is.EqualTo(CommandType.Sync));
        Assert.That(reader.ReadPrefixedBytes(), Is.EqualTo(new byte[] { 0xAA }));
        Assert.That(reader.Left, Is.EqualTo(0));
    }
}

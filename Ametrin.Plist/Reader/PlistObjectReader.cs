namespace Ametrin.Plist.Reader;

public readonly struct PlistObjectReader
{
    internal PlistDocument Document { get; }
    public PlistObjectType Type => Info.Type;
    internal PlistObjectInfo Info { get; }
    private readonly int _index;

    internal PlistObjectReader(PlistDocument document, int index, PlistObjectInfo info)
    {
        Document = document;
        Info = info;

        _index = index;
    }

    internal readonly int GetOffset() => Document.offsetTable[_index] + Info.MarkerSize;
    internal readonly ReadOnlySpan<byte> GetSpan() => Document.data.AsSpan(GetOffset()..);
    internal readonly ReadOnlySpan<byte> GetSpan(int count) => Document.data.AsSpan(GetOffset(), count);

    public readonly int GetInt32()
    {
        if (Type is not PlistObjectType.VarInt) throw new InvalidOperationException($"Expected a int, got a {Type}");
        if (Info.ContainerCount > 4) throw new OverflowException("");
        return (int)ReadUIntVarBigEndian(GetSpan(Info.ContainerCount));
    }
    public readonly ulong GetUInt64()
    {
        if (Type is not PlistObjectType.VarInt) throw new InvalidOperationException($"Expected a int, got a {Type}");
        return ReadUIntVarBigEndian(GetSpan(Info.ContainerCount));
    }
    public readonly double GetDouble()
    {
        if (Type is not PlistObjectType.VarReal) throw new InvalidOperationException($"Expected a real, got a {Type}");

        return Info.ContainerCount switch
        {
            4 => ReadFloatBigEndian(GetSpan(4)),
            8 => ReadDoubleBigEndian(GetSpan(8)),
            _ => throw new InvalidOperationException($"Unsupported float size {Info.ContainerCount}"), 
        };
    }

    public readonly string GetString() => Type switch
    {
        PlistObjectType.ASCIIString => Encoding.ASCII.GetString(GetSpan(Info.ContainerCount)),
        PlistObjectType.UTF16String => Encoding.BigEndianUnicode.GetString(GetSpan(Info.ContainerCount * 2)),
        _ => throw new InvalidOperationException($"Expected a String, got a {Type}"),
    };

    public ReadOnlySpan<byte> GetBytes()
    {
        if (Type is not PlistObjectType.UID and not PlistObjectType.ByteArray) throw new InvalidOperationException($"Expected a UID, got a {Type}");
        return GetSpan(Info.ContainerCount);
    }

    public readonly PlistArrayReader GetArray() => new(Document, _index, Info);
    public readonly PlistDictionaryReader GetDictionary() => new(Document, _index, Info);

    public PlistObjectReader() { throw new InvalidOperationException(); }
}

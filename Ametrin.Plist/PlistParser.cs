using System.IO;
using System.Runtime.InteropServices;

namespace Ametrin.Plist;

public static class PlistParser
{
    public static bool IsPlist(ReadOnlySpan<byte> data)
    {
        return data is [0x62, 0x70, 0x6C, 0x69, 0x73, 0x74, 0x30, 0x30, ..]; // bplist00
    }

    public static PlistDocument ToDocument(ImmutableArray<byte> data)
    {
        if (data.Length < 8)
        {
            throw new ArgumentException("Data too short to be a valid binary plist.", nameof(data));
        }

        if (!IsPlist(data.AsSpan()))
        {
            throw new ArgumentException("Invalid binary plist header (missing 'bplist00').", nameof(data));
        }

        return ToDocumentImpl(data);
    }

    public static PlistDocument ToDocument(Stream stream)
    {
        if (stream.Length < 8)
        {
            throw new ArgumentException("Data too short to be a valid binary plist.", nameof(stream));
        }

        Span<byte> headerBuffer = stackalloc byte[8];
        stream.ReadExactly(headerBuffer);

        if (IsPlist(headerBuffer))
        {
            throw new ArgumentException("Invalid binary plist header (missing 'bplist00').", nameof(stream));
        }

        var data = new byte[stream.Length];

        headerBuffer.CopyTo(data);
        stream.ReadExactly(data.AsSpan(8..));

        return ToDocumentImpl(ImmutableCollectionsMarshal.AsImmutableArray(data));
    }

    private static PlistDocument ToDocumentImpl(ImmutableArray<byte> data)
    {
        // 32 byte info trailer at the end
        var trailer = PlistTrailer.FromBinary(data.AsSpan(data.Length - 32, 32));

        var offsetTable = ParseOffsetTable(data.AsSpan(trailer.OffsetTablePosition, trailer.OffsetIntSize * trailer.ObjectCount), trailer.ObjectCount, trailer.OffsetIntSize);

        return new(data, offsetTable, trailer);
    }

    private static ImmutableArray<int> ParseOffsetTable(ReadOnlySpan<byte> offsetTable, int objectCount, int offsetIntSize)
    {
        Debug.Assert(offsetIntSize <= 4);

        var offsets = new int[objectCount];
        for (var i = 0; i < objectCount; i++)
        {
            offsets[i] = (int)ReadUIntVarBigEndian(offsetTable.Slice(i * offsetIntSize, offsetIntSize));
        }
        return ImmutableCollectionsMarshal.AsImmutableArray(offsets);
    }
}
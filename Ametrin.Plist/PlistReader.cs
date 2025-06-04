using System.IO;
using System.Runtime.InteropServices;
using Ametrin.Plist.Reader;

namespace Ametrin.Plist;

public static class PlistReader
{
    public static bool HasPlistHeader(ReadOnlySpan<byte> data)
    {
        return data is [0x62, 0x70, 0x6C, 0x69, 0x73, 0x74, 0x30, 0x30, ..]; // bplist00
    }

    public static PlistDocument ToDocument(ImmutableArray<byte> data)
    {
        if (!HasPlistHeader(data.AsSpan()))
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

        if (HasPlistHeader(headerBuffer))
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

    public static string ToDisplayString(this in PlistObjectReader reader)
    {
        var sb = new StringBuilder();
        Impl(reader, sb, 1);
        return sb.ToString();

        static void Impl(in PlistObjectReader reader, StringBuilder sb, int indent)
        {
            switch (reader.Type)
            {
                case PlistObjectType.Null:
                    sb.AppendLine("null");
                    return;

                case PlistObjectType.VarInt:
                    sb.Append(reader.GetUInt64());
                    sb.AppendLine();
                    return;

                case PlistObjectType.VarReal:
                    sb.Append(reader.GetDouble());
                    sb.AppendLine();
                    return;

                case PlistObjectType.ASCIIString or PlistObjectType.UTF16String:
                    sb.AppendLine(reader.GetString());
                    return;

                case PlistObjectType.ByteArray or PlistObjectType.UID:
                    sb.AppendLine(Convert.ToHexString(reader.GetBytes()));
                    return;

                case PlistObjectType.Array:
                    var array = reader.GetArray();
                    sb.AppendLine($"Array ({array.Length} Items)");
                    foreach (var item in array)
                    {
                        Indent(sb, indent);
                        Impl(item, sb, indent + 1);
                    }
                    return;


                case PlistObjectType.Dictionary:
                    var dict = reader.GetDictionary();
                    sb.AppendLine($"Dictionary ({dict.Count} Items)");
                    foreach (var pair in dict)
                    {
                        Indent(sb, indent);
                        sb.Append($"{pair.Key}: ");
                        Impl(pair.Value, sb, indent + 1);
                    }
                    return;

                default:
                    throw new Exception($"Unsupported plist data type {reader.Type}");
            }
        }

        static void Indent(StringBuilder sb, int indent)
        {
            for (var i = 0; i < indent; i++)
            {
                sb.Append("    ");
            }
        }
    }
}
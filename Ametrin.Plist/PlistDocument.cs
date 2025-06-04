using Ametrin.Plist.Reader;

namespace Ametrin.Plist;

/// <summary>
/// binary property list
/// </summary>
public sealed class PlistDocument
{
    /// <summary>
    /// the raw binary plist data 
    /// </summary>
    internal ImmutableArray<byte> BinaryData { get; }

    /// <summary>
    /// the n-th element is the memory locaton of the n-th object in <see cref="BinaryData"/>
    /// </summary>
    internal ImmutableArray<int> OffsetTable { get; }

    internal PlistTrailer Trailer { get; }

    internal PlistDocument(ImmutableArray<byte> data, ImmutableArray<int> offsetTable, PlistTrailer trailer)
    {
        this.BinaryData = data;
        OffsetTable = offsetTable;
        Trailer = trailer;
    }

    public PlistObjectReader GetRoot() => GetObject(Trailer.RootIndex);

    public object? ParseToObject() => GetRoot().ParseToObject();

    internal PlistObjectReader GetObject(int objectIndex)
    {
        var info = GetObjectInfo(objectIndex);
        return new(this, objectIndex, info);
    }

    private PlistObjectInfo GetObjectInfo(int objectIndex)
        => GetObjectInfo(BinaryData.AsSpan(OffsetTable[objectIndex]..));

    /// <param name="data">the binary data starting the the object marker</param>
    /// <returns>the object type, the count of the additional data and the size of the marker in bytes</returns>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="FormatException"></exception>
    private static PlistObjectInfo GetObjectInfo(ReadOnlySpan<byte> data)
    {
        var marker = data[0];
        var objType = marker >> 4;
        var objInfo = marker & 0x0F;

        return (objType, objInfo) switch
        {
            (0x0, 0x0) => new(PlistObjectType.Null, 0, 1),
            (0x0, 0x8) => new(PlistObjectType.False, 0, 1),
            (0x0, 0x9) => new(PlistObjectType.True, 0, 1),
            (0x1, >= 0 and < 4) => new(PlistObjectType.VarInt, 1 << objInfo, 1),
            (0x2, 2 or 3) => new(PlistObjectType.VarReal, 1 << objInfo, 1),
            (0x3, _) => new(PlistObjectType.Date, 8, 1),
            (0x4, _) => new(PlistObjectType.ByteArray, ReadContainerCount(objInfo, data, out var markerSize), markerSize),
            (0x5, _) => new(PlistObjectType.ASCIIString, ReadContainerCount(objInfo, data, out var markerSize), markerSize),
            (0x6, _) => new(PlistObjectType.UTF16String, ReadContainerCount(objInfo, data, out var markerSize), markerSize),
            (0x8, _) => new(PlistObjectType.UID, objInfo + 1, 1),
            (0xA, _) => new(PlistObjectType.Array, ReadContainerCount(objInfo, data, out var markerSize), markerSize),
            (0xC, _) => new(PlistObjectType.Set, ReadContainerCount(objInfo, data, out var markerSize), markerSize),
            (0xD, _) => new(PlistObjectType.Dictionary, ReadContainerCount(objInfo, data, out var markerSize), markerSize),
            _ => throw new NotSupportedException($"Unsupported object type 0x{marker:X}"),
        };

        static int ReadContainerCount(int objInfo, ReadOnlySpan<byte> data, out int markerSize)
        {
            // If objInfo < 0xF, that is the length. Otherwise, the next bytes describe an integer length
            markerSize = 1;
            return objInfo switch
            {
                < 0xF => objInfo,
                _ => ReadVarLength(data, ref markerSize),
            };
        }

        static int ReadVarLength(ReadOnlySpan<byte> data, ref int markerSize)
        {
            var marker = data[1];
            if (marker >> 4 is not 0x1)
            {
                throw new FormatException("Expected an integer object when object info is 0xF");
            }

            var intInfo = marker & 0x0F;

            var intSize = 1 << intInfo;
            markerSize += 1 + intSize;
            return (int)ReadUIntVarBigEndian(data.Slice(2, intSize));
        }
    }
}

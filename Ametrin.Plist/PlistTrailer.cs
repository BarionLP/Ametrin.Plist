namespace Ametrin.Plist;

public readonly record struct PlistTrailer(byte OffsetIntSize, byte ObjectPointerSize, int ObjectCount, int RootIndex, int OffsetTablePosition)
{
    internal static PlistTrailer FromBinary(ReadOnlySpan<byte> trailer)
    {
        Debug.Assert(trailer.Length is 32);

        // [0‐5]:   unused
        // [6]:     int size for the offset table
        // [7]:     size of object references
        // [8‐15]:  amount of objects in the list (uint64)
        // [16‐23]: index of the root object (uint64)
        // [24‐31]: location of the offset table (uint64)
        byte offsetIntSize = trailer[6];
        byte objectRefSize = trailer[7];
        ulong objectCount = ReadUInt64BigEndian(trailer.Slice(8, 8));
        ulong rootIndex = ReadUInt64BigEndian(trailer.Slice(16, 8));
        ulong offsetTablePosition = ReadUInt64BigEndian(trailer.Slice(24, 8));

        // these limits are arbitrary guard-rails and can be loosened in the future

        if (offsetIntSize > 4) throw new NotSupportedException($"This Plist uses {offsetIntSize} byte integers (limit: 4)");
        if (objectCount > ushort.MaxValue) throw new NotSupportedException($"This Plist contains {objectCount} objects (limit {ushort.MaxValue})");
        if (offsetTablePosition >= int.MaxValue) throw new NotSupportedException($"This Plist is too big (offsetTable start at {offsetTablePosition})");
        if (rootIndex >= objectCount) throw new NotSupportedException($"rootIndex ({rootIndex}) outside of object array (Length: {objectCount})");

        return new (offsetIntSize, objectRefSize, (int)objectCount, (int)rootIndex, (int)offsetTablePosition);
    }
}
namespace Ametrin.Plist;

public static class BinaryHelpers
{
    public static ulong ReadUIntVarBigEndian(ReadOnlySpan<byte> data)
    {
        if (data.Length < 1 || data.Length > 8)
        {
            throw new OverflowException("ReadUIntVarBigEndian support 1 to 8 bytes");
        }

        ulong val = 0;
        for (int i = 0; i < data.Length; i++)
        {
            val = (val << 8) | data[i];
        }
        return val;
    }

    public static ulong ReadUInt64BigEndian(ReadOnlySpan<byte> data)
    {
        Debug.Assert(data.Length is 8);
        return ((ulong)data[0] << 56) |
               ((ulong)data[1] << 48) |
               ((ulong)data[2] << 40) |
               ((ulong)data[3] << 32) |
               ((ulong)data[4] << 24) |
               ((ulong)data[5] << 16) |
               ((ulong)data[6] << 8) |
               ((ulong)data[7]);
    }
    public static float ReadFloatBigEndian(ReadOnlySpan<byte> data)
    {
        Debug.Assert(data.Length is 4);
        return BitConverter.ToSingle(ToBigEndian(data, stackalloc byte[4]));
    }

    public static double ReadDoubleBigEndian(ReadOnlySpan<byte> data)
    {
        Debug.Assert(data.Length is 8);
        return BitConverter.ToDouble(ToBigEndian(data, stackalloc byte[8]));
    }

    public static ReadOnlySpan<byte> ToBigEndian(scoped ReadOnlySpan<byte> data, Span<byte> destination)
    {
        Debug.Assert(data.Length == destination.Length);
        data.CopyTo(destination);
        if (BitConverter.IsLittleEndian)
        {
            destination.Reverse();
        }

        return destination;
    }
}
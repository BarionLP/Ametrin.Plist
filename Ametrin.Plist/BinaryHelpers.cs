namespace Ametrin.Plist;

internal static class BinaryHelpers
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
}
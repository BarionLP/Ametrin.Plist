using System.Collections;

namespace Ametrin.Plist.Reader;

public readonly struct PlistArrayReader : IEnumerable<PlistObjectReader>
{
    private readonly PlistObjectReader[] _elements;
    public int Length => _elements.Length;

    public PlistObjectReader this[int index] => _elements[index];

    internal PlistArrayReader(PlistDocument document, int index, PlistObjectInfo info)
    {
        if (info.Type is not PlistObjectType.Array) throw new InvalidOperationException($"Expected an Array, got a {info.Type}");

        Debug.Assert(document.Trailer.ObjectPointerSize <= 4);

        _elements = new PlistObjectReader[info.ContainerCount];
        var data = document.data.AsSpan((document.offsetTable[index] + info.MarkerSize)..);

        for (int i = 0; i < info.ContainerCount; i++)
        {
            var childIndex = (int)ReadUIntVarBigEndian(data.Slice(i * document.Trailer.ObjectPointerSize, document.Trailer.ObjectPointerSize));
            _elements[i] = document.GetObject(childIndex);
        }
    }

    public IEnumerator<PlistObjectReader> GetEnumerator() => _elements.AsEnumerable().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

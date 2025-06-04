using System.Collections;

namespace Ametrin.Plist.Reader;

public readonly struct PlistDictionaryReader : IEnumerable<KeyValuePair<string, PlistObjectReader>>
{
    private readonly Dictionary<string, PlistObjectReader> _elements = [];
    public int Count => _elements.Count;

    public PlistObjectReader this[string key] => _elements.TryGetValue(key, out var reader) ? reader : throw new KeyNotFoundException();

    internal PlistDictionaryReader(PlistDocument document, int index, PlistObjectInfo info)
    {
        if (info.Type is not PlistObjectType.Dictionary) throw new InvalidOperationException($"Expected a Dictionary, got a {info.Type}");

        Debug.Assert(document.Trailer.ObjectPointerSize <= 4);

        var data = document.BinaryData.AsSpan((document.OffsetTable[index] + info.MarkerSize)..);

        for (int i = 0; i < info.ContainerCount; i++)
        {
            var keyIndex = (int)ReadUIntVarBigEndian(data.Slice(i * document.Trailer.ObjectPointerSize, document.Trailer.ObjectPointerSize));
            var key = document.GetObject(keyIndex).GetString();
            var valueIndex = (int)ReadUIntVarBigEndian(data.Slice(info.ContainerCount * document.Trailer.ObjectPointerSize + i * document.Trailer.ObjectPointerSize, document.Trailer.ObjectPointerSize));
            if (!_elements.TryAdd(key, document.GetObject(valueIndex)))
            {
                throw new FormatException($"Plist Dictionary contains duplicate key '{key}'");
            }
        }
    }

    public IEnumerator<KeyValuePair<string, PlistObjectReader>> GetEnumerator() => _elements.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
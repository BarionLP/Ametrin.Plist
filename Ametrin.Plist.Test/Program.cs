using System.Collections.Frozen;
using System.Runtime.InteropServices;
using System.Xml;
using Ametrin.Plist;
using Ametrin.Plist.Reader;

FrozenSet<string> exclude = [];

foreach (var path in Directory.EnumerateFiles(@"I:\Coding\TestChamber\AppleImageImporter\Fake iPhone", "*.AAE", SearchOption.AllDirectories))
{
    using var aaeStream = File.OpenRead(path);
    var aaeData = new XmlDocument();
    aaeData.Load(aaeStream);

    var dictionary = aaeData["plist"]!["dict"]!;
    var base64Data = dictionary["data"]!.InnerText.Trim().Replace("\n\t", "");
    var binaryPlist = ImmutableCollectionsMarshal.AsImmutableArray(Convert.FromBase64String(base64Data));

    if (PlistParser.IsPlist(binaryPlist.AsSpan()))
    {
        var root = PlistParser.ToDocument(binaryPlist).GetRoot();
        Console.WriteLine(path);
        Write(root);
    }
}

// var binaryPlist = ImmutableCollectionsMarshal.AsImmutableArray(Convert.FromBase64String("YnBsaXN0MDDUAQIDBAUGBwpYJHZlcnNpb25ZJGFyY2hpdmVyVCR0b3BYJG9iamVjdHMSAAGGoF8QD05TS2V5ZWRBcmNoaXZlctEICVRyb290gAGjCwwPVSRudWxs0Q0OViRjbGFzc4AC0hAREhNaJGNsYXNzbmFtZVgkY2xhc3Nlc18QJFNTU1NjcmVlbnNob3RNdXRhYmxlTW9kaWZpY2F0aW9uSW5mb6MUFRZfECRTU1NTY3JlZW5zaG90TXV0YWJsZU1vZGlmaWNhdGlvbkluZm9fEB1TU1NTY3JlZW5zaG90TW9kaWZpY2F0aW9uSW5mb1hOU09iamVjdAgRGiQpMjdJTFFTV11gZ2lueYKprdT0AAAAAAAAAQEAAAAAAAAAFwAAAAAAAAAAAAAAAAAAAP0="));

// if (PlistParser.IsPlist(binaryPlist.AsSpan()))
// {
//     var root = PlistParser.ToDocument(binaryPlist).GetRoot();
//     Console.WriteLine(root);
// }

static void Write(PlistObjectReader reader, int indent = 0)
{
    switch (reader.Type)
    {
        case PlistObjectType.Null:
            Console.WriteLine("null");
            return;

        case PlistObjectType.VarInt:
            Console.WriteLine(reader.GetInt32());
            return;

        case PlistObjectType.ASCIIString or PlistObjectType.UTF16String:
            Console.WriteLine(reader.GetString());
            return;

        case PlistObjectType.ByteArray or PlistObjectType.UID:
            Console.WriteLine(Convert.ToHexString(reader.GetBytes()));
            return;

        case PlistObjectType.Array:
            var array = reader.GetArray();
            Console.WriteLine($"Array ({array.Length} Items)");
            foreach (var item in array)
            {
                Indent(indent + 1);
                Write(item, indent + 1);
            }
            return;


        case PlistObjectType.Dictionary:
            var dict = reader.GetDictionary();
            Console.WriteLine($"Dictionary ({dict.Count} Items)");
            foreach (var pair in dict)
            {
                Indent(indent + 1);
                Console.Write($"{pair.Key}: ");
                Write(pair.Value, indent + 1);
            }
            return;

        default:
            throw new Exception($"Unsupported Data Type {reader.Type}");
    }

    static void Indent(int indent)
    {
        Console.Write(string.Concat(Enumerable.Repeat("    ", indent)));
    }
}
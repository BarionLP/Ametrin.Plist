using System.Runtime.InteropServices;

namespace Ametrin.Plist.Test;

public sealed class PlistReaderTests
{
    [Test]
    public async Task ReadAppleSlowMotionInfo()
    {
        var data = ImmutableCollectionsMarshal.AsImmutableArray(Convert.FromBase64String("YnBsaXN0MDDRAQJac2xvd01vdGlvbtIDBAUWV3JlZ2lvbnNUcmF0ZaEG0QcIWXRpbWVSYW5nZdIJCgsUVXN0YXJ0WGR1cmF0aW9u1AwNDg8QERITVWZsYWdzVXZhbHVlWXRpbWVzY2FsZVVlcG9jaBABEqcw50ASO5rKABAA1AwNDg8QFRITEwAAAAMMOYyAIj+AAAAICxYbIygqLTc8QktUWmBqcHJ3fH6HkAAAAAAAAAEBAAAAAAAAABcAAAAAAAAAAAAAAAAAAACV"));
        var document = PlistReader.ToDocument(data);
        var root = document.GetRoot().GetDictionary();
        var slowMotion = root["slowMotion"].GetDictionary();
        await Assert.That(slowMotion["rate"].GetSingle()).IsEqualTo(1);
        var region = slowMotion["regions"].GetArray()[0].GetDictionary();
        var timeRange = region["timeRange"].GetDictionary();
        var start = timeRange["start"].GetDictionary();
        var duration = timeRange["duration"].GetDictionary();

        await Assert.That(start["flags"].GetInt32()).IsEqualTo(1);
        await Assert.That(duration["flags"].GetInt32()).IsEqualTo(1);

        await Assert.That(start["value"].GetUInt64()).IsEqualTo(2805000000ul);
        await Assert.That(duration["value"].GetUInt64()).IsEqualTo(13090000000ul);


        await Assert.That(start["timescale"].GetInt32()).IsEqualTo(1000000000);
        await Assert.That(duration["timescale"].GetInt32()).IsEqualTo(1000000000);

        await Assert.That(start["epoch"].GetInt32()).IsEqualTo(0);
        await Assert.That(duration["epoch"].GetInt32()).IsEqualTo(0);
    }
}

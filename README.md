# Ametrin.Plist
A fast and efficient binary property list (plist) reader written in .NET.

## how to use
```cs
// data can be a Stream or ImmutableArray<byte>
var document = PlistReader.ToDocument(data);
var root = document.GetRoot();

var dictionary = root.GetDictionary();

var val1 = dictionary["val1"].GetInt32();
var val2 = dictionary["val2"].GetBoolean();

var array = dictionary["array"].GetArray();
var i0 = array[0].GetUInt64();
var i1 = array[1].GetDouble();
```
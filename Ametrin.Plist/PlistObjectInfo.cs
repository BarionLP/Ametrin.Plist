namespace Ametrin.Plist;

internal readonly record struct PlistObjectInfo(PlistObjectType Type, int ContainerCount, int MarkerSize);

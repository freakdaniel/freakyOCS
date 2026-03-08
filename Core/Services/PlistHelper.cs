using Claunia.PropertyList;

namespace OcsNet.Core.Services;

/// <summary>
/// Helper extensions for creating plist objects with simpler syntax.
/// </summary>
public static class PlistHelper
{
    public static NSNumber Bool(bool value) => new(value);
    public static NSString Str(string value) => new(value);
    public static NSNumber Int(int value) => new(value);
    public static NSNumber Long(long value) => new(value);
    public static NSNumber UInt(uint value) => new(value);
    public static NSData Data(byte[] value) => new(value);
    public static NSData EmptyData() => new([]);
    
    /// <summary>
    /// Creates an NSDictionary with fluent syntax.
    /// Usage: Dict(("key1", Bool(true)), ("key2", Str("value")))
    /// </summary>
    public static NSDictionary Dict(params (string key, NSObject value)[] items)
    {
        var dict = new NSDictionary();
        foreach (var (key, value) in items)
            dict[key] = value;
        return dict;
    }

    /// <summary>
    /// Creates an NSArray from items.
    /// </summary>
    public static NSArray Arr(params NSObject[] items)
    {
        var arr = new NSArray(items.Length);
        foreach (var item in items)
            arr.Add(item);
        return arr;
    }

    public static NSArray EmptyArr() => new();
    public static NSDictionary EmptyDict() => new();
}

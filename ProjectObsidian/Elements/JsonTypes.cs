using System;
using System.Linq;
using Elements.Core;
using Newtonsoft.Json.Linq;

namespace Obsidian.Elements;

public static class JsonTypeHelper
{
    public static readonly Type[] JsonTokens =
    {
        typeof(IJsonToken), typeof(JsonObject), typeof(JsonArray),
    };
    public static readonly Type[] AllValidTypes =
    {
        typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
        typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(string), typeof(Uri),
        typeof(IJsonToken), typeof(JsonObject), typeof(JsonArray),
    };
    public static readonly Type[] AllValidGetTypes =
    {
        typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
        typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(string), typeof(Uri),
        typeof(JsonObject), typeof(JsonArray),
    };
    public static readonly Type[] ValidValueTypes =
    {
        typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
        typeof(long), typeof(ulong), typeof(float), typeof(double)
    };
    public static readonly Type[] ValidObjectGetTypes =
    {
        typeof(string), typeof(Uri), typeof(JsonObject), typeof(JsonArray),
    };
    public static readonly Type[] ValidObjectSetTypes =
    {
        typeof(string), typeof(Uri), typeof(IJsonToken), typeof(JsonObject), typeof(JsonArray),
    };
}

[DataModelType]
public interface IJsonToken
{
    public JToken Wrapped { get; }
}
[DataModelType]
public class JsonObject : IJsonToken
{
    public JObject WrappedObject { get; private set; }
    public JToken Wrapped => WrappedObject;
    public JsonObject(JObject wrap) => WrappedObject = wrap;
    public int Count => WrappedObject.Count;
    public override string ToString() => WrappedObject.ToString();
    public static JsonObject FromString(string str)
    {
        try
        {
            return new JsonObject(JObject.Parse(str));
        }
        catch
        {
            return null;
        }
    }
    public T Get<T>(string tag)
    {
        try
        {
            //TODO: theres probably a better way to do this than boxing the value
            if (typeof(T) == typeof(JsonObject)) return (T)(object)new JsonObject(WrappedObject[tag].Value<JObject>());
            if (typeof(T) == typeof(JsonArray)) return (T)(object)new JsonArray(WrappedObject[tag].Value<JArray>());
            return WrappedObject[tag].Value<T>() ?? default;
        }
        catch
        {
            return default;
        }
    }
    public T GetValue<T>(string tag) where T : unmanaged
    {
        try
        {
            return WrappedObject[tag]?.Value<T>() ?? default;
        }
        catch
        {
            return default;
        }
    }
    public T GetObject<T>(string tag) where T : class
    {
        try
        {
            if (typeof(T) == typeof(JsonObject))
            {
                var value = WrappedObject[tag]?.Value<JObject>();
                if (value is null) return null;
                return new JsonObject(value) as T;
            }
            if (typeof(T) == typeof(JsonArray))
            {
                var value = WrappedObject[tag]?.Value<JArray>();
                if (value is null) return null;
                return new JsonArray(value) as T;
            }
            return WrappedObject[tag]?.Value<T>();
        }
        catch
        {
            return null;
        }
    }
    public JsonObject Add<T>(string tag, T value)
    {
        var cloned = (JObject)WrappedObject.DeepClone();
        var token = value is IJsonToken jToken ? jToken.Wrapped.DeepClone() : new JValue(value);
        cloned.Add(tag, token);
        var output = new JsonObject(cloned);
        return output;
    }
    public JsonObject Remove(string tag)
    {
        try
        {
            if (!WrappedObject.ContainsKey(tag)) return this;
            var output = (JObject)WrappedObject.DeepClone();
            output.Remove(tag);
            return new JsonObject(output);
        }
        catch
        {
            return null;
        }
    }
}

[DataModelType]
public class JsonArray : IJsonToken
{
    public JArray WrappedObject { get; private set; }
    public JToken Wrapped => WrappedObject;

    public JsonArray(JArray wrap) => WrappedObject = wrap;

    public int Count => WrappedObject.Count;
    public override string ToString() => WrappedObject.ToString();
    public static JsonArray FromString(string str)
    {
        try
        {
            return new JsonArray(JArray.Parse(str));
        }
        catch
        {
            return null;
        }
    }
    public T Get<T>(int index)
    {
        try
        {
            if (typeof(T) == typeof(JsonObject)) return (T)(object)new JsonObject(WrappedObject[index].Value<JObject>());
            if (typeof(T) == typeof(JsonArray)) return (T)(object)new JsonArray(WrappedObject[index].Value<JArray>());
            return WrappedObject[index].Value<T>() ?? default;
        }
        catch
        {
            return default;
        }
    }
    public T GetValue<T>(int index) where T : unmanaged
    {
        try
        {
            return WrappedObject[index].Value<T>();
        }
        catch
        {
            return default;
        }
    }
    public T GetObject<T>(int index) where T : class
    {
        try
        {
            if (typeof(T) == typeof(JsonObject))
            {
                var value = WrappedObject[index].Value<JObject>();
                if (value is null) return null;
                return new JsonObject(value) as T;
            }
            if (typeof(T) == typeof(JsonArray))
            {
                var value = WrappedObject[index].Value<JArray>();
                if (value is null) return null;
                return new JsonArray(value) as T;
            }
            return WrappedObject[index].Value<T>();
        }
        catch
        {
            return null;
        }
    }
    public JsonArray Insert<T>(int index, T value)
    {
        try
        {
            var cloned = (JArray)WrappedObject.DeepClone();
            var token = value switch
            {
                null => JValue.CreateNull(),
                IJsonToken jToken => jToken.Wrapped.DeepClone(),
                _ => new JValue(value),
            };
            cloned.Insert(index, token);
            var output = new JsonArray(cloned);
            return output;
        }
        catch
        {
            return null;
        }
    }
    public JsonArray Append<T>(T value)
    {
        try
        {
            var cloned = (JArray)WrappedObject.DeepClone();
            var token = value switch
            {
                null => JValue.CreateNull(),
                IJsonToken jToken => jToken.Wrapped.DeepClone(),
                _ => new JValue(value),
            };
            cloned.Add(token);
            var output = new JsonArray(cloned);
            return output;
        }
        catch
        {
            return null;
        }
    }
    public JsonArray Remove(int index)
    {
        try
        {
            if (index < 0 || index >= Count) return this;
            var output = (JArray)WrappedObject.DeepClone();
            output.RemoveAt(index);
            return new JsonArray(output);
        }
        catch
        {
            return null;
        }
    }
}
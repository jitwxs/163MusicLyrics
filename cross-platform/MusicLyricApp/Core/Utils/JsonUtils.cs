using System.Collections.Generic;
using Newtonsoft.Json;

namespace MusicLyricApp.Core.Utils;

public static class JsonUtils
{
    public static T ToEntity<T>(this string val)
    {
        return JsonConvert.DeserializeObject<T>(val)
               ?? throw new JsonSerializationException("Expected a JSON value, but received null.");
    }

    public static List<T> ToEntityList<T>(this string val)
    {
        return JsonConvert.DeserializeObject<List<T>>(val)
               ?? throw new JsonSerializationException("Expected a JSON array, but received null.");
    }

    public static string ToJson<T>(this T entity, Formatting formatting = Formatting.None)
    {
        return JsonConvert.SerializeObject(entity, formatting);
    }
}
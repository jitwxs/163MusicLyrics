using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MusicLyricApp.Core.Utils;

public static class JsonUtils
{
    public static T ToEntity<T>(this string val)
    {
        return JsonConvert.DeserializeObject<T>(val);
    }

    public static List<T> ToEntityList<T>(this string val)
    {
        return JsonConvert.DeserializeObject<List<T>>(val);
    }

    public static string ToJson<T>(this T entity, Formatting formatting = Formatting.None)
    {
        return JsonConvert.SerializeObject(entity, formatting);
    }

    public static JObject ToJObject(this string val)
    {
        return JObject.Parse(val);
    }
}
using Newtonsoft.Json;
using System.Collections.Generic;

namespace WindowsFormsApp1
{
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
    }
}

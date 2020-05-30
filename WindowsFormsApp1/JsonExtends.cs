using Newtonsoft.Json;

namespace WindowsFormsApp1
{
    /// <summary>
    /// Json扩展方法
    /// </summary>
    public static class JsonExtends
    {
        public static T ToEntity<T>(this string val)
        {
            return JsonConvert.DeserializeObject<T>(val);
        }

        //public static List<T> ToEntityList<T>(this string val)
        //{
        //    return JsonConvert.DeserializeObject<List<T>>(val);
        //}
        public static string ToJson<T>(this T entity, Formatting formatting = Formatting.None)
        {
            return JsonConvert.SerializeObject(entity, formatting);
        }
    }
}

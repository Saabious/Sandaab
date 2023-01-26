using Newtonsoft.Json;

namespace Sandaab.Core.Components
{
    public class JsonHelper
    {
        public static object Get(string type, string json)
        {
            return string.IsNullOrEmpty(json) ? null : JsonConvert.DeserializeObject(json, Type.GetType(type, true));
        }

        public static void Set(object value, out string type, out string json)
        {
            var t = value?.GetType();
            type = t == null ? string.Empty : (string.IsNullOrEmpty(t.FullName) ? t.Name : t.FullName);
            json = t == null ? string.Empty : (JsonConvert.SerializeObject(value));
        }
    }
}

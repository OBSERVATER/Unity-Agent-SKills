using Newtonsoft.Json;

namespace Observater.AiSkills.Runtime
{
    public static class JsonExtension
    {
        public static string ToJson<T>(this T obj) where T : class => JsonConvert.SerializeObject(obj,Formatting.Indented);
        public static T FromJson<T>(this string s) where T : class 
            => JsonConvert.DeserializeObject<T>(s,new JsonSerializerSettings()
        {
            Formatting = Formatting.Indented
        });
    }
}
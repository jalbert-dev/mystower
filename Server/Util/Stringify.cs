using Newtonsoft.Json;

namespace Util
{
    public static class Stringify
    {
        public static string ToPrettyJson(this object self) 
            => $"{self.GetType().Name} => {JsonConvert.SerializeObject(self, Formatting.Indented)}";
        public static string ToJson(this object self)
            => JsonConvert.SerializeObject(self, Formatting.None);
    }
}
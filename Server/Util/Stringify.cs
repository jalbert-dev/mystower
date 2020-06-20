using Newtonsoft.Json;

namespace Server.Util
{
    public static class Stringify
    {
        public static string ToJsonString(this object self) 
            => $"{self.GetType().Name} => {JsonConvert.SerializeObject(self, Formatting.Indented)}";
    }
}
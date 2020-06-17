using System.IO;
using Newtonsoft.Json;
using Server.Data;

namespace Server
{
    public static class GameStateIO
    {
        public static void SaveToStream(GameState self, TextWriter outStream)
            => outStream.Write(JsonConvert.SerializeObject(self));

        public static GameState LoadFromString(string str)
            => JsonConvert.DeserializeObject<GameState>(str);
    }
}
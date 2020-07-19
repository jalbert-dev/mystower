using System;
using Newtonsoft.Json;

namespace Server.Database
{
    public struct StatBlock
    {
        [JsonProperty(Required=Required.Always)]
        public int hp;
        [JsonProperty(Required=Required.Always)]
        public int atk;
        [JsonProperty(Required=Required.Always)]
        public int def;
    }

    [CodeGen.DatabaseType]
    public partial class ActorArchetype
    {
        private StatBlock lvlMinStatus = default(StatBlock);
        private StatBlock lvlMaxStatus = default(StatBlock);
        private string defaultAiType = "";

        private string nameId = "";
        private string appearanceId = "";

        private const int MIN_LEVEL = 1;
        private const int MAX_LEVEL = 99;
        public static int ClampLevel(int lvl) => Math.Min(MAX_LEVEL, Math.Max(MIN_LEVEL, lvl));

        public StatBlock StatusAtLevel(int level)
            => new StatBlock
            {
                hp = Lerp(lvlMinStatus.hp, lvlMaxStatus.hp, level),
                atk = Lerp(lvlMinStatus.atk, lvlMaxStatus.atk, level),
                def = Lerp(lvlMinStatus.def, lvlMaxStatus.def, level),
            };

        private int Lerp(int a, int b, int lvl)
        {
            lvl = ClampLevel(lvl);
            float t = (float)(lvl - 1) / (float)(MAX_LEVEL - 1);
            return (int)((1 - t) * a + t * b);
        }
    }
}
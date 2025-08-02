using System.Text.Json.Serialization;

namespace Hearthvale.GameCode.Data
{
    public class CharacterStats
    {
        [JsonPropertyName("maxHealth")]
        public int MaxHealth { get; set; }

        [JsonPropertyName("attackPower")]
        public int AttackPower { get; set; }

        [JsonPropertyName("xpYield")]
        public int XpYield { get; set; }
    }
}
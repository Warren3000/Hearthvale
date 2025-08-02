using System.Text.Json.Serialization;

namespace Hearthvale.GameCode.Data
{
    public class WeaponStats
    {
        [JsonPropertyName("baseDamage")]
        public int BaseDamage { get; set; }

        [JsonPropertyName("scale")]
        public float Scale { get; set; }
    }
}
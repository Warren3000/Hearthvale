namespace Hearthvale.GameCode.Rendering
{
    public class ThemeConfig
    {
        public float Desaturate { get; set; } = 0.35f;
        public float Vignette { get; set; } = 0.45f;
        public float Grain { get; set; } = 0.05f;

        public Microsoft.Xna.Framework.Color FogColor { get; set; } = new Microsoft.Xna.Framework.Color(18, 22, 22);
        public Microsoft.Xna.Framework.Color UIBg { get; set; } = new Microsoft.Xna.Framework.Color(12, 14, 16, 220);
        public Microsoft.Xna.Framework.Color UIAccent { get; set; } = new Microsoft.Xna.Framework.Color(180, 180, 120);
        public string FontPrimary { get; set; } = "fonts/04B_30.spritefont";
    }

    public static class Theme
    {
        public static ThemeConfig Current { get; } = new ThemeConfig();
    }
}

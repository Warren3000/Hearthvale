using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Hearthvale.GameCode.Data
{
    public class DecorationSetDefinition
    {
        public string TexturePath { get; set; }
        public List<DecorationRegion> Regions { get; set; } = new List<DecorationRegion>();
        public List<DecorationGroup> Groups { get; set; } = new List<DecorationGroup>();
    }

    public class DecorationGroup
    {
        public string Name { get; set; }
        public List<DecorationRegion> Regions { get; set; } = new List<DecorationRegion>();
    }

    public class DecorationRegion
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Name { get; set; }

        public Rectangle ToRectangle()
        {
            return new Rectangle(X, Y, Width, Height);
        }
    }
}

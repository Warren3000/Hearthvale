using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using System.Collections.Generic;
using Hearthvale.GameCode.Managers;

namespace Hearthvale.GameCode.Rendering
{
    /// <summary>
    /// Handles rendering of dungeon elements with sprites
    /// </summary>
    public class DungeonElementRenderer
    {
        private readonly Dictionary<string, TextureRegion> _elementTextures;
        private readonly TextureAtlas _dungeonAtlas;

        public DungeonElementRenderer(TextureAtlas dungeonAtlas)
        {
            _dungeonAtlas = dungeonAtlas;
            _elementTextures = new Dictionary<string, TextureRegion>();
            LoadElementTextures();
        }

        private void LoadElementTextures()
        {
            // Map element types to their texture regions in the atlas
            // These names should match your atlas definition
            _elementTextures["switch_off"] = _dungeonAtlas.GetRegion("switch_off");
            _elementTextures["switch_on"] = _dungeonAtlas.GetRegion("switch_on");
            _elementTextures["lever_left"] = _dungeonAtlas.GetRegion("lever_left");
            _elementTextures["lever_right"] = _dungeonAtlas.GetRegion("lever_right");
            _elementTextures["pressure_plate"] = _dungeonAtlas.GetRegion("pressure_plate");
            _elementTextures["pressure_plate_pressed"] = _dungeonAtlas.GetRegion("pressure_plate_pressed");
        }

        public void DrawElement(SpriteBatch spriteBatch, IDungeonElement element, int tileSize)
        {
            if (element is DungeonSwitch dungeonSwitch)
            {
                DrawSwitch(spriteBatch, dungeonSwitch, tileSize);
            }
            else if (element is DungeonLever lever)
            {
                DrawLever(spriteBatch, lever, tileSize);
            }
            else if (element is DungeonPressurePlate plate)
            {
                DrawPressurePlate(spriteBatch, plate, tileSize);
            }
        }
        private void DrawSwitch(SpriteBatch spriteBatch, DungeonSwitch dungeonSwitch, int tileSize)
        {
            string textureKey = dungeonSwitch.IsActive ? "switch_on" : "switch_off";

            if (_elementTextures.TryGetValue(textureKey, out var region))
            {
                Vector2 position = new Vector2(dungeonSwitch.Column * tileSize, dungeonSwitch.Row * tileSize);
                spriteBatch.Draw(region.Texture, position, region.SourceRectangle, Color.White);
            }
        }

        private void DrawLever(SpriteBatch spriteBatch, DungeonLever lever, int tileSize)
        {
            string textureKey = lever.IsActive ? "lever_right" : "lever_left";

            if (_elementTextures.TryGetValue(textureKey, out var region))
            {
                Vector2 position = new Vector2(lever.Column * tileSize, lever.Row * tileSize);
                spriteBatch.Draw(region.Texture, position, region.SourceRectangle, Color.White);
            }
        }

        private void DrawPressurePlate(SpriteBatch spriteBatch, DungeonPressurePlate plate, int tileSize)
        {
            string textureKey = plate.IsActive ? "pressure_plate_pressed" : "pressure_plate";

            if (_elementTextures.TryGetValue(textureKey, out var region))
            {
                Vector2 position = new Vector2(plate.Column * tileSize, plate.Row * tileSize);
                spriteBatch.Draw(region.Texture, position, region.SourceRectangle, Color.White);
            }
        }

        public TextureRegion GetElementTexture(string elementType, bool isActive = false)
        {
            string key = elementType + (isActive ? "_active" : "");
            return _elementTextures.GetValueOrDefault(key);
        }
    }
}
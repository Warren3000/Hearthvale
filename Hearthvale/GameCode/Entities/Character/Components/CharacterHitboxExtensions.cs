using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;

namespace Hearthvale.GameCode.Entities
{
    public static class CharacterHitboxExtensions
    {
        // Tight AABB in world space from current sprite frame's opaque pixels
        public static Rectangle GetTightSpriteBounds(this Character character, byte alphaThreshold = 25)
        {
            if (character == null)
            {
                return Rectangle.Empty;
            }

            _ = alphaThreshold;

            return character.GetSpriteBoundsAt(character.Position);
        }
    }
}
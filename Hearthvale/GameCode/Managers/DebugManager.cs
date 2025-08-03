using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Entities.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;

public class DebugManager
{
    public bool DebugDrawEnabled { get; set; } = true;
    public bool ShowCollisionBoxes { get; set; } = true;
    public bool ShowAttackAreas { get; set; } = true;
    public bool ShowUIOverlay { get; set; } = true;

    private Texture2D _whitePixel;

    public DebugManager(Texture2D whitePixel)
    {
        _whitePixel = whitePixel;
#if DEBUG
        DebugDrawEnabled = true;
        ShowCollisionBoxes = true;
        ShowAttackAreas = true;
        ShowUIOverlay = true;
#else
        DebugDrawEnabled = false;
        ShowCollisionBoxes = false;
        ShowAttackAreas = false;
        ShowUIOverlay = false;
#endif
    }

    public void Draw(SpriteBatch spriteBatch, Player player, IEnumerable<NPC> npcs, Tilemap tilemap, int wallTileId, IEnumerable<IDungeonElement> elements, Matrix viewMatrix)
    {
        if (!DebugDrawEnabled) return;

        if (ShowCollisionBoxes)
        {
            DrawRect(spriteBatch, player.Bounds, Color.LimeGreen * 0.5f);
            foreach (var npc in npcs)
                DrawRect(spriteBatch, npc.Bounds, Color.Red * 0.5f);

            // Wall collision boxes
            for (int row = 0; row < tilemap.Rows; row++)
            {
                for (int col = 0; col < tilemap.Columns; col++)
                {
                    if (tilemap.GetTileId(col, row) == wallTileId)
                    {
                        var rect = new Rectangle(
                            (int)(col * tilemap.TileWidth),
                            (int)(row * tilemap.TileHeight),
                            (int)tilemap.TileWidth,
                            (int)tilemap.TileHeight
                        );
                        DrawRect(spriteBatch, rect, Color.Red * 0.5f);
                    }
                }
            }
        }
        if (ShowAttackAreas)
        {
            // Draw players' sword swing arcs
            if (player.EquippedWeapon?.IsSlashing == true)
            {
                Vector2 playerCenter = player.Position + new Vector2(player.Sprite.Width / 2f, player.Sprite.Height / 2f);
                float baseAngle = player.EquippedWeapon.Rotation;
                //float swingArc = MathHelper.PiOver4; // 90 degrees swing
                //float startAngle = baseAngle - swingArc;
                //float endAngle = baseAngle + swingArc;
                float swingArc = MathHelper.ToRadians(40); // 40 degrees total arc
                float startAngle = baseAngle - swingArc;
                float endAngle = baseAngle + swingArc;
                float handleOffset = 8f;
                float bladeLength = player.EquippedWeapon.Length - handleOffset;
                float thickness = 12f;

                DrawArc(spriteBatch, _whitePixel, playerCenter, bladeLength, handleOffset, thickness, startAngle, endAngle, Color.Orange, 24);
                player.EquippedWeapon.DrawHitPolygon(spriteBatch, _whitePixel, playerCenter, Color.Red);
            }
            // Draw NPCs' sword swing arcs
            foreach (var npc in npcs)
            {
                if (npc.EquippedWeapon?.IsSlashing == true)
                {
                    Vector2 npcCenter = npc.Position + new Vector2(npc.Sprite.Width / 2f, npc.Sprite.Height / 2f);
                    float radius = npc.EquippedWeapon.Length;
                    float baseAngle = npc.EquippedWeapon.Rotation;
                    float swingArc = MathHelper.PiOver4;
                    float handleOffset = 8f;
                    float bladeLength = npc.EquippedWeapon.Length - handleOffset;
                    float startAngle = baseAngle - swingArc;
                    float endAngle = baseAngle + swingArc;
                    float thickness = 12f;

                    DrawArc(spriteBatch, _whitePixel, npcCenter, bladeLength, handleOffset, thickness, startAngle, endAngle, Color.Orange, 24);
                    npc.EquippedWeapon.DrawHitPolygon(spriteBatch, _whitePixel, npcCenter, Color.Red);
                }
            }
        }
        // Dungeon element collision boxes
        foreach (var element in elements)
        {
            var boundsProperty = element.GetType().GetProperty("Bounds");
            if (boundsProperty != null)
            {
                var bounds = (Rectangle)boundsProperty.GetValue(element);
                var topLeft = Vector2.Transform(new Vector2(bounds.X, bounds.Y), viewMatrix);
                var bottomRight = Vector2.Transform(new Vector2(bounds.Right, bounds.Bottom), viewMatrix);
                var screenRect = new Rectangle(
                    (int)topLeft.X,
                    (int)topLeft.Y,
                    (int)(bottomRight.X - topLeft.X),
                    (int)(bottomRight.Y - topLeft.Y)
                );
                DrawRect(spriteBatch, screenRect, Color.Red * 0.5f);
            }
        }
    }
    private void DrawArc(SpriteBatch spriteBatch, Texture2D pixel, Vector2 center, float length, float handleOffset, float thickness, float startAngle, float endAngle, Color color, int segments = 16)
    {
        // Use the same offset as Weapon.Draw and Weapon.GetTransformedHitPolygon
        const float visualRotationOffset = MathHelper.PiOver4; // 45 degrees
        startAngle -= visualRotationOffset;
        endAngle -= visualRotationOffset;

        float angleStep = (endAngle - startAngle) / segments;
        float innerRadius = handleOffset;
        float outerRadius = handleOffset + length;

        Vector2[] innerPoints = new Vector2[segments + 1];
        Vector2[] outerPoints = new Vector2[segments + 1];

        for (int i = 0; i <= segments; i++)
        {
            float angle = startAngle + angleStep * i;
            innerPoints[i] = center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * innerRadius;
            outerPoints[i] = center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * outerRadius;
        }

        for (int i = 0; i < segments; i++)
        {
            spriteBatch.Draw(pixel, outerPoints[i], null, color, (float)Math.Atan2(outerPoints[i + 1].Y - outerPoints[i].Y, outerPoints[i + 1].X - outerPoints[i].X), Vector2.Zero, Vector2.Distance(outerPoints[i], outerPoints[i + 1]), SpriteEffects.None, 0);
            spriteBatch.Draw(pixel, innerPoints[i], null, color, (float)Math.Atan2(innerPoints[i + 1].Y - innerPoints[i].Y, innerPoints[i + 1].X - innerPoints[i].X), Vector2.Zero, Vector2.Distance(innerPoints[i], innerPoints[i + 1]), SpriteEffects.None, 0);
            spriteBatch.Draw(pixel, innerPoints[i], null, color, (float)Math.Atan2(outerPoints[i].Y - innerPoints[i].Y, outerPoints[i].X - innerPoints[i].X), Vector2.Zero, Vector2.Distance(innerPoints[i], outerPoints[i]), SpriteEffects.None, 0);
        }
    }
    private void DrawRect(SpriteBatch spriteBatch, Rectangle rect, Color color)
    {
        // Top
        spriteBatch.Draw(_whitePixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), color);
        // Left
        spriteBatch.Draw(_whitePixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), color);
        // Right
        spriteBatch.Draw(_whitePixel, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), color);
        // Bottom
        spriteBatch.Draw(_whitePixel, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), color);
    }
}
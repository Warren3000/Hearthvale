using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Entities.Players;
using Hearthvale.GameCode.Input;
using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGameGum;
using MonoGameGum.Forms.Controls;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using SpriteBatch = Microsoft.Xna.Framework.Graphics.SpriteBatch;
using Hearthvale.GameCode.Entities;
using Hearthvale.GameCode.Entities.Components;

public class DebugManager
{
    private static DebugManager _instance;
    public static DebugManager Instance => _instance ?? throw new InvalidOperationException("DebugManager not initialized. Call Initialize first.");
    public float FontScale { get; set; } = 1f;

    #region Debug Toggle Properties
    // --- Master Debug Controls ---
    public bool DebugDrawEnabled { get; set; } = false; // Master switch for all debug drawing
    public bool ShowUIOverlay { get; set; } = true;

    // --- Core Debug Categories ---
    public bool ShowPhysicsDebug { get; set; } = false; // Collision boxes, velocity, AI targets
    public bool ShowCombatDebug { get; set; } = false; // Weapon hitboxes, attack ranges
    public bool ShowRenderingDebug { get; set; } = false; // Position discrepancies
    public bool ShowSpriteAlignment { get; set; } = false; // Sprite/hitbox alignment

    // --- Detailed Debug Options ---
    public bool ShowDetailedPhysics { get; set; } = false; // Verbose physics info
    public bool ShowDungeonElements { get; set; } = false; // Dungeon element bounds
    public bool ShowUIDebugGrid { get; set; } = false; // UI alignment grid
    public bool ShowTilesetViewer { get; set; } = false; // Tileset viewer
    public bool ShowDetailedWeaponDebug { get; set; } = false;

    // NEW: show dungeon elements’ collision (e.g., chest tight/collision bounds)
    public bool ShowDungeonCollisionDebug { get; set; } = false;
    
    // Show collision bounds debug overlay (walls, chests, dungeon entities)
    public bool ShowCollisionBounds { get; set; } = false;
    #endregion

    #region Performance and Caching
    // Performance optimizations
    private int _frameCounter = 0;
    private const int DEBUG_UPDATE_FREQUENCY = 5; // Update AI debug less frequently
    private readonly Dictionary<NPC, DebugNpcCache> _npcDebugCache = new();

    private readonly Texture2D _whitePixel;

    // Cache wall collision rectangles for the current tilemap
    private List<Rectangle> _cachedWallCollisionRects;
    private Point _cachedMapSize;

    // Cached debug data to avoid recalculating every frame
    private struct DebugNpcCache
    {
        public Vector2 LastPlayerCenter;
        public Vector2 LastEngagementPoint;
        public float LastAttackRange;
        public int LastUpdateFrame;
    }
    #endregion

    private DebugManager(Texture2D whitePixel)
    {
        _whitePixel = whitePixel;
#if DEBUG
        DebugDrawEnabled = true;
        ShowUIOverlay = true;
#else
        DebugDrawEnabled = false;
        ShowUIOverlay = false;
#endif
    }

    /// <summary>
    /// Initializes the singleton instance. Call this once at startup.
    /// </summary>
    public static void Initialize(Texture2D whitePixel)
    {
        _instance = new DebugManager(whitePixel);
    }

    #region Debug Mode Control Methods
    /// <summary>
    /// Disables all debug drawing features. Useful for clean gameplay.
    /// </summary>
    public void DisableAllDebugDrawing()
    {
        DebugDrawEnabled = false;
        ShowPhysicsDebug = false;
        ShowCombatDebug = false;
        ShowRenderingDebug = false;
        ShowSpriteAlignment = false;
        ShowDungeonElements = false;
        ShowUIDebugGrid = false;
        ShowDetailedPhysics = false;
        ShowDungeonCollisionDebug = false;
    }

    /// <summary>
    /// Enables minimal debug drawing for performance.
    /// </summary>
    public void EnableMinimalDebugDrawing()
    {
        DebugDrawEnabled = true;
        ShowPhysicsDebug = false;
        ShowCombatDebug = false;
        ShowRenderingDebug = false;
        ShowSpriteAlignment = false;
        ShowDungeonElements = false;
        ShowUIDebugGrid = false;
        ShowDetailedPhysics = false;
        ShowDungeonCollisionDebug = false;
        ShowUIOverlay = true;
    }

    /// <summary>
    /// Enables all debug drawing features. WARNING: May cause performance issues.
    /// </summary>
    public void EnableAllDebugDrawing()
    {
        DebugDrawEnabled = true;
        ShowPhysicsDebug = true;
        ShowCombatDebug = true;
        ShowRenderingDebug = true;
        ShowSpriteAlignment = true;
        ShowDungeonElements = true;
        ShowDetailedPhysics = true;
        ShowDungeonCollisionDebug = true;
        ShowUIDebugGrid = false; // Keep this off by default
    }

    /// <summary>
    /// Sets debug drawing to game mode (minimal debug info).
    /// </summary>
    public void SetGameMode()
    {
        EnableMinimalDebugDrawing();
    }

    /// <summary>
    /// Sets debug drawing to development mode (full debug info).
    /// </summary>
    public void SetDevelopmentMode()
    {
        EnableAllDebugDrawing();
    }

    /// <summary>
    /// Toggles between game mode and development mode.
    /// </summary>
    public void ToggleDebugMode()
    {
        if (ShowPhysicsDebug || ShowCombatDebug || ShowRenderingDebug)
        {
            SetGameMode();
        }
        else
        {
            SetDevelopmentMode();
        }
    }

    /// <summary>
    /// Toggles physics debug mode
    /// </summary>
    public void TogglePhysicsDebug()
    {
        ShowPhysicsDebug = !ShowPhysicsDebug;
        if (ShowPhysicsDebug)
        {
            DebugDrawEnabled = true;
        }
    }

    /// <summary>
    /// Toggles combat debug mode
    /// </summary>
    public void ToggleCombatDebug()
    {
        ShowCombatDebug = !ShowCombatDebug;
        if (ShowCombatDebug)
        {
            DebugDrawEnabled = true;
        }
    }

    /// <summary>
    /// Toggles rendering debug mode to diagnose position discrepancies
    /// </summary>
    public void ToggleRenderingDebug()
    {
        ShowRenderingDebug = !ShowRenderingDebug;
        if (ShowRenderingDebug)
        {
            DebugDrawEnabled = true;
        }
    }

    /// <summary>
    /// Toggles sprite alignment debug mode to diagnose hitbox issues
    /// </summary>
    public void ToggleSpriteAlignmentDebug()
    {
        ShowSpriteAlignment = !ShowSpriteAlignment;
        if (ShowSpriteAlignment)
        {
            DebugDrawEnabled = true;
        }
    }

    // NEW: Toggle dungeon collision debug overlay
    public void ToggleDungeonCollisionDebug()
    {
        ShowDungeonCollisionDebug = !ShowDungeonCollisionDebug;
        if (ShowDungeonCollisionDebug)
        {
            DebugDrawEnabled = true;
        }
    }
    #endregion

    #region Main Draw Methods
    /// <summary>
    /// Main debug draw method - draws all enabled debug visualizations
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, Player player, IEnumerable<NPC> npcs, IEnumerable<IDungeonElement> elements, Matrix viewMatrix, SpriteFont debugFont = null, IEnumerable<Rectangle> wallRects = null)
    {
        if (!DebugDrawEnabled) return;

        _frameCounter++;

        if (ShowPhysicsDebug)
        {
            DrawPhysicsDebug(spriteBatch, player, npcs);
        }

        if (ShowCombatDebug)
        {
            DrawCombatDebug(spriteBatch, player, npcs);
        }

        if (ShowRenderingDebug)
        {
            DrawRenderingDebug(spriteBatch, player, npcs, debugFont);
        }

        if (ShowSpriteAlignment)
        {
            DrawSpriteAlignmentDebug(spriteBatch, player, npcs, debugFont);
        }

        if (ShowDungeonElements && elements != null)
        {
            DrawDungeonElements(spriteBatch, elements, viewMatrix);
        }

        // NEW: draw dungeon element collision using their own DrawDebug (e.g., chests show collision)
        if (ShowDungeonCollisionDebug && elements != null)
        {
            foreach (var element in elements)
            {
                element?.DrawDebug(spriteBatch, _whitePixel);
            }
        }

        if (ShowUIDebugGrid)
        {
            DrawUIDebugGrid(spriteBatch, Core.GraphicsDevice.Viewport, 40, 40, Color.Black * 0.25f, debugFont);
        }

        if (ShowCollisionBounds && (wallRects != null || elements != null))
        {
            DrawCollisionBounds(spriteBatch, wallRects, elements, debugFont);
        }
    }

    /// <summary>
    /// Draws physics-related debug information like collision boxes, velocity, and AI targets.
    /// </summary>
    private void DrawPhysicsDebug(SpriteBatch spriteBatch, Player player, IEnumerable<NPC> npcs)
    {
        // Draw wall collisions
        var tilemap = TilesetManager.Instance.Tilemap;
        if (tilemap != null)
        {
            DrawWallCollisions(spriteBatch, tilemap);
        }

        // Draw player physics
        if (player != null)
        {
            DrawCharacterPhysics(spriteBatch, player, Color.LimeGreen);
        }

        // Draw NPC physics
        if (npcs != null)
        {
            foreach (var npc in npcs)
            {
                if (npc != null)
                {
                    DrawCharacterPhysics(spriteBatch, npc, npc.IsDefeated ? Color.Gray : Color.CornflowerBlue);
                }
            }
        }
    }

    /// <summary>
    /// Draws combat-related debug information like weapon hitboxes and attack ranges.
    /// </summary>
    private void DrawCombatDebug(SpriteBatch spriteBatch, Player player, IEnumerable<NPC> npcs)
    {
        // Draw player combat info
        if (player != null)
        {
            DrawCharacterCombat(spriteBatch, player, Color.Pink);
        }

        // Draw NPC combat info
        if (npcs != null)
        {
            if (_frameCounter % DEBUG_UPDATE_FREQUENCY == 0)
            {
                UpdateNpcDebugCache(npcs, player);
            }

            foreach (var npc in npcs)
            {
                if (npc != null && !npc.IsDefeated)
                {
                    DrawCharacterCombat(spriteBatch, npc, Color.OrangeRed);
                }
            }
        }
    }

    /// <summary>
    /// Draws detailed rendering debug information to diagnose position discrepancies
    /// </summary>
 private void DrawRenderingDebug(SpriteBatch spriteBatch, Player player, IEnumerable<NPC> npcs, SpriteFont debugFont)
    {
        // Draw player rendering info
        if (player != null)
        {
            DrawCharacterRenderingInfo(spriteBatch, player, "PLAYER", Color.LimeGreen, debugFont);
        }

        // Draw NPC rendering info
        if (npcs != null)
        {
            int npcIndex = 0;
            foreach (var npc in npcs)
            {
                if (npc != null && !npc.IsDefeated)
                {
                    DrawCharacterRenderingInfo(spriteBatch, npc, $"NPC_{npcIndex++}", Color.OrangeRed, debugFont);
                }
            }
        }
    }

    /// <summary>
    /// Draws sprite alignment debug info to diagnose hitbox snapping issues
    /// </summary>
 private void DrawSpriteAlignmentDebug(SpriteBatch spriteBatch, Player player, IEnumerable<NPC> npcs, SpriteFont debugFont)
    {
        // Draw player alignment
        if (player != null)
        {
            DrawCharacterAlignment(spriteBatch, player, Color.LimeGreen, debugFont);
        }

        // Draw NPC alignment
        if (npcs != null)
        {
            foreach (var npc in npcs)
            {
                if (npc != null && !npc.IsDefeated)
                {
                    DrawCharacterAlignment(spriteBatch, npc, Color.OrangeRed, debugFont);
                }
            }
        }
    }
    #endregion

    #region Character-Specific Debug Drawing
    /// <summary>
    /// Draws physics info for a single character.
    /// </summary>
    private void DrawCharacterPhysics(SpriteBatch spriteBatch, Character character, Color color)
    {
        // Draw logical position
        DrawCross(spriteBatch, _whitePixel, character.Position, 6, Color.Blue, 2);

        // Use Bounds property directly
        Rectangle bounds = character.Bounds;
        
        // Check for override
        Color drawColor = color;
        if (character.HasCollisionOverride)
        {
            drawColor = Color.Gold; // Distinct color for defensive shape
        }

        DrawRect(spriteBatch, bounds, drawColor * 0.8f);

        // Draw velocity vector
        Vector2 velocity = character.GetVelocity();
        if (velocity.LengthSquared() > 0.1f)
        {
            Vector2 center = new Vector2(bounds.Center.X, bounds.Center.Y);
            //DrawLine(spriteBatch, _whitePixel, center, center + Vector2.Normalize(velocity) * 20f, Color.Yellow);
        }

        // Draw detailed bounds if enabled
        if (ShowDetailedPhysics)
        {
            DrawSpriteAnalysisBounds(spriteBatch, character, color, Color.Yellow * 0.4f);
        }

        // Draw NPC-specific physics info
        if (character is NPC npc)
        {
            var movement = character.MovementComponent;
            if (movement != null)
            {
                // Draw chase target
                if (movement.ChaseTarget.HasValue)
                {
                    Vector2 center = new Vector2(bounds.Center.X, bounds.Center.Y);
                    DrawLine(spriteBatch, _whitePixel, center, movement.ChaseTarget.Value, Color.Cyan * 0.5f);
                    DrawCircle(spriteBatch, movement.ChaseTarget.Value, 3f, Color.Cyan);
                }
                // Draw stuck indicator
                if (npc.IsStuck)
                {
                    DrawCircleOutline(spriteBatch, new Vector2(bounds.Center.X, bounds.Center.Y), 10, Color.Red, 16);
                }
            }
        }
    }

    /// <summary>
    /// Draws combat info for a single character.
    /// </summary>
    private void DrawCharacterCombat(SpriteBatch spriteBatch, Character character, Color color)
    {
        if (character.EquippedWeapon == null) return;

        Rectangle tightBounds = character.GetTightSpriteBounds();
        Vector2 center = new Vector2(tightBounds.Left + tightBounds.Width / 2f, tightBounds.Top + tightBounds.Height / 2f);

        // Optionally draw the opaque region bounds for debugging hitbox generation
        if (ShowDetailedWeaponDebug)
        {
            character.EquippedWeapon.DrawOpaqueRegionBounds(spriteBatch, _whitePixel, center, Color.Yellow * 0.5f);
        }

        var combatPolygon = character.WeaponComponent?.GetCombatHitPolygon();
        if (combatPolygon != null && combatPolygon.Count >= 3)
        {
            var outlineColor = character.EquippedWeapon.IsSlashing ? color * 0.9f : color * 0.35f;
            if (character.EquippedWeapon.IsSlashing || ShowDetailedWeaponDebug)
            {
                DrawPolygonOutline(spriteBatch, _whitePixel, combatPolygon, outlineColor);
            }
        }
        else if (ShowDetailedWeaponDebug)
        {
            var fallbackPolygon = character.EquippedWeapon.GetTransformedHitPolygon(center);
            DrawPolygonOutline(spriteBatch, _whitePixel, fallbackPolygon, color * 0.35f);
        }

        // Draw NPC AI engagement info
        if (character is NPC npc && _npcDebugCache.ContainsKey(npc))
        {
            var cache = _npcDebugCache[npc];
            DrawCircleOutline(spriteBatch, center, cache.LastAttackRange, Color.Red * 0.5f, 12);
            DrawCircle(spriteBatch, cache.LastEngagementPoint, 2f, Color.Lime);
        }
    }

    private void DrawCharacterRenderingInfo(SpriteBatch spriteBatch, Character character, string label, Color color, SpriteFont font)
    {
        if (character?.Sprite == null) return;

        // Get all the different position/bounds values
        Vector2 logicalPos = character.Position;
        Vector2 spritePos = character.Sprite.Position;
        Rectangle bounds = character.Bounds;
        Rectangle tightBounds = character.GetTightSpriteBounds();
        
        // Draw logical position (large cross)
        //DrawCross(spriteBatch, _whitePixel, logicalPos, 12, Color.Red, 3);
        
        // Draw sprite position (medium cross)
        //DrawCross(spriteBatch, _whitePixel, spritePos, 8, Color.Yellow, 2);
        
        // Draw bounds center (small cross)
        Vector2 boundsCenter = new Vector2(bounds.Center.X, bounds.Center.Y);
        DrawCross(spriteBatch, _whitePixel, boundsCenter, 4, Color.Blue, 1);
        
        // Draw tight bounds center
        Vector2 tightCenter = new Vector2(tightBounds.Center.X, tightBounds.Center.Y);
        DrawCross(spriteBatch, _whitePixel, tightCenter, 4, Color.Magenta, 1);

        // Draw connecting lines
        DrawLine(spriteBatch, _whitePixel, logicalPos, spritePos, Color.White * 0.3f);
        DrawLine(spriteBatch, _whitePixel, spritePos, boundsCenter, Color.White * 0.3f);
        
        // Draw text info if font provided
        if (font != null)
        {
            Vector2 textPos = logicalPos + new Vector2(20, -40);
            float scale = FontScale * 0.5f;
            
            spriteBatch.DrawString(font, $"{label}", textPos, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(font, $"Pos: {(int)logicalPos.X},{(int)logicalPos.Y}", 
                textPos + new Vector2(0, font.LineSpacing * scale), Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(font, $"Sprite: {(int)spritePos.X},{(int)spritePos.Y}", 
                textPos + new Vector2(0, font.LineSpacing * scale * 2), Color.Yellow, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(font, $"Bounds: {bounds.X},{bounds.Y} {bounds.Width}x{bounds.Height}", 
                textPos + new Vector2(0, font.LineSpacing * scale * 3), Color.Blue, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(font, $"Tight: {tightBounds.X},{tightBounds.Y} {tightBounds.Width}x{tightBounds.Height}", 
                textPos + new Vector2(0, font.LineSpacing * scale * 4), Color.Magenta, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }

    private void DrawCharacterAlignment(SpriteBatch spriteBatch, Character character, Color color, SpriteFont font)
    {
        if (character?.Sprite == null) return;

        var sprite = character.Sprite;
        Rectangle spriteBounds = new Rectangle(
            (int)sprite.Position.X,
            (int)sprite.Position.Y,
            (int)sprite.Width,
            (int)sprite.Height
        );

        // Draw sprite bounds
        DrawRect(spriteBatch, spriteBounds, Color.Yellow * 0.5f, 2f);
        
        // Draw tight bounds
        Rectangle tightBounds = character.GetTightSpriteBounds();
        DrawRect(spriteBatch, tightBounds, Color.Magenta * 0.5f, 1f);

        // Calculate and draw offset vectors
        Vector2 spriteToLogical = character.Position - sprite.Position;
        Vector2 boundsOffset = new Vector2(character.Bounds.X - sprite.Position.X, character.Bounds.Y - sprite.Position.Y);
        
        // Draw offset info
        if (font != null && (Math.Abs(spriteToLogical.X) > 1 || Math.Abs(spriteToLogical.Y) > 1))
        {
            Vector2 midPoint = sprite.Position + spriteToLogical / 2;
            string offsetText = $"Offset: {(int)spriteToLogical.X},{(int)spriteToLogical.Y}";
            spriteBatch.DrawString(font, offsetText, midPoint, Color.Red, 0f, 
                font.MeasureString(offsetText) / 2, FontScale * 0.7f, SpriteEffects.None, 0f);
        }

        // Draw sprite origin
        if (sprite.Origin != Vector2.Zero)
        {
            Vector2 worldOrigin = sprite.Position + sprite.Origin;
            DrawCircle(spriteBatch, worldOrigin, 3f, Color.Purple);
            
            if (font != null)
            {
                string originText = $"Origin: {(int)sprite.Origin.X},{(int)sprite.Origin.Y}";
                spriteBatch.DrawString(font, originText, worldOrigin + new Vector2(5, 5), 
                    Color.Purple, 0f, Vector2.Zero, FontScale * 0.6f, SpriteEffects.None, 0f);
            }
        }
    }
    #endregion

    #region Weapon Debug Drawing
    private void DrawPolygonOutline(SpriteBatch spriteBatch, Texture2D pixel, IReadOnlyList<Vector2> polygon, Color color)
    {
        if (pixel == null || polygon == null || polygon.Count < 2)
        {
            return;
        }

        for (int i = 0; i < polygon.Count; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + 1) % polygon.Count];
            DrawLine(spriteBatch, pixel, a, b, color);
        }
    }
    #endregion

    #region Helper Methods for Cache and Performance
    /// <summary>
    /// Updates the cached debug information for NPCs to improve performance.
    /// </summary>
    private void UpdateNpcDebugCache(IEnumerable<NPC> npcs, Player player)
    {
        if (player == null) return;

        foreach (var npc in npcs)
        {
            if (npc.IsDefeated)
            {
                _npcDebugCache.Remove(npc);
                continue;
            }

            float attackRange = npc.GetEffectiveAttackRange();
            float desiredStandOff = attackRange - 5f;
            Vector2 engagementPoint = ComputeEngagementPointDebug(player.Bounds, npc.Bounds, desiredStandOff);

            _npcDebugCache[npc] = new DebugNpcCache
            {
                LastPlayerCenter = new Vector2(player.Bounds.Center.X, player.Bounds.Center.Y),
                LastEngagementPoint = engagementPoint,
                LastAttackRange = attackRange,
                LastUpdateFrame = _frameCounter
            };
        }

        // Clean up old cache entries
        if (_frameCounter % 60 == 0)
        {
            var toRemove = _npcDebugCache.Keys.Where(n => n.IsDefeated || !npcs.Contains(n)).ToList();
            foreach (var key in toRemove)
            {
                _npcDebugCache.Remove(key);
            }
        }
    }

    // Helper method that mirrors the NPC's ComputeEngagementPoint for debugging
    private static Vector2 ComputeEngagementPointDebug(Rectangle playerBounds, Rectangle npcBounds, float desiredStandOff)
    {
        Vector2 playerCenter = new Vector2(playerBounds.Left + playerBounds.Width / 2f, playerBounds.Top + playerBounds.Height / 2f);
        Vector2 selfCenter = new Vector2(npcBounds.Left + npcBounds.Width / 2f, npcBounds.Top + npcBounds.Height / 2f);

        float radius = MathF.Max(10f, desiredStandOff);

        // FIXED: Calculate angle from PLAYER to NPC (where NPC should be relative to player)
        // This ensures the engagement point is always positioned around the player center
        float baseAngle = MathF.Atan2(selfCenter.Y - playerCenter.Y, selfCenter.X - playerCenter.X);

        // Return the engagement point around the player center at the desired standoff distance
        return playerCenter + new Vector2(MathF.Cos(baseAngle), MathF.Sin(baseAngle)) * radius;
    }
    #endregion

    #region Environment and UI Debug Drawing
    private void DrawDungeonElements(SpriteBatch spriteBatch, IEnumerable<IDungeonElement> elements, Matrix viewMatrix)
    {
        foreach (var element in elements)
        {
            var boundsProperty = element.GetType().GetProperty("Bounds");
            if (boundsProperty != null)
            {
                var bounds = (Rectangle)boundsProperty.GetValue(element);
                DrawRect(spriteBatch, bounds, Color.Red * 0.5f);
            }
        }
    }

    private void DrawWallCollisions(SpriteBatch spriteBatch, Tilemap tilemap)
    {
        EnsureWallCollisionCache(tilemap);
        // Draw wall collision rectangles in world space
        foreach (var rect in _cachedWallCollisionRects)
        {
            DrawRect(spriteBatch, rect, Color.Blue * 0.3f);
        }
    }

    /// <summary>
    /// Draws a grid overlay in screen space to help align UI elements.
    /// </summary>
    public void DrawUIDebugGrid(SpriteBatch spriteBatch, Viewport viewport, int cellWidth, int cellHeight, Color color, SpriteFont font = null)
    {
        int width = viewport.Width;
        int height = viewport.Height;

        // Draw vertical grid lines (X axis is unchanged)
        for (int x = 0; x <= width; x += cellWidth)
        {
            spriteBatch.Draw(_whitePixel, new Rectangle(x, 0, 1, height), color);
            if (font != null)
            {
                string xLabel = x.ToString();
                // Y=height-2 puts label at bottom, not top
                spriteBatch.DrawString(font, xLabel, new Vector2(x + 2, height - font.LineSpacing * FontScale - 2), Color.White, 0f, Vector2.Zero, FontScale, SpriteEffects.None, 0f);
            }
        }

        // Draw horizontal grid lines (Y axis is now top-down, no inversion)
        for (int y = 0; y <= height; y += cellHeight)
        {
            spriteBatch.Draw(_whitePixel, new Rectangle(0, y, width, 1), color);
            if (font != null)
            {
                string yLabel = y.ToString();
                // Place label at left, top
                spriteBatch.DrawString(font, yLabel, new Vector2(2, y + 2), Color.White, 0f, Vector2.Zero, FontScale, SpriteEffects.None, 0f);
            }
        }

        // Draw axes (top and left)
        spriteBatch.Draw(_whitePixel, new Rectangle(0, 0, width, 2), Color.Red * 0.7f); // X axis at top
        spriteBatch.Draw(_whitePixel, new Rectangle(0, 0, 2, height), Color.Red * 0.7f); // Y axis at left

        if (font != null)
        {
            // "X" label at top right
            spriteBatch.DrawString(font, "X", new Vector2(width - font.MeasureString("X").X * FontScale - 4, 4), Color.Red, 0f, Vector2.Zero, FontScale, SpriteEffects.None, 0f);
            // "Y" label at top left
            spriteBatch.DrawString(font, "Y", new Vector2(4, 4), Color.Red, 0f, Vector2.Zero, FontScale, SpriteEffects.None, 0f);
            // "(0,0)" at top left
            spriteBatch.DrawString(font, "(0,0)", new Vector2(4, 4 + font.LineSpacing * FontScale + 2), Color.Red, 0f, Vector2.Zero, FontScale, SpriteEffects.None, 0f);
        }

        // Draw bounding boxes for Gum UI elements
        DrawUIElementBounds(spriteBatch, font);
    }

    /// <summary>
    /// Title screen-specific debug drawing. Currently draws the UI grid when enabled.
    /// </summary>
    public void DrawTitleScreen(SpriteBatch spriteBatch)
    {
        if (!DebugDrawEnabled || !ShowUIDebugGrid || spriteBatch == null)
            return;

        DrawUIDebugGrid(spriteBatch, spriteBatch.GraphicsDevice.Viewport, 40, 40, Color.Black * 0.25f);
    }

    /// <summary>
    /// Draws both full sprite bounds (yellow) and analyzed content bounds (green) for debugging
    /// </summary>
    public void DrawSpriteAnalysisBounds(SpriteBatch spriteBatch, Character character, Color contentColor, Color fullColor)
    {
        if (character?.Sprite == null) return;

        // Draw full sprite bounds
        var fullBounds = new Rectangle(
            (int)character.Position.X,
            (int)character.Position.Y,
            (int)character.Sprite.Width,
            (int)character.Sprite.Height
        );
        DrawRect(spriteBatch, fullBounds, fullColor);

        // Draw the actual content bounds (tight fit)
        Rectangle contentBounds = character.GetTightSpriteBounds();
        DrawRect(spriteBatch, contentBounds, contentColor);

        // Draw center points
        Vector2 fullCenter = new Vector2(fullBounds.Center.X, fullBounds.Center.Y);
        Vector2 contentCenter = new Vector2(contentBounds.Center.X, contentBounds.Center.Y);

        // Mark the centers with small crosses
        int crossSize = 4;
        DrawCross(spriteBatch, _whitePixel, fullCenter, crossSize, fullColor);
        DrawCross(spriteBatch, _whitePixel, contentCenter, crossSize, contentColor);
    }
    #endregion

    #region UI Element Debug Methods
    private void DrawUIElementBounds(SpriteBatch spriteBatch, SpriteFont font)
    {
        var root = GumService.Default.Root;
        foreach (var child in root.Children)
        {
            DrawVisualBoundsRecursive(spriteBatch, child, font, Vector2.Zero);
        }
    }

    private void DrawVisualBoundsRecursive(SpriteBatch spriteBatch, object visualObj, SpriteFont font, Vector2 parentOffset = default)
    {
        var type = visualObj.GetType();
        float x, y, w, h;
        string name = "";

        var absXProp = type.GetProperty("AbsoluteX");
        var absYProp = type.GetProperty("AbsoluteY");
        var absWProp = type.GetProperty("AbsoluteWidth");
        var absHProp = type.GetProperty("AbsoluteHeight");
        var nameProp = type.GetProperty("Name");

        if (absXProp != null && absYProp != null && absWProp != null && absHProp != null)
        {
            x = Convert.ToSingle(absXProp.GetValue(visualObj));
            y = Convert.ToSingle(absYProp.GetValue(visualObj));
            w = Convert.ToSingle(absWProp.GetValue(visualObj));
            h = Convert.ToSingle(absHProp.GetValue(visualObj));
        }
        else
        {
            var xProp = type.GetProperty("X");
            var yProp = type.GetProperty("Y");
            var wProp = type.GetProperty("Width");
            var hProp = type.GetProperty("Height");

            x = parentOffset.X + (xProp != null ? Convert.ToSingle(xProp.GetValue(visualObj)) : 0f);
            y = parentOffset.Y + (yProp != null ? Convert.ToSingle(yProp.GetValue(visualObj)) : 0f);
            w = wProp != null ? Convert.ToSingle(wProp.GetValue(visualObj)) : 0f;
            h = hProp != null ? Convert.ToSingle(hProp.GetValue(visualObj)) : 0f;
        }

        if (nameProp != null)
            name = nameProp.GetValue(visualObj)?.ToString() ?? "";

        // Only draw debug box for button backgrounds
        if (name == "StartButtonBackground" || name == "OptionsButtonBackground")
        {
            var rect = new Rectangle((int)x, (int)y, (int)w, (int)h);
            DrawRectScreen(spriteBatch, rect, Color.Cyan * 0.7f);

            if (font != null)
            {
                string label = $"{name} ({x},{y})";
                spriteBatch.DrawString(font, label, new Vector2(x + 2, y + 2), Color.Cyan, 0f, Vector2.Zero, FontScale, SpriteEffects.None, 0f);
            }
        }

        var childrenProp = type.GetProperty("Children");
        var children = childrenProp?.GetValue(visualObj) as System.Collections.IEnumerable;
        if (children != null)
        {
            foreach (var child in children)
            {
                DrawVisualBoundsRecursive(spriteBatch, child, font, new Vector2(x, y));
            }
        }
    }
    #endregion

    #region Wall Collision Cache Management
    private void EnsureWallCollisionCache(Tilemap tilemap)
    {
        // Rebuild cache if map size changes or cache is empty
        var size = new Point(tilemap.Columns, tilemap.Rows);
        if (_cachedWallCollisionRects != null && _cachedMapSize == size)
            return;

        _cachedMapSize = size;
        _cachedWallCollisionRects = new List<Rectangle>();

        var t = tilemap.GetType();

        // Prefer a direct API if it exists
        var getRectsMethod = t.GetMethod("GetWallCollisionRectangles") ?? t.GetMethod("GetCollisionRectangles");
        if (getRectsMethod != null && typeof(System.Collections.IEnumerable).IsAssignableFrom(getRectsMethod.ReturnType))
        {
            var result = getRectsMethod.Invoke(tilemap, null) as System.Collections.IEnumerable;
            if (result != null)
            {
                foreach (var item in result)
                {
                    if (item is Rectangle r) _cachedWallCollisionRects.Add(r);
                }
                return;
            }
        }

        var rectsProp = t.GetProperty("WallCollisionRectangles") ?? t.GetProperty("CollisionRectangles");
        if (rectsProp != null)
        {
            var result = rectsProp.GetValue(tilemap) as System.Collections.IEnumerable;
            if (result != null)
            {
                foreach (var item in result)
                {
                    if (item is Rectangle r) _cachedWallCollisionRects.Add(r);
                }
                return;
            }
        }

        // Fallback: build rectangles from a boolean "is wall" query if available
        Func<int, int, bool> isWall = null;
        var isWallMethod = t.GetMethod("IsWall") ?? t.GetMethod("IsBlocked") ?? t.GetMethod("IsSolid");
        if (isWallMethod != null && isWallMethod.GetParameters().Length == 2)
        {
            isWall = (c, r) => (bool)isWallMethod.Invoke(tilemap, new object[] { c, r });
        }
        else
        {
            // Try using tile IDs + a wall ID (if present on TilesetManager)
            int wallId = -1;
            var tm = TilesetManager.Instance;
            var wallIdProp = tm.GetType().GetProperty("WallTileId");
            if (wallIdProp != null)
            {
                wallId = Convert.ToInt32(wallIdProp.GetValue(tm));
            }
            var getTileId = t.GetMethod("GetTileId");
            if (getTileId != null && wallId != -1)
            {
                isWall = (c, r) => Convert.ToInt32(getTileId.Invoke(tilemap, new object[] { c, r })) == wallId;
            }
        }

        if (isWall != null)
        {
            // Merge horizontal runs of wall tiles into rectangles per row
            int rows = tilemap.Rows;
            int cols = tilemap.Columns;
            float tw = tilemap.TileWidth;
            float th = tilemap.TileHeight;

            for (int row = 0; row < rows; row++)
            {
                int runStart = -1;
                for (int col = 0; col <= cols; col++)
                {
                    bool wall = col < cols && isWall(col, row);
                    if (wall)
                    {
                        if (runStart == -1) runStart = col;
                    }
                    else if (runStart != -1)
                    {
                        int runEnd = col - 1;
                        var rect = new Rectangle(
                            (int)(runStart * tw),
                            (int)(row * th),
                            (int)((runEnd - runStart + 1) * tw),
                            (int)th
                        );
                        _cachedWallCollisionRects.Add(rect);
                        runStart = -1;
                    }
                }
            }
        }
        // If none of the above worked, leave cache empty to avoid incorrect visuals.
    }
    #endregion

    #region Primitive Drawing Helpers
    private static void DrawLine(SpriteBatch spriteBatch, Texture2D pixel, Vector2 a, Vector2 b, Color color, float thickness = 1f)
    {
        var distance = Vector2.Distance(a, b);
        if (distance < 0.001f) return;

        var angle = (float)Math.Atan2(b.Y - a.Y, b.X - a.X);
        spriteBatch.Draw(pixel, a, null, color, angle, Vector2.Zero, new Vector2(distance, thickness), SpriteEffects.None, 0f);
    }

    private void DrawRect(SpriteBatch spriteBatch, Rectangle rect, Color color, float thickness = 1f)
    {
        int th = (int)Math.Ceiling(thickness);
        // Top
        spriteBatch.Draw(_whitePixel, new Rectangle(rect.X, rect.Y, rect.Width, th), color);
        // Left
        spriteBatch.Draw(_whitePixel, new Rectangle(rect.X, rect.Y, th, rect.Height), color);
        // Right
        spriteBatch.Draw(_whitePixel, new Rectangle(rect.Right - th, rect.Y, th, rect.Height), color);
        // Bottom
        spriteBatch.Draw(_whitePixel, new Rectangle(rect.X, rect.Bottom - th, rect.Width, th), color);
    }

    private void DrawRectWithDepth(SpriteBatch spriteBatch, Rectangle rect, Color color, float thickness = 1f, float layerDepth = 0f)
    {
        int th = (int)Math.Ceiling(thickness);
        // Top
        spriteBatch.Draw(_whitePixel, new Rectangle(rect.X, rect.Y, rect.Width, th), null, color, 0f, Vector2.Zero, SpriteEffects.None, layerDepth);
        // Left
        spriteBatch.Draw(_whitePixel, new Rectangle(rect.X, rect.Y, th, rect.Height), null, color, 0f, Vector2.Zero, SpriteEffects.None, layerDepth);
        // Right
        spriteBatch.Draw(_whitePixel, new Rectangle(rect.Right - th, rect.Y, th, rect.Height), null, color, 0f, Vector2.Zero, SpriteEffects.None, layerDepth);
        // Bottom
        spriteBatch.Draw(_whitePixel, new Rectangle(rect.X, rect.Bottom - th, rect.Width, th), null, color, 0f, Vector2.Zero, SpriteEffects.None, layerDepth);
    }
    
    private void DrawRectScreen(SpriteBatch spriteBatch, Rectangle rect, Color color)
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

    private void DrawCross(SpriteBatch spriteBatch, Texture2D pixel, Vector2 center, int size, Color color, float thickness = 1f)
    {
        int th = (int)Math.Ceiling(thickness);
        spriteBatch.Draw(pixel, new Rectangle((int)(center.X - size / 2f), (int)(center.Y - (th-1)/2f), size, th), color);
        spriteBatch.Draw(pixel, new Rectangle((int)(center.X - (th-1)/2f), (int)(center.Y - size / 2f), th, size), color);
    }

    private void DrawCircleOutline(SpriteBatch spriteBatch, Vector2 center, float radius, Color color, int segments = 16, float thickness = 1f)
    {
        float angleStep = MathHelper.TwoPi / segments;
        Vector2 prevPoint = center + new Vector2(radius, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i;
            Vector2 newPoint = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            DrawLine(spriteBatch, _whitePixel, prevPoint, newPoint, color, thickness);
            prevPoint = newPoint;
        }
    }

    private void DrawCircle(SpriteBatch spriteBatch, Vector2 center, float radius, Color color)
    {
        if (radius <= 0) return;
        // A simple way to draw a filled circle is to scale a pixel texture.
        // This is not perfectly anti-aliased but is fast and easy for debugging.
        var texture = _whitePixel;
        var scale = radius * 2f; // Diameter
        var origin = new Vector2(0.5f, 0.5f); // Center of the pixel
        spriteBatch.Draw(texture, center, null, color, 0f, origin, scale, SpriteEffects.None, 0f);
    }

    /// <summary>
    /// Draws collision bounds debug overlay for walls, chests, and dungeon entities
    /// </summary>
    public void DrawCollisionBounds(SpriteBatch spriteBatch, IEnumerable<Rectangle> wallRects, IEnumerable<IDungeonElement> dungeonElements, SpriteFont debugFont)
    {
        System.Diagnostics.Debug.WriteLine($"DrawCollisionBounds called");
        
        float layerDepth = 0.9f; // High layer depth to render on top
        int boundsCount = 0;
        
        // Draw wall collision rectangles in blue
        if (wallRects != null)
        {
            foreach (var wallRect in wallRects)
            {
                DrawRectWithDepth(spriteBatch, wallRect, Color.Blue * 0.7f, 2f, layerDepth);
                boundsCount++;
            }
        }
        
        // Draw dungeon element bounds in different colors
        if (dungeonElements != null)
        {
            foreach (var element in dungeonElements)
            {
                var boundsProperty = element.GetType().GetProperty("Bounds");
                if (boundsProperty != null)
                {
                    var bounds = (Rectangle)boundsProperty.GetValue(element);
                    
                    // Different colors for different element types
                    Color elementColor = Color.Red * 0.7f; // Default red
                    
                    if (element.GetType().Name.Contains("Loot") || element.GetType().Name.Contains("Chest"))
                    {
                        elementColor = Color.Yellow * 0.7f; // Yellow for chests/loot
                    }
                    else if (element.GetType().Name.Contains("Switch"))
                    {
                        elementColor = Color.Green * 0.7f; // Green for switches
                    }
                    else if (element.GetType().Name.Contains("Trap"))
                    {
                        elementColor = Color.Purple * 0.7f; // Purple for traps
                    }
                    
                    DrawRectWithDepth(spriteBatch, bounds, elementColor, 2f, layerDepth);
                    boundsCount++;
                    
                    // Draw element type label if font is available
                    if (debugFont != null)
                    {
                        string elementType = element.GetType().Name;
                        Vector2 labelPosition = new Vector2(bounds.X + 2, bounds.Y + 2);
                        
                        // Very small text scale (1/4 of original size) with opacity
                        float textScale = 0.175f; // 0.7f / 4 = 0.175f for 1/4 size
                        Color textColor = Color.White * 0.8f; // Add opacity
                        Color bgColor = Color.Black * 0.6f; // Background with opacity
                        
                        // Draw text background
                        Vector2 textSize = debugFont.MeasureString(elementType) * textScale;
                        Rectangle textBg = new Rectangle((int)labelPosition.X - 1, (int)labelPosition.Y - 1, (int)textSize.X + 2, (int)textSize.Y + 2);
                        spriteBatch.Draw(_whitePixel, textBg, null, bgColor, 0f, Vector2.Zero, SpriteEffects.None, layerDepth);
                        
                        // Draw text
                        spriteBatch.DrawString(debugFont, elementType, labelPosition, textColor, 0f, Vector2.Zero, textScale, SpriteEffects.None, layerDepth);
                    }
                }
            }
        }
        
        // Draw legend/info text if font is available
        if (debugFont != null && boundsCount > 0)
        {
            string legendText = $"Collision Bounds ({boundsCount} objects)\nBlue: Walls | Yellow: Chests | Green: Switches | Purple: Traps | Red: Other";
            Vector2 legendPosition = new Vector2(10, 10);
            
            // Smaller legend text with opacity
            float legendScale = 0.2f; // 0.8f / 4 = 0.2f for 1/4 size
            Color legendTextColor = Color.White * 0.9f; // Slight opacity for readability
            Color legendBgColor = Color.Black * 0.7f; // Background with opacity
            
            // Draw legend background
            Vector2 legendSize = debugFont.MeasureString(legendText) * legendScale;
            Rectangle legendBg = new Rectangle((int)legendPosition.X - 5, (int)legendPosition.Y - 2, (int)legendSize.X + 10, (int)legendSize.Y + 4);
            spriteBatch.Draw(_whitePixel, legendBg, null, legendBgColor, 0f, Vector2.Zero, SpriteEffects.None, layerDepth);
            
            // Draw legend text
            spriteBatch.DrawString(debugFont, legendText, legendPosition, legendTextColor, 0f, Vector2.Zero, legendScale, SpriteEffects.None, layerDepth);
        }
        
        System.Diagnostics.Debug.WriteLine($"Drew {boundsCount} collision bounds");
    }
    #endregion
}
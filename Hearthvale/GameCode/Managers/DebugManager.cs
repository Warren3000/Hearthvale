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

public class DebugManager
{
    private static DebugManager _instance;
    public static DebugManager Instance => _instance ?? throw new InvalidOperationException("DebugManager not initialized. Call Initialize first.");

    public bool DebugDrawEnabled { get; set; } = true;
    public bool ShowCollisionBoxes { get; set; } = false;
    public bool ShowAttackAreas { get; set; } = false; // Changed to false by default
    public bool ShowUIOverlay { get; set; } = true;
    public bool ShowDetailedDebug { get; set; } = false;
    public bool ShowUIDebugGrid { get; set; } = false;
    public bool ShowDungeonElements { get; set; } = false; // Changed to false by default
    public bool ShowWallCollisions { get; set; } = false; // Changed to false by default
    public bool ShowTilesetViewer { get; set; } = false;

    // NEW: Always-on weapon hitbox outlines (uses Weapon.HitPolygon)
    public bool ShowWeaponHitboxes { get; set; } = false;

    // Performance optimizations
    private int _frameCounter = 0;
    private const int DEBUG_UPDATE_FREQUENCY = 1; // Only update debug visuals every 10 frames
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
        public bool IsChaseNpc;
        public int LastUpdateFrame;
    }

    /// <summary>
    /// The scale factor for all debug font rendering.
    /// </summary>
    public float FontScale { get; set; } = 1f;

    private DebugManager(Texture2D whitePixel)
    {
        _whitePixel = whitePixel;
#if DEBUG
        DebugDrawEnabled = true;
        ShowUIOverlay = true; // Keep UI overlay enabled for debug info
        // Keep most debug features off by default to avoid lag
#else
        DebugDrawEnabled = false;
        ShowCollisionBoxes = false;
        ShowAttackAreas = false;
        ShowUIOverlay = false;
        ShowDungeonElements = false;
        ShowWallCollisions = false;
        ShowWeaponHitboxes = false;
#endif
    }

    /// <summary>
    /// Initializes the singleton instance. Call this once at startup.
    /// </summary>
    public static void Initialize(Texture2D whitePixel)
    {
        _instance = new DebugManager(whitePixel);
    }

    /// <summary>
    /// Disables all debug drawing features. Useful for clean gameplay.
    /// </summary>
    public void DisableAllDebugDrawing()
    {
        DebugDrawEnabled = false;
        ShowCollisionBoxes = false;
        ShowAttackAreas = false;
        ShowDungeonElements = false;
        ShowWallCollisions = false;
        ShowUIDebugGrid = false;
        ShowWeaponHitboxes = false;
    }

    /// <summary>
    /// Enables minimal debug drawing for performance.
    /// </summary>
    public void EnableMinimalDebugDrawing()
    {
        DebugDrawEnabled = true;
        ShowCollisionBoxes = false;
        ShowAttackAreas = false;
        ShowDungeonElements = false;
        ShowWallCollisions = false;
        ShowUIDebugGrid = false;
        ShowWeaponHitboxes = false;
        ShowUIOverlay = true; // Always keep UI overlay enabled for basic debug info
    }

    /// <summary>
    /// Enables all debug drawing features. WARNING: May cause performance issues.
    /// </summary>
    public void EnableAllDebugDrawing()
    {
        DebugDrawEnabled = true;
        ShowCollisionBoxes = true;
        ShowAttackAreas = true;
        ShowDungeonElements = true;
        ShowWallCollisions = true;
        ShowUIDebugGrid = false; // Keep this off by default as it can be intrusive
        ShowWeaponHitboxes = true;
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
        if (ShowCollisionBoxes || ShowAttackAreas || ShowDungeonElements || ShowWeaponHitboxes)
        {
            SetGameMode();
        }
        else
        {
            SetDevelopmentMode();
        }
    }

    public void Draw(SpriteBatch spriteBatch, Player player, IEnumerable<NPC> npcs, IEnumerable<IDungeonElement> elements, Matrix viewMatrix)
    {
        if (!DebugDrawEnabled) return;

        _frameCounter++;

        // Character collision boxes (player and NPCs)
        if (ShowCollisionBoxes)
        {
            DrawCharacterCollisionBoxes(spriteBatch, player, npcs);
            
            // Draw NPC movement debug visualization using public method
            if (npcs != null)
            {
                foreach (var npc in npcs)
                {
                    if (npc != null)
                    {
                        npc.DrawMovementDebug(spriteBatch, _whitePixel);
                    }
                }
            }
        }
        
        // Wall collision boxes (cached, so less expensive)
        if (ShowWallCollisions)
        {
            var tilemap = TilesetManager.Instance.Tilemap;
            DrawWallCollisions(spriteBatch, tilemap);
        }

        if (ShowAttackAreas)
        {
            DrawAttackArea(spriteBatch, player, npcs);
            // Only update NPC AI debug every few frames to reduce lag
            if (_frameCounter % DEBUG_UPDATE_FREQUENCY == 0)
            {
                DrawNpcAIDebug(spriteBatch, npcs, player);
            }
        }

        // NEW: Weapon hitbox outlines (uses the polygon WeaponHitboxGenerator assigned to Weapon.HitPolygon)
        if (ShowWeaponHitboxes)
        {
            DrawWeaponHitboxes(spriteBatch, player, npcs);
        }

        // Dungeon element collision boxes (only if there are few elements)
        if (ShowDungeonElements && elements?.Count() < 50 // Limit to avoid lag
        )
        {
            DrawDungeonElements(spriteBatch, elements, viewMatrix);
        }

        // Draw UI debug grid if enabled
        if (ShowUIDebugGrid)
        {
            DrawUIDebugGrid(spriteBatch, Core.GraphicsDevice.Viewport, 40, 40, Color.Black * 0.25f);
        }
    }

    /// <summary>
    /// Draws collision boxes for player and NPCs
    /// </summary>
    private void DrawCharacterCollisionBoxes(SpriteBatch spriteBatch, Player player, IEnumerable<NPC> npcs)
    {
        if (!DebugDrawEnabled || !ShowCollisionBoxes) return;

        // Draw player bounds (green)
        if (player != null)
        {
            // Use the tight bounds from sprite analysis
            Rectangle bounds = player.GetTightSpriteBounds();
            DrawRect(spriteBatch, bounds, Color.LimeGreen * 0.7f);
            
            // Also show full sprite bounds for comparison if detailed debug is enabled
            if (ShowDetailedDebug)
            {
                DrawSpriteAnalysisBounds(spriteBatch, player, Color.LimeGreen * 0.7f, Color.Yellow * 0.4f);
            }
        }

        // Draw NPC bounds (red for alive, gray for defeated)
        foreach (var npc in npcs)
        {
            Color boxColor = npc.IsDefeated ? Color.Gray * 0.5f : Color.Purple * 0.7f;
            
            // Use the tight bounds from sprite analysis
            Rectangle bounds = npc.GetTightSpriteBounds();
            DrawRect(spriteBatch, bounds, boxColor);
            
            // Show full sprite bounds for comparison if detailed debug is enabled
            if (ShowDetailedDebug)
            {
                DrawSpriteAnalysisBounds(spriteBatch, npc, boxColor, Color.Yellow * 0.4f);
            }
        }
    }

    /// <summary>
    /// Optimized NPC AI debugging - only updates cache periodically and limits drawing
    /// </summary>
    public void DrawNpcAIDebug(SpriteBatch spriteBatch, IEnumerable<NPC> npcs, Player player)
    {
        if (!DebugDrawEnabled || !ShowAttackAreas || player == null) return;

        int npcCount = 0;
        const int MAX_DEBUG_NPCS = 5; // Limit number of NPCs to debug at once

        foreach (var npc in npcs)
        {
            if (npc.IsDefeated || npcCount >= MAX_DEBUG_NPCS) continue;

            // Only debug knight NPCs to reduce visual clutter
            if (!npc.Name.ToLower().Contains("knight")) continue;

            npcCount++;

            Vector2 npcCenter = npc.Position + new Vector2(npc.Sprite.Width / 2f, npc.Sprite.Height / 2f);
            Vector2 playerCenter = player.Position + new Vector2(player.Sprite.Width / 2f, player.Sprite.Height / 2f);

            // Update cache only periodically
            if (!_npcDebugCache.ContainsKey(npc) ||
                _frameCounter - _npcDebugCache[npc].LastUpdateFrame > DEBUG_UPDATE_FREQUENCY)
            {
                float attackRange = MathF.Max(npc.EquippedWeapon?.Length ?? 32f, 32f);
                float desiredStandOff = attackRange - 3f;
                Vector2 engagementPoint = ComputeEngagementPointDebug(player.Bounds, npc.Bounds, desiredStandOff);

                _npcDebugCache[npc] = new DebugNpcCache
                {
                    LastPlayerCenter = playerCenter,
                    LastEngagementPoint = engagementPoint,
                    LastAttackRange = attackRange,
                    IsChaseNpc = npc.Name.ToLower().Contains("knight"),
                    LastUpdateFrame = _frameCounter
                };
            }

            var cache = _npcDebugCache[npc];

            // Draw simplified debug info
            if (cache.IsChaseNpc)
            {
                // Simple line to player (reduced opacity to reduce visual noise)
                DrawLine(spriteBatch, _whitePixel, npcCenter, cache.LastPlayerCenter, Color.Yellow * 0.3f);

                // Attack range circle (thinner line)
                DrawCircleOutline(spriteBatch, npcCenter, cache.LastAttackRange, Color.Red * 0.5f, 8); // Fewer segments

                // Engagement target (smaller dot)
                DrawCircle(spriteBatch, cache.LastEngagementPoint, 2f, Color.Lime * 0.8f);
            }
        }

        // Clean up old cache entries
        if (_frameCounter % (DEBUG_UPDATE_FREQUENCY * 10) == 0)
        {
            var toRemove = _npcDebugCache.Keys.Where(npc => npc.IsDefeated).ToList();
            foreach (var npc in toRemove)
            {
                _npcDebugCache.Remove(npc);
            }
        }
    }

    /// <summary>
    /// Draws weapon swing arc and active hit polygon while slashing.
    /// </summary>
    private void DrawAttackArea(SpriteBatch spriteBatch, Player player, IEnumerable<NPC> npcs)
    {
        // Player swing debug
        if (player?.EquippedWeapon?.IsSlashing == true)
        {
            Vector2 playerCenter = player.Position + new Vector2(player.Sprite.Width / 2f, player.Sprite.Height / 2f);

            // Arc + hit polygon in world space; SpriteBatch camera transform will handle anchoring/zoom
            DrawSwingArcForWeapon(spriteBatch, _whitePixel, player.EquippedWeapon, playerCenter, Color.Orange, MathHelper.ToRadians(40), 24);
            DrawHitPolygonForWeapon(spriteBatch, _whitePixel, player.EquippedWeapon, playerCenter, Color.Red);
        }

        // NPC swing debug
        foreach (var npc in npcs)
        {
            if (npc?.EquippedWeapon?.IsSlashing == true)
            {
                Vector2 npcCenter = npc.Position + new Vector2(npc.Sprite.Width / 2f, npc.Sprite.Height / 2f);
                float halfArc = MathHelper.PiOver4 + MathHelper.PiOver2; // preserves prior debug setting

                DrawSwingArcForWeapon(spriteBatch, _whitePixel, npc.EquippedWeapon, npcCenter, Color.Orange, halfArc, 24);
                DrawHitPolygonForWeapon(spriteBatch, _whitePixel, npc.EquippedWeapon, npcCenter, Color.Red);
            }
        }
    }

    // NEW: Always-on visualization of a weapon's current hitbox (idle or slashing)
    private void DrawWeaponHitboxes(SpriteBatch spriteBatch, Player player, IEnumerable<NPC> npcs)
    {
        // Player weapon
        if (player?.EquippedWeapon != null)
        {
            Vector2 center = player.Position + new Vector2(player.Sprite.Width / 2f, player.Sprite.Height / 2f);
            DrawHitPolygonForWeapon(spriteBatch, _whitePixel, player.EquippedWeapon, center, Color.LimeGreen * 0.9f);
        }

        // NPC weapons
        foreach (var npc in npcs)
        {
            if (npc?.EquippedWeapon == null) continue;
            Vector2 center = npc.Position + new Vector2(npc.Sprite.Width / 2f, npc.Sprite.Height / 2f);
            DrawHitPolygonForWeapon(spriteBatch, _whitePixel, npc.EquippedWeapon, center, Color.Cyan * 0.9f);
        }
    }

    // Unified helpers for arc + polygon debug rendering (moved from Weapon to DebugManager)
    private void DrawSwingArcForWeapon(SpriteBatch spriteBatch, Texture2D pixel, Weapon weapon, Vector2 ownerCenter, Color color, float? halfArcOverrideRadians = null, int segments = Weapon.DefaultDebugArcSegments)
    {
        if (weapon == null || pixel == null) return;

        var spec = weapon.GetDebugSwingSpec(halfArcOverrideRadians);

        float angleStep = (spec.EndAngle - spec.StartAngle) / segments;
        float innerRadius = spec.HandleOffset;
        float outerRadius = spec.HandleOffset + spec.BladeLength;

        // Use the weapon’s actual world origin (same as sprite) instead of ownerCenter
        Vector2 origin = weapon.GetWorldOrigin(ownerCenter);

        Vector2 ToPoint(float angle, float radius) => origin + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;

        Vector2 prevInner = ToPoint(spec.StartAngle, innerRadius);
        Vector2 prevOuter = ToPoint(spec.StartAngle, outerRadius);

        for (int i = 1; i <= segments; i++)
        {
            float angle = spec.StartAngle + angleStep * i;
            Vector2 inner = ToPoint(angle, innerRadius);
            Vector2 outer = ToPoint(angle, outerRadius);

            DrawLine(spriteBatch, pixel, prevOuter, outer, color);
            DrawLine(spriteBatch, pixel, prevInner, inner, color);
            DrawLine(spriteBatch, pixel, prevInner, prevOuter, color);

            prevInner = inner;
            prevOuter = outer;
        }
    }

    private void DrawHitPolygonForWeapon(SpriteBatch spriteBatch, Texture2D pixel, Weapon weapon, Vector2 ownerCenter, Color color)
    {
        if (weapon == null || pixel == null) return;

        var poly = weapon.GetTransformedHitPolygon(ownerCenter);
        for (int i = 0; i < poly.Count; i++)
        {
            var a = poly[i];
            var b = poly[(i + 1) % poly.Count];
            DrawLine(spriteBatch, pixel, a, b, color);
        }
    }

    private static void DrawLine(SpriteBatch spriteBatch, Texture2D pixel, Vector2 a, Vector2 b, Color color)
    {
        var distance = Vector2.Distance(a, b);
        if (distance < 0.001f) return;

        var angle = (float)Math.Atan2(b.Y - a.Y, b.X - a.X);
        spriteBatch.Draw(pixel, a, null, color, angle, Vector2.Zero, new Vector2(distance, 1f), SpriteEffects.None, 0f);
    }

    private void DrawDungeonElements(SpriteBatch spriteBatch, IEnumerable<IDungeonElement> elements, Matrix viewMatrix)
    {
        if (!DebugDrawEnabled || !ShowDungeonElements) return;

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
    private void DrawWallCollisions(SpriteBatch spriteBatch, Tilemap tilemap)
    {
        if (!DebugDrawEnabled || !ShowWallCollisions || tilemap == null) return;
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
        string tag = "";

        var absXProp = type.GetProperty("AbsoluteX");
        var absYProp = type.GetProperty("AbsoluteY");
        var absWProp = type.GetProperty("AbsoluteWidth");
        var absHProp = type.GetProperty("AbsoluteHeight");
        var nameProp = type.GetProperty("Name");
        var tagProp = type.GetProperty("Tag"); // <-- Add this

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
            DrawRect(spriteBatch, rect, Color.Cyan * 0.7f);

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

    /// <summary>
    /// Title screen-specific debug drawing. Currently draws the UI grid when enabled.
    /// </summary>
    public void DrawTitleScreen(SpriteBatch spriteBatch)
    {
        if (!DebugDrawEnabled || !ShowUIDebugGrid || spriteBatch == null)
            return;

        DrawUIDebugGrid(spriteBatch, spriteBatch.GraphicsDevice.Viewport, 40, 40, Color.Black * 0.25f);
    }

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

    private void DrawCircleOutline(SpriteBatch spriteBatch, Vector2 center, float radius, Color color, int segments = 16)
    {
        float angleStep = MathHelper.TwoPi / segments;
        Vector2 prevPoint = center + new Vector2(radius, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i;
            Vector2 newPoint = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            DrawLine(spriteBatch, _whitePixel, prevPoint, newPoint, color);
            prevPoint = newPoint;
        }
    }

    private void DrawCircle(SpriteBatch spriteBatch, Vector2 center, float radius, Color color)
    {
        int size = (int)(radius * 2);
        Rectangle rect = new Rectangle((int)(center.X - radius), (int)(center.Y - radius), size, size);
        spriteBatch.Draw(_whitePixel, rect, color);
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

    // Remove or comment out the verbose console logging from NPC.cs
    // The logging in Update method should be removed or wrapped in preprocessor directives

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
        DrawLine(spriteBatch, _whitePixel,
            fullCenter - new Vector2(crossSize, 0),
            fullCenter + new Vector2(crossSize, 0),
            fullColor);
        DrawLine(spriteBatch, _whitePixel,
            fullCenter - new Vector2(0, crossSize),
            fullCenter + new Vector2(0, crossSize),
            fullColor);

        DrawLine(spriteBatch, _whitePixel,
            contentCenter - new Vector2(crossSize, 0),
            contentCenter + new Vector2(crossSize, 0),
            contentColor);
        DrawLine(spriteBatch, _whitePixel,
            contentCenter - new Vector2(0, crossSize),
            contentCenter + new Vector2(0, crossSize),
            contentColor);
    }
}
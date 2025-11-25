using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System;
using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.Utils;
using MonoGameLibrary.Graphics;
using Hearthvale.GameCode.Collision;
using MonoGame.Extended;

public enum ChestState
{
    Closed,
    Opening,
    Opened
}

public class LootTable
{
    public string Id { get; }
    public List<LootEntry> Entries { get; }

    public LootTable(string id)
    {
        Id = id;
        Entries = new List<LootEntry>();
    }

    public List<string> GenerateLoot()
    {
        var loot = new List<string>();
        var random = new Random();
        foreach (var entry in Entries)
        {
            if (random.NextDouble() <= entry.DropChance)
            {
                loot.Add(entry.ItemId);
            }
        }
        return loot;
    }
}

public class LootEntry
{
    public string ItemId { get; }
    public double DropChance { get; }
    public int MinQuantity { get; }
    public int MaxQuantity { get; }

    public LootEntry(string itemId, double dropChance, int minQuantity = 1, int maxQuantity = 1)
    {
        ItemId = itemId;
        DropChance = dropChance;
        MinQuantity = minQuantity;
        MaxQuantity = maxQuantity;
    }
}

public class DungeonLoot : IDungeonElement
{
    public string Id { get; }
    public bool IsActive { get; private set; }
    public LootTable LootTable { get; }
    public int Column { get; }
    public int Row { get; }
    public bool IsTrapped { get; }
    public string TrapId { get; }
    public ChestState State { get; private set; } = ChestState.Closed;
    public bool IsOpened => State == ChestState.Opened;

    private List<string> _generatedLoot = new();

    private AnimatedSprite _closedIdleSprite;
    private AnimatedSprite _openingSprite;
    private AnimatedSprite _openedIdleSprite;
    private AnimatedSprite _currentSprite;

    private double _openingElapsed;
    private double _openingDurationSeconds;

    // Collision actor (optional if collision world attached)
    private ChestCollisionActor _collisionActor;
    private CollisionWorld _collisionWorld;

    // Cache last tight bounds to avoid redundant updates
    private Rectangle _lastTightBounds;

    public Rectangle Bounds
    {
        get
        {
            int size = GetTileSize();
            return new Rectangle(Column * size, Row * size, size, size);
        }
    }

    public DungeonLoot(string id, string lootTableId, int column = 0, int row = 0,
        bool isTrapped = false, string trapId = null)
    {
        Id = id;
        Column = column;
        Row = row;
        IsTrapped = isTrapped;
        TrapId = trapId;
        LootTable = new LootTable(lootTableId);

        Hearthvale.GameCode.Rendering.DungeonLootRenderer.Register(this);
    }

    internal void InitializeAnimations(TextureAtlas atlas, string closedIdleAnim, string openingAnim, string openedIdleAnim)
    {
        _closedIdleSprite = atlas.HasAnimation(closedIdleAnim) ? atlas.CreateAnimatedSprite(closedIdleAnim) : null;
        _openingSprite = atlas.HasAnimation(openingAnim) ? atlas.CreateAnimatedSprite(openingAnim) : null;
        _openedIdleSprite = !string.IsNullOrEmpty(openedIdleAnim) && atlas.HasAnimation(openedIdleAnim)
            ? atlas.CreateAnimatedSprite(openedIdleAnim)
            : null;

        _currentSprite = _closedIdleSprite ?? _openingSprite ?? _openedIdleSprite;

        if (_openingSprite?.Animation != null)
        {
            var frames = _openingSprite.Animation.Frames?.Count ?? 1;
            var delay = _openingSprite.Animation.Delay.TotalSeconds;
            _openingDurationSeconds = Math.Max(0.01, frames * delay);
        }

        // Initialize initial collision bounds if a collision world already attached
        if (_collisionWorld != null)
        {
            EnsureCollisionActorInitialized();
            UpdateCollisionActorBounds(force: true);
        }
    }

    public void Activate()
    {
        if (State == ChestState.Closed)
        {
            State = ChestState.Opening;
            _currentSprite = _openingSprite ?? _openedIdleSprite ?? _closedIdleSprite;
            _generatedLoot = LootTable?.GenerateLoot() ?? new List<string>();

            if (IsTrapped && !string.IsNullOrEmpty(TrapId))
            {
                var trap = DungeonManager.Instance.GetElement<DungeonTrap>(TrapId);
                trap?.Activate();
            }
        }
    }

    public void Deactivate() { }

    public void Update(GameTime gameTime)
    {
        if (_currentSprite == null) return;

        _currentSprite.Update(gameTime);

        if (State == ChestState.Opening)
        {
            _openingElapsed += gameTime.ElapsedGameTime.TotalSeconds;
            if (_openingElapsed >= _openingDurationSeconds)
            {
                State = ChestState.Opened;
                _currentSprite = _openedIdleSprite ?? _openingSprite ?? _closedIdleSprite;
                IsActive = true;
            }
        }

        // Keep collision in sync
        if (_collisionActor != null)
        {
            UpdateCollisionActorBounds();
        }
    }

    public void DrawDebug(SpriteBatch spriteBatch, Texture2D pixel)
    {
        var color = State switch
        {
            ChestState.Closed => (IsTrapped ? Color.DarkRed : Color.SaddleBrown),
            ChestState.Opening => Color.Goldenrod,
            ChestState.Opened => Color.Gold,
            _ => Color.Brown
        };
        spriteBatch.Draw(pixel, Bounds, color * 0.25f);

        // Show tight collision bounds if different
        if (_collisionActor?.Bounds is RectangleF rf)
        {
            var rect = new Rectangle((int)rf.X, (int)rf.Y, (int)rf.Width, (int)rf.Height);
            spriteBatch.Draw(pixel, rect, Color.Lime * 0.35f);
        }
    }

    public List<string> TryOpen()
    {
        if (State == ChestState.Opened)
            return new List<string>(_generatedLoot);

        if (State == ChestState.Closed)
        {
            Activate();
            return new List<string>(); // Loot accessible after opening
        }
        return new List<string>();
    }

    public List<string> GetLoot() =>
        State == ChestState.Opened ? new List<string>(_generatedLoot) : new List<string>();

    public bool IsPositionOnContainer(float x, float y, int tileSize = 32)
    {
        var containerX = Column * tileSize;
        var containerY = Row * tileSize;
        return x >= containerX && x < containerX + tileSize &&
               y >= containerY && y < containerY + tileSize;
    }

    internal AnimatedSprite GetSprite() => _currentSprite;

    public void AttachCollision(CollisionWorld world)
    {
        if (world == null || _collisionWorld == world) return;
        _collisionWorld = world;
        EnsureCollisionActorInitialized();
        UpdateCollisionActorBounds(force: true);
        if (_collisionActor != null)
        {
            _collisionWorld.AddActor(_collisionActor);
        }
    }

    private void EnsureCollisionActorInitialized()
    {
        if (_collisionActor != null) return;
        var tight = ComputeTightWorldBounds();
        if (tight.Width <= 0 || tight.Height <= 0)
        {
            // fallback to tile bounds
            tight = Bounds;
        }
        _collisionActor = new ChestCollisionActor(this, tight);
    }

    private void UpdateCollisionActorBounds(bool force = false)
    {
        var tight = ComputeTightWorldBounds();
        if (force || tight != _lastTightBounds)
        {
            _collisionActor.SyncFromLoot();
            _lastTightBounds = tight;
        }
    }

    /// <summary>
    /// Computes the tight opaque pixel AABB in world space for current frame.
    /// </summary>
    private Rectangle ComputeTightWorldBounds()
    {
        // Use a standard collision box for loot (slightly smaller than tile)
        int tileSize = GetTileSize();
        int padding = 4;
        
        return new Rectangle(
            Column * tileSize + padding,
            Row * tileSize + padding,
            tileSize - (padding * 2),
            tileSize - (padding * 2)
        );
    }

    private static int GetTileSize()
    {
        try
        {
            var map = TilesetManager.Instance?.Tilemap;
            if (map != null)
                return (int)MathF.Round(MathF.Max(map.TileWidth, map.TileHeight));
        }
        catch { }
        return 32;
    }
}
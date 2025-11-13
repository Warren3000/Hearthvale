using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;

namespace Hearthvale.GameCode.Data.Atlases.Models;

public sealed class NpcAtlasDefinition
{
    private readonly Dictionary<string, NpcAnimationTemplate> _animations;

    public NpcAtlasDefinition(
        string id,
        TextureAtlas atlas,
        Dictionary<string, NpcAnimationTemplate> animations,
        NpcCollisionProfile collision,
        IReadOnlyList<NpcAttachmentDescriptor> attachments)
    {
        Id = id;
        Atlas = atlas;
        _animations = animations ?? throw new ArgumentNullException(nameof(animations));
        Collision = collision;
        Attachments = attachments;
    }

    public string Id { get; }
    public TextureAtlas Atlas { get; }
    public NpcCollisionProfile Collision { get; }
    public IReadOnlyList<NpcAttachmentDescriptor> Attachments { get; }

    public Dictionary<string, Animation> CreateAnimations()
    {
        var map = new Dictionary<string, Animation>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in _animations)
        {
            map[pair.Key] = pair.Value.CreateInstance();
        }

        return map;
    }
}

public readonly struct NpcCollisionProfile
{
    public static readonly NpcCollisionProfile Empty = new(Rectangle.Empty, null);

    public NpcCollisionProfile(Rectangle hitbox, Vector2? projectileOrigin)
    {
        Hitbox = hitbox;
        ProjectileOrigin = projectileOrigin;
    }

    public Rectangle Hitbox { get; }
    public Vector2? ProjectileOrigin { get; }
}

public readonly struct NpcAttachmentDescriptor
{
    public NpcAttachmentDescriptor(string slot, string atlas)
    {
        Slot = slot;
        Atlas = atlas;
    }

    public string Slot { get; }
    public string Atlas { get; }
}

public sealed class NpcAnimationTemplate
{
    private readonly List<TextureRegion> _frames;
    private readonly TimeSpan _delay;

    public NpcAnimationTemplate(List<TextureRegion> frames, TimeSpan delay)
    {
        _frames = frames ?? throw new ArgumentNullException(nameof(frames));
        _delay = delay;
    }

    public Animation CreateInstance()
    {
        return new Animation(new List<TextureRegion>(_frames), _delay);
    }
}
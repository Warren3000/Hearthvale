using Hearthvale.GameCode.Data.Atlases.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Hearthvale.GameCode.Data.Atlases;

public sealed class ManifestNpcAtlasCatalog : INpcAtlasCatalog
{
    private readonly ContentManager _content;
    private readonly Dictionary<string, NpcAtlasDefinition> _definitions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TextureAtlas> _atlasCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public ManifestNpcAtlasCatalog(ContentManager content)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
    }

    public void LoadManifest(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("Path must be provided.", nameof(relativePath));
        }

        var manifest = ReadManifest(relativePath);

        if (manifest?.Npcs == null)
        {
            return;
        }

        foreach (var npc in manifest.Npcs)
        {
            if (string.IsNullOrWhiteSpace(npc.Id) || string.IsNullOrWhiteSpace(npc.TextureAtlas))
            {
                continue;
            }

            var atlas = LoadAtlas(npc.TextureAtlas);
            var definition = BuildDefinition(npc, atlas);
            _definitions[npc.Id] = definition;
        }
    }

    public bool TryGetDefinition(string npcId, out NpcAtlasDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(npcId))
        {
            definition = null;
            return false;
        }

        return _definitions.TryGetValue(npcId, out definition);
    }

    private TextureAtlas LoadAtlas(string atlasPath)
    {
        if (_atlasCache.TryGetValue(atlasPath, out var cached))
        {
            return cached;
        }

        var atlas = TextureAtlas.FromFile(_content, atlasPath);
        _atlasCache[atlasPath] = atlas;
        return atlas;
    }

    private NpcAtlasDefinition BuildDefinition(NpcManifestEntry entry, TextureAtlas atlas)
    {
        var animations = new Dictionary<string, NpcAnimationTemplate>(StringComparer.OrdinalIgnoreCase);

        if (entry.Animations != null)
        {
            foreach (var kvp in entry.Animations)
            {
                var alias = kvp.Key;
                var anim = kvp.Value;

                if (string.IsNullOrWhiteSpace(alias) || anim == null)
                {
                    continue;
                }

                var resolvedName = ResolveAtlasAnimationName(alias, anim.AtlasAnimation, atlas);

                if (resolvedName == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Missing animation for NPC '{entry.Id}'. Requested='{anim.AtlasAnimation}' Alias='{alias}'.");
                    continue;
                }

                var baseAnimation = atlas.GetAnimation(resolvedName);
                var frames = new List<TextureRegion>(baseAnimation.Frames);
                var delay = baseAnimation.Delay;

                if (anim.Fps.HasValue && anim.Fps.Value > 0)
                {
                    delay = TimeSpan.FromSeconds(1f / anim.Fps.Value);
                }

                animations[alias] = new NpcAnimationTemplate(frames, delay);
            }
        }

        var collision = entry.Collision != null
            ? new NpcCollisionProfile(
                new Rectangle(
                    entry.Collision.Hitbox?.X ?? 0,
                    entry.Collision.Hitbox?.Y ?? 0,
                    entry.Collision.Hitbox?.Width ?? 0,
                    entry.Collision.Hitbox?.Height ?? 0),
                entry.Collision.ProjectileOrigin != null
                    ? new Vector2(entry.Collision.ProjectileOrigin.X, entry.Collision.ProjectileOrigin.Y)
                    : null)
            : NpcCollisionProfile.Empty;

        var attachments = entry.Attachments != null
            ? entry.Attachments.ConvertAll(a => new NpcAttachmentDescriptor(a.Slot ?? string.Empty, a.Atlas ?? string.Empty))
            : new List<NpcAttachmentDescriptor>();

        return new NpcAtlasDefinition(entry.Id, atlas, animations, collision, attachments);
    }

    private static string ResolveAtlasAnimationName(string alias, string requestedName, TextureAtlas atlas)
    {
        var initialCandidates = new[]
        {
            requestedName,
            alias
        };

        var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in initialCandidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            if (unique.Add(candidate) && atlas.HasAnimation(candidate))
            {
                return candidate;
            }
        }

        var extended = new List<string>();

        foreach (var candidate in initialCandidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            int underscoreIndex = candidate.IndexOf('_');
            if (underscoreIndex > 0 && underscoreIndex < candidate.Length - 1)
            {
                var withoutPrefix = candidate[(underscoreIndex + 1)..];
                if (!string.IsNullOrWhiteSpace(withoutPrefix))
                {
                    extended.Add(withoutPrefix);
                }
            }
        }

        foreach (var candidate in extended)
        {
            if (unique.Add(candidate) && atlas.HasAnimation(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private NpcAtlasManifest ReadManifest(string relativePath)
    {
        var normalizedPath = Path.Combine(_content.RootDirectory, relativePath).Replace('\\', '/');

        using var stream = TitleContainer.OpenStream(normalizedPath);
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return JsonSerializer.Deserialize<NpcAtlasManifest>(json, _options);
    }
}

internal sealed class NpcAtlasManifest
{
    public List<NpcManifestEntry> Npcs { get; set; }
}

internal sealed class NpcManifestEntry
{
    public string Id { get; set; }
    public string DisplayName { get; set; }
    public string TextureAtlas { get; set; }
    public Dictionary<string, NpcAnimationEntry> Animations { get; set; }
    public NpcCollisionEntry Collision { get; set; }
    public List<NpcAttachmentEntry> Attachments { get; set; }
}

internal sealed class NpcAnimationEntry
{
    public string AtlasAnimation { get; set; }
    public float? Fps { get; set; }
    public bool? Loop { get; set; }
}

internal sealed class NpcCollisionEntry
{
    public NpcRectangleEntry Hitbox { get; set; }
    public NpcVectorEntry ProjectileOrigin { get; set; }
}

internal sealed class NpcRectangleEntry
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

internal sealed class NpcVectorEntry
{
    public float X { get; set; }
    public float Y { get; set; }
}

internal sealed class NpcAttachmentEntry
{
    public string Slot { get; set; }
    public string Atlas { get; set; }
}
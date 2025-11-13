using Hearthvale.GameCode.Data.Atlases.Models;

namespace Hearthvale.GameCode.Data.Atlases;

public sealed class NullNpcAtlasCatalog : INpcAtlasCatalog
{
    public static NullNpcAtlasCatalog Instance { get; } = new();

    private NullNpcAtlasCatalog()
    {
    }

    public void LoadManifest(string relativePath)
    {
    }

    public bool TryGetDefinition(string npcId, out NpcAtlasDefinition definition)
    {
        definition = null;
        return false;
    }
}
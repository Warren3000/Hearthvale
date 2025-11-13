using Hearthvale.GameCode.Data.Atlases.Models;

namespace Hearthvale.GameCode.Data.Atlases;

public interface INpcAtlasCatalog
{
    void LoadManifest(string relativePath);
    bool TryGetDefinition(string npcId, out NpcAtlasDefinition definition);
}
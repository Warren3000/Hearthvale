using Microsoft.Xna.Framework;

public class DungeonPuzzle : IDungeonElement
{
    public string Id { get; }
    public bool IsActive { get; private set; }
    public PuzzleType Type { get; }
    public bool IsSolved { get; private set; }

    public void Activate() { /* Start puzzle */ }
    public void Deactivate() { /* End puzzle */ }
    public void Update(GameTime gameTime) { /* Puzzle logic */ }
}

public enum PuzzleType
{
    BlockPush,
    LightMirror,
    Sequence,
    Riddle
}
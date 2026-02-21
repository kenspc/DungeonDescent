namespace DungeonDescent;

class Tile
{
    public TileType Type { get; set; }
    public bool IsExplored { get; set; }
    public bool IsVisible   { get; set; }

    public bool IsWalkable => Type != TileType.Wall;

    public Tile(TileType type) => Type = type;
}

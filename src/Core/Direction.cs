namespace DungeonDescent;

static class Direction
{
    public static readonly Point Up    = new(0, -1);
    public static readonly Point Down  = new(0,  1);
    public static readonly Point Left  = new(-1, 0);
    public static readonly Point Right = new(1,  0);

    public static readonly Point[] Cardinals = [Up, Down, Left, Right];
}

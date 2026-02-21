namespace DungeonDescent;

record struct Point(int X, int Y)
{
    public Point Add(Point other) => new(X + other.X, Y + other.Y);
    public int ManhattanDistance(Point other) => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
    public bool IsAdjacentTo(Point other) => ManhattanDistance(other) == 1;
}

namespace DungeonDescent;

record Room(int X, int Y, int Width, int Height)
{
    public Point Center => new(X + Width / 2, Y + Height / 2);

    public bool Overlaps(Room other) =>
        X <= other.X + other.Width  && X + Width  >= other.X &&
        Y <= other.Y + other.Height && Y + Height >= other.Y;

    public bool Contains(Point p) =>
        p.X >= X && p.X < X + Width &&
        p.Y >= Y && p.Y < Y + Height;
}

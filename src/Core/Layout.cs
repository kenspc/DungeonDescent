namespace DungeonDescent;

// Single source of truth for the fixed-grid window layout. The four
// game surfaces (title / map / status / log) are stacked vertically
// in this exact order with no gaps; overlays cover the entire
// WindowWidth x WindowHeight area.
//
// Changing any of these constants must keep the invariant
//   TitleHeight + MapHeight + StatusHeight + LogHeight == WindowHeight
// because RootScreen positions surfaces by accumulating those heights.
static class Layout
{
    public const int WindowWidth  = 60;
    public const int WindowHeight = 26;

    public const int TitleHeight  = 1;
    public const int MapHeight    = 20;
    public const int StatusHeight = 2;
    public const int LogHeight    = 3;

    public const int TitleY  = 0;
    public const int MapY    = TitleY + TitleHeight;     // 1
    public const int StatusY = MapY + MapHeight;         // 21
    public const int LogY    = StatusY + StatusHeight;   // 23
}

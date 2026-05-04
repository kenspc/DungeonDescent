using SadRogue.Primitives;

namespace DungeonDescent;

// Foreground palette (IBM CGA 16-color). Background is always console
// default (black). Reset / Bold have no analogue under SadConsole and are
// intentionally absent.
static class Palette
{
    public static readonly Color White   = new(255, 255, 255);
    public static readonly Color Yellow  = new(255, 255, 85);
    public static readonly Color Green   = new(85, 255, 85);
    public static readonly Color Red     = new(255, 85, 85);
    public static readonly Color Cyan    = new(85, 255, 255);
    public static readonly Color Magenta = new(255, 85, 255);
    public static readonly Color Blue    = new(85, 85, 255);
    public static readonly Color Gray    = new(170, 170, 170);
    public static readonly Color DarkRed = new(170, 0, 0);
}

using SadConsole.Input;

namespace DungeonDescent;

// Shared helpers for overlays that close on "any key". Treating pure
// modifier keys (Shift / Ctrl / Alt / Windows) as "any key" means a
// user holding Shift to type '?' would dismiss the help screen on
// the very next frame. These helpers filter those out.
static class OverlayInput
{
    public static bool IsModifier(Keys key) => key switch
    {
        Keys.LeftShift    or Keys.RightShift    => true,
        Keys.LeftControl  or Keys.RightControl  => true,
        Keys.LeftAlt      or Keys.RightAlt      => true,
        Keys.LeftWindows  or Keys.RightWindows  => true,
        _ => false,
    };

    public static bool HasNonModifierKeyPress(Keyboard keyboard)
    {
        foreach (var key in keyboard.KeysPressed)
        {
            if (!IsModifier(key.Key)) return true;
        }
        return false;
    }
}

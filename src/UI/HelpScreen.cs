using SadConsole;
using SadConsole.Input;

namespace DungeonDescent;

// Help overlay. Any non-modifier key returns to RootScreen; the help
// screen never mutates game state and never burns a turn.
class HelpScreen : ScreenSurface
{
    private readonly RootScreen _root;
    // Skip the very first ProcessKeyboard tick so the same '?' keypress
    // that opened the overlay isn't immediately consumed to close it.
    private bool _swallowFirstFrame = true;

    public HelpScreen(RootScreen root) : base(Layout.WindowWidth, Layout.WindowHeight)
    {
        _root = root;

        IsFocused   = true;
        UseKeyboard = true;
        SadConsoleRenderer.DrawHelp(this);
    }

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        if (_swallowFirstFrame)
        {
            _swallowFirstFrame = false;
            return false;
        }

        if (OverlayInput.HasNonModifierKeyPress(keyboard))
        {
            _root.CloseOverlay();
            return true;
        }
        return false;
    }
}

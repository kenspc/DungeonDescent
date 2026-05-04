using SadConsole;
using SadConsole.Input;

namespace DungeonDescent;

// Help overlay. Any key returns to RootScreen; the help screen never
// mutates game state and never burns a turn.
class HelpScreen : ScreenSurface
{
    private readonly RootScreen _root;

    public HelpScreen(RootScreen root) : base(60, 26)
    {
        _root = root;

        IsFocused   = true;
        UseKeyboard = true;
        SadConsoleRenderer.DrawHelp(this);
    }

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        if (keyboard.KeysPressed.Count > 0)
        {
            _root.CloseOverlay();
            return true;
        }
        return false;
    }
}

using SadConsole;
using SadConsole.Input;

namespace DungeonDescent;

// Final screen shown when Game.Status == Won. Any non-modifier key
// terminates the process; there is no way back to gameplay. The first
// keyboard tick is intentionally swallowed so the keypress that
// triggered victory (or any held key) does not dismiss the screen
// before the player can read the score.
class VictoryScreen : ScreenSurface
{
    private bool _swallowFirstFrame = true;

    public VictoryScreen(Game game) : base(Layout.WindowWidth, Layout.WindowHeight)
    {
        IsFocused   = true;
        UseKeyboard = true;
        SadConsoleRenderer.DrawVictory(game, this);
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
            SadConsole.Game.Instance.MonoGameInstance.Exit();
            return true;
        }
        return false;
    }
}

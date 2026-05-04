using SadConsole;
using SadConsole.Input;

namespace DungeonDescent;

// Final screen shown when Game.Status == Dead. Any non-modifier key
// terminates the process; there is no way back to gameplay. The first
// keyboard tick is intentionally swallowed so the keypress that
// triggered death (or any held key) does not dismiss the screen
// before the player can read the score.
class GameOverScreen : ScreenSurface
{
    private bool _swallowFirstFrame = true;

    public GameOverScreen(Game game) : base(Layout.WindowWidth, Layout.WindowHeight)
    {
        IsFocused   = true;
        UseKeyboard = true;
        SadConsoleRenderer.DrawGameOver(game, this);
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

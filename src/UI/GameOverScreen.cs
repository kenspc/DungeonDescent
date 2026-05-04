using SadConsole;
using SadConsole.Input;

namespace DungeonDescent;

// Final screen shown when Game.Status == Dead. Any key terminates the
// process; there is no way back to gameplay.
class GameOverScreen : ScreenSurface
{
    public GameOverScreen(Game game) : base(60, 26)
    {
        IsFocused   = true;
        UseKeyboard = true;
        SadConsoleRenderer.DrawGameOver(game, this);
    }

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        if (keyboard.KeysPressed.Count > 0)
        {
            SadConsole.Game.Instance.MonoGameInstance.Exit();
            return true;
        }
        return false;
    }
}

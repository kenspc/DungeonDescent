using SadConsole;
using SadConsole.Input;

namespace DungeonDescent;

// Final screen shown when Game.Status == Won. Any key terminates the
// process; there is no way back to gameplay.
class VictoryScreen : ScreenSurface
{
    public VictoryScreen(Game game) : base(60, 26)
    {
        IsFocused   = true;
        UseKeyboard = true;
        SadConsoleRenderer.DrawVictory(game, this);
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

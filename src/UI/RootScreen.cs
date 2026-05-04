using SadConsole;
using SadConsole.Input;

namespace DungeonDescent;

// Top-level ScreenObject for the running game. Hosts four child surfaces
// (title, map, status, log) at fixed positions inside a 60x26 window, and
// drives keyboard input through SadConsoleKeyAdapter into Game.HandleKey.
class RootScreen : ScreenObject
{
    private readonly Game _game;
    private readonly ScreenSurface _titleSurface;
    private readonly ScreenSurface _mapSurface;
    private readonly ScreenSurface _statusSurface;
    private readonly ScreenSurface _logSurface;

    public RootScreen(Game game)
    {
        _game = game;

        _titleSurface  = new ScreenSurface(60, 1)  { Position = (0, 0) };
        _mapSurface    = new ScreenSurface(60, 20) { Position = (0, 1) };
        _statusSurface = new ScreenSurface(60, 2)  { Position = (0, 21) };
        _logSurface    = new ScreenSurface(60, 3)  { Position = (0, 23) };

        Children.Add(_titleSurface);
        Children.Add(_mapSurface);
        Children.Add(_statusSurface);
        Children.Add(_logSurface);

        // RootScreen owns the keyboard, not the children.
        IsFocused = true;
        UseKeyboard = true;

        Refresh();
    }

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        foreach (var key in keyboard.KeysPressed)
        {
            // Hard-wired quit so we still exit even if HandleKey ignores 'q'.
            if (key.Key == Keys.Q)
            {
                SadConsole.Game.Instance.MonoGameInstance.Exit();
                return true;
            }

            var info = SadConsoleKeyAdapter.ToConsoleKeyInfo(key, keyboard);
            if (info != null)
            {
                _game.HandleKey(info.Value);
                Refresh();
                return true;
            }
        }
        return false;
    }

    private void Refresh()
    {
        SadConsoleRenderer.RenderTitle(_game, _titleSurface);
        SadConsoleRenderer.RenderMap(_game, _mapSurface);
        SadConsoleRenderer.RenderStatus(_game, _statusSurface);
        SadConsoleRenderer.RenderLog(_game, _logSurface);
    }
}

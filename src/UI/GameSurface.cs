using SadConsole;
using SadConsole.Input;

namespace DungeonDescent;

// Event-driven SadConsole console that owns the Game instance, dispatches
// keyboard input through SadConsoleKeyAdapter, and re-renders the whole
// surface after every accepted action. Pre-M4: it occupies the full 60x26
// game window; in M4 (Task 8) the rendering responsibility is split out
// across multiple sub-surfaces under RootScreen.
class GameSurface : SadConsole.Console
{
    private readonly Game _game;

    public GameSurface(Game game) : base(60, 26)
    {
        _game = game;
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

    private void Refresh() => SadConsoleRenderer.RenderAll(_game, this);
}

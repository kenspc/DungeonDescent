using SadConsole;
using SadConsole.Input;

namespace DungeonDescent;

// Top-level ScreenObject for the running game. Hosts four child surfaces
// (title, map, status, log) at fixed positions inside a 60x26 window, and
// drives keyboard input through SadConsoleKeyAdapter into Game.HandleKey.
// Overlays (inventory, help, end screens) replace the four surfaces
// temporarily via OpenOverlay / CloseOverlay.
class RootScreen : ScreenObject
{
    private readonly Game _game;
    private readonly ScreenSurface _titleSurface;
    private readonly ScreenSurface _mapSurface;
    private readonly ScreenSurface _statusSurface;
    private readonly ScreenSurface _logSurface;
    private readonly List<ScreenSurface> _gameSurfaces;

    private ScreenObject? _currentOverlay;

    public RootScreen(Game game)
    {
        _game = game;

        _titleSurface  = new ScreenSurface(60, 1)  { Position = (0, 0) };
        _mapSurface    = new ScreenSurface(60, 20) { Position = (0, 1) };
        _statusSurface = new ScreenSurface(60, 2)  { Position = (0, 21) };
        _logSurface    = new ScreenSurface(60, 3)  { Position = (0, 23) };

        _gameSurfaces = new List<ScreenSurface>
        {
            _titleSurface, _mapSurface, _statusSurface, _logSurface,
        };

        foreach (var s in _gameSurfaces)
            Children.Add(s);

        // RootScreen owns the keyboard; surface children are draw-only.
        IsFocused   = true;
        UseKeyboard = true;

        Refresh();
    }

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        // While an overlay is up, the overlay handles its own input.
        if (_currentOverlay != null) return false;

        foreach (var key in keyboard.KeysPressed)
        {
            // Hard-wired quit so we still exit even if HandleKey ignores 'q'.
            if (key.Key == Keys.Q)
            {
                SadConsole.Game.Instance.MonoGameInstance.Exit();
                return true;
            }

            if (key.Key == Keys.I)
            {
                OpenOverlay(new InventoryScreen(_game, this));
                return true;
            }

            if (key.Key == Keys.OemQuestion &&
                (keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift)))
            {
                OpenOverlay(new HelpScreen(this));
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

    public void OpenOverlay(ScreenObject overlay)
    {
        foreach (var s in _gameSurfaces)
            s.IsVisible = false;

        Children.Add(overlay);
        _currentOverlay = overlay;
        overlay.IsFocused = true;
    }

    public void CloseOverlay()
    {
        if (_currentOverlay == null) return;

        Children.Remove(_currentOverlay);
        _currentOverlay = null;

        foreach (var s in _gameSurfaces)
            s.IsVisible = true;

        IsFocused = true;
    }

    public void Refresh()
    {
        SadConsoleRenderer.RenderTitle(_game, _titleSurface);
        SadConsoleRenderer.RenderMap(_game, _mapSurface);
        SadConsoleRenderer.RenderStatus(_game, _statusSurface);
        SadConsoleRenderer.RenderLog(_game, _logSurface);
    }

    public override void Update(TimeSpan delta)
    {
        // Auto-promote to GameOver / Victory overlay when the game state
        // transitions out of Playing. Status is one-way (Dead/Won never
        // revert), so once an end-screen is shown it stays for the rest
        // of the process lifetime.
        if (_game.Status == GameStatus.Dead && _currentOverlay is not GameOverScreen)
        {
            ReplaceOverlay(new GameOverScreen(_game));
        }
        else if (_game.Status == GameStatus.Won && _currentOverlay is not VictoryScreen)
        {
            ReplaceOverlay(new VictoryScreen(_game));
        }

        base.Update(delta);
    }

    private void ReplaceOverlay(ScreenObject overlay)
    {
        if (_currentOverlay != null)
        {
            Children.Remove(_currentOverlay);
            _currentOverlay = null;
        }

        OpenOverlay(overlay);
    }
}

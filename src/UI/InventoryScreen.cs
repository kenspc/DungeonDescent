using SadConsole;
using SadConsole.Input;

namespace DungeonDescent;

// Inventory overlay. Replaces the four game surfaces while open. Routes
// digits 1-9 into Game.UseInventoryItem, Esc back to RootScreen.
class InventoryScreen : ScreenSurface
{
    private readonly Game _game;
    private readonly RootScreen _root;

    public InventoryScreen(Game game, RootScreen root) : base(60, 26)
    {
        _game = game;
        _root = root;

        IsFocused   = true;
        UseKeyboard = true;
        Refresh();
    }

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        foreach (var key in keyboard.KeysPressed)
        {
            if (key.Key == Keys.Escape)
            {
                _root.CloseOverlay();
                return true;
            }

            if (key.Key >= Keys.D1 && key.Key <= Keys.D9 &&
                _game.Player.Inventory.Count > 0)
            {
                int index = (int)key.Key - (int)Keys.D1;
                if (index < _game.Player.Inventory.Count)
                {
                    string msg = _game.UseInventoryItem(index);
                    _game.Log.Add(msg);
                    _game.EndPlayerTurn(); // using an item costs a turn
                    _root.CloseOverlay();
                    _root.Refresh();
                    return true;
                }
            }
        }
        return false;
    }

    private void Refresh() => SadConsoleRenderer.DrawInventory(_game, this);
}

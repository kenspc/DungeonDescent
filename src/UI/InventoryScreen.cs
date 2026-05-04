using SadConsole;
using SadConsole.Input;

namespace DungeonDescent;

// Inventory overlay. Replaces the four game surfaces while open. Routes
// digits 1-9 into Game.UseInventoryItem, Esc back to RootScreen.
class InventoryScreen : ScreenSurface
{
    private readonly Game _game;
    private readonly RootScreen _root;

    public InventoryScreen(Game game, RootScreen root) : base(Layout.WindowWidth, Layout.WindowHeight)
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

            int? slot = TryGetSlotIndex(key.Key);
            if (slot != null && _game.Player.Inventory.Count > 0 &&
                slot.Value < _game.Player.Inventory.Count)
            {
                string msg = _game.UseInventoryItem(slot.Value);
                _game.Log.Add(msg);
                _game.EndPlayerTurn(); // using an item costs a turn
                _root.CloseOverlay();
                _root.Refresh();
                return true;
            }
        }
        return false;
    }

    // Map both the number row (D1..D9) and the numpad (NumPad1..NumPad9)
    // to a 0-based inventory slot index.
    private static int? TryGetSlotIndex(Keys key)
    {
        if (key >= Keys.D1 && key <= Keys.D9)
            return (int)key - (int)Keys.D1;
        if (key >= Keys.NumPad1 && key <= Keys.NumPad9)
            return (int)key - (int)Keys.NumPad1;
        return null;
    }

    private void Refresh() => SadConsoleRenderer.DrawInventory(_game, this);
}

using SadConsole.Input;

namespace DungeonDescent;

// Bridges SadConsole's AsciiKey + Keyboard into the System.ConsoleKeyInfo
// that Game.HandleKey was originally written against. Game.HandleKey reads
// both ConsoleKeyInfo.Key (for arrows / WASD) and ConsoleKeyInfo.KeyChar
// (for `>`, `<`, `.`), so every returned ConsoleKeyInfo must populate both.
static class SadConsoleKeyAdapter
{
    public static ConsoleKeyInfo? ToConsoleKeyInfo(AsciiKey key, Keyboard keyboard)
    {
        bool shift = keyboard.IsKeyDown(Keys.LeftShift) ||
                     keyboard.IsKeyDown(Keys.RightShift);

        // Movement / action letters — case-insensitive; do not require shift.
        switch (key.Key)
        {
            case Keys.W: return new ConsoleKeyInfo('w', ConsoleKey.W, false, false, false);
            case Keys.A: return new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false);
            case Keys.S: return new ConsoleKeyInfo('s', ConsoleKey.S, false, false, false);
            case Keys.D: return new ConsoleKeyInfo('d', ConsoleKey.D, false, false, false);
            case Keys.Q: return new ConsoleKeyInfo('q', ConsoleKey.Q, false, false, false);
            case Keys.I: return new ConsoleKeyInfo('i', ConsoleKey.I, false, false, false);

            case Keys.Up:    return new ConsoleKeyInfo('\0', ConsoleKey.UpArrow,    false, false, false);
            case Keys.Down:  return new ConsoleKeyInfo('\0', ConsoleKey.DownArrow,  false, false, false);
            case Keys.Left:  return new ConsoleKeyInfo('\0', ConsoleKey.LeftArrow,  false, false, false);
            case Keys.Right: return new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false);

            case Keys.Escape: return new ConsoleKeyInfo('\0', ConsoleKey.Escape, false, false, false);
        }

        // Number row 1-9 (D1..D9). Game.UseInventoryItem reads KeyChar.
        if (key.Key >= Keys.D1 && key.Key <= Keys.D9 && !shift)
        {
            int offset = (int)key.Key - (int)Keys.D1;
            char ch = (char)('1' + offset);
            ConsoleKey ck = (ConsoleKey)((int)ConsoleKey.D1 + offset);
            return new ConsoleKeyInfo(ch, ck, false, false, false);
        }

        // Punctuation that Game.HandleKey switches on.
        switch (key.Key)
        {
            case Keys.OemPeriod:
                return shift
                    ? new ConsoleKeyInfo('>', ConsoleKey.OemPeriod, true,  false, false)
                    : new ConsoleKeyInfo('.', ConsoleKey.OemPeriod, false, false, false);
            case Keys.OemComma:
                if (shift)
                    return new ConsoleKeyInfo('<', ConsoleKey.OemComma, true, false, false);
                break;
            case Keys.OemQuestion:
                if (shift)
                    return new ConsoleKeyInfo('?', ConsoleKey.Oem2, true, false, false);
                break;
        }

        return null;
    }
}

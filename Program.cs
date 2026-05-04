using SadConsole;
using SadConsole.Quick;
using SadRogue.Primitives;
using SadGame = SadConsole.Game;
using SadConsoleConsole = SadConsole.Console;

Settings.WindowTitle = "Dungeon Descent";

SadGame.Create(80, 30, (_, _) =>
{
    var screen = new SadConsoleConsole(80, 30);
    screen.Print(38, 14, "@", Color.Yellow);
    screen.Print(30, 16, "Press any key to exit", Color.Gray);
    screen.IsFocused = true;
    screen.WithKeyboard((_, _) =>
    {
        SadGame.Instance.MonoGameInstance.Exit();
        return true;
    });
    SadGame.Instance.Screen = screen;
    SadGame.Instance.DestroyDefaultStartingConsole();
});
SadGame.Instance.Run();
SadGame.Instance.Dispose();

using SadConsole;
using SadConsole.Quick;
using SadGame = SadConsole.Game;
using SadConsoleConsole = SadConsole.Console;

Settings.WindowTitle = "Dungeon Descent";

SadGame.Create(80, 30, (_, _) =>
{
    var screen = new SadConsoleConsole(80, 30);
    var game = new DungeonDescent.Game();
    DungeonDescent.SadConsoleRenderer.RenderAll(game, screen);

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

using SadConsole;
using SadGame = SadConsole.Game;

Settings.WindowTitle = "Dungeon Descent";

SadGame.Create(60, 26, (_, _) =>
{
    var game = new DungeonDescent.Game();
    var surface = new DungeonDescent.GameSurface(game);
    surface.IsFocused = true;
    SadGame.Instance.Screen = surface;
    SadGame.Instance.DestroyDefaultStartingConsole();
});
SadGame.Instance.Run();
SadGame.Instance.Dispose();

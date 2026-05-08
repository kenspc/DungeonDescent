using SadConsole;
using SadConsole.Configuration;
using SadGame = SadConsole.Game;

Settings.WindowTitle = "Dungeon Descent";

// Path B2 (per docs/plans/pixel-font-pass.md M2 step 7): load a 16x16
// pixel font and render it at IFont.Sizes.Two for 32x32 cells, giving a
// 60x26 grid in a 1920x832 physical window. The font path resolves
// against AppContext.BaseDirectory so it works under both
// `dotnet run` (working dir = project root) and a published build.
var fontPath = Path.Combine(AppContext.BaseDirectory,
    "assets/fonts/unifont/unifont.font");

var startup = new Builder()
    .SetWindowSizeInCells(DungeonDescent.Layout.WindowWidth,
                          DungeonDescent.Layout.WindowHeight)
    .ConfigureFonts((cfg, _) => cfg.UseCustomFont(fontPath))
    .SetDefaultFontSize(IFont.Sizes.Two)
    .OnStart((_, _) =>
    {
        var game = new DungeonDescent.Game();
        var root = new DungeonDescent.RootScreen(game);
        SadGame.Instance.Screen = root;
        SadGame.Instance.DestroyDefaultStartingConsole();
    });

// try/finally guarantees Dispose runs even if Run throws, so SDL /
// MonoGame native handles release before the process exits.
try
{
    SadGame.Create(startup);
    SadGame.Instance.Run();
}
finally
{
    SadGame.Instance.Dispose();
}

using SadConsole;
using SadConsole.Configuration;
using SadGame = SadConsole.Game;

// CLI:
//   dotnet run                                    (uses default unifont placeholder)
//   dotnet run -- --font <path>                   (overrides font path; useful
//                                                  for cycling through candidates
//                                                  in M3 audit without editing
//                                                  this file)
//   dotnet run -- --probe-seed <int>              (prints Rooms.Count for
//                                                  a single Map(seed) call and
//                                                  exits — for verifying that
//                                                  a seed is "known-good"
//                                                  before pinning Game.cs:19)
string defaultFont = "assets/fonts/px437-fmtowns-re/px437-fmtowns-re.font";
string fontPath = defaultFont;
int? probeSeed = null;

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--font" && i + 1 < args.Length)
    {
        fontPath = args[i + 1];
        i++;
    }
    else if (args[i] == "--probe-seed" && i + 1 < args.Length
             && int.TryParse(args[i + 1], out int seed))
    {
        probeSeed = seed;
        i++;
    }
}

if (probeSeed.HasValue)
{
    var probeMap = new DungeonDescent.Map(probeSeed.Value);
    System.Console.WriteLine($"seed={probeSeed.Value}, Rooms.Count={probeMap.Rooms.Count}");
    return;
}

Settings.WindowTitle = "Dungeon Descent";

var resolvedFontPath = Path.Combine(AppContext.BaseDirectory, fontPath);

var startup = new Builder()
    .SetWindowSizeInCells(DungeonDescent.Layout.WindowWidth,
                          DungeonDescent.Layout.WindowHeight)
    .ConfigureFonts((cfg, _) => cfg.UseCustomFont(resolvedFontPath))
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

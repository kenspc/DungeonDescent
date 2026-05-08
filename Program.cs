using SadConsole;
using SadConsole.Configuration;
using SadGame = SadConsole.Game;
using Console = System.Console;

// CLI:
//   dotnet run                                    (uses the selected
//                                                  px437-fmtowns-re pixel font)
//   dotnet run -- --font <path>                   (overrides font path; relative
//                                                  paths are resolved against
//                                                  AppContext.BaseDirectory,
//                                                  absolute paths are used as-is)
//   dotnet run -- --probe-seed <int>              (prints Rooms.Count for
//                                                  a single Map(seed) call and
//                                                  exits — audit utility used
//                                                  to verify a seed is
//                                                  "known-good" during seed-pin
//                                                  workflows)
//   dotnet run -- --help | -h                     (prints this usage)
const string defaultFontPath = "assets/fonts/px437-fmtowns-re/px437-fmtowns-re.font";
string fontPath = defaultFontPath;
int? probeSeed = null;

int idx = 0;
while (idx < args.Length)
{
    string flag = args[idx];
    switch (flag)
    {
        case "--help":
        case "-h":
            PrintUsage();
            return;
        case "--font":
            if (idx + 1 >= args.Length)
            {
                Console.Error.WriteLine("error: --font requires a path argument");
                PrintUsage();
                Environment.Exit(2);
            }
            fontPath = args[idx + 1];
            idx += 2;
            break;
        case "--probe-seed":
            if (idx + 1 >= args.Length)
            {
                Console.Error.WriteLine("error: --probe-seed requires an integer argument");
                PrintUsage();
                Environment.Exit(2);
            }
            if (!int.TryParse(args[idx + 1], out int seed))
            {
                Console.Error.WriteLine(
                    $"error: --probe-seed expects an integer, got '{args[idx + 1]}'");
                Environment.Exit(2);
            }
            probeSeed = seed;
            idx += 2;
            break;
        default:
            Console.Error.WriteLine($"error: unknown argument '{flag}'");
            PrintUsage();
            Environment.Exit(2);
            break;
    }
}

if (probeSeed.HasValue)
{
    try
    {
        var probeMap = new DungeonDescent.Map(probeSeed.Value);
        Console.WriteLine($"seed={probeSeed.Value}, Rooms.Count={probeMap.Rooms.Count}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(
            $"error: Map construction failed for seed={probeSeed.Value}: {ex.Message}");
        Environment.Exit(1);
    }
    return;
}

Settings.WindowTitle = "Dungeon Descent";

// Absolute paths are used verbatim so users can point --font at any location;
// relative paths are resolved against the binary's base directory so the
// default works regardless of the current working directory.
string resolvedFontPath = Path.IsPathRooted(fontPath)
    ? fontPath
    : Path.Combine(AppContext.BaseDirectory, fontPath);

if (!File.Exists(resolvedFontPath))
{
    Console.Error.WriteLine($"error: font file not found at '{resolvedFontPath}'");
    Console.Error.WriteLine(
        "hint: pass --font <path> to override; relative paths are resolved against " +
        $"'{AppContext.BaseDirectory}'.");
    Environment.Exit(1);
}

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

static void PrintUsage()
{
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  dotnet run");
    Console.Error.WriteLine("  dotnet run -- --font <path>");
    Console.Error.WriteLine("  dotnet run -- --probe-seed <int>");
    Console.Error.WriteLine("  dotnet run -- --help | -h");
}

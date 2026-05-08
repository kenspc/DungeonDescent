# SadConsole 10.9 Font API Verification (Task 2)

Verification performed 2026-05-08 against the locally installed SadConsole 10.9.0 NuGet package XML doc:
`/home/kenspc/.nuget/packages/sadconsole/10.9.0/lib/net8.0/SadConsole.xml`.

Context7 (`/thraka/sadconsole`) was consulted but the doc set is sparse (3 snippets total, all variations of the basic `Builder` example). The XML doc was the authoritative source.

---

## Assumption A — `Game.Create(int, int, string fontPath, EventHandler<GameHost>)` overload exists

**Verdict: REFUTED.** No such overload exists in 10.9. The plan's Technical Approach assumed an API that does not match the v10 surface.

**Evidence:**
- No `M:SadConsole.Game.Create` entries in `SadConsole.xml` taking a fontPath string.
- Only the legacy `Game.Create(int width, int height, ...)` form (no font argument) and the v10 `Game.Create(Builder)` form exist.
- The Context7 README example uses the Builder form throughout.

**Actual API surface for custom-font initialization (Builder pattern):**

```csharp
// SadConsole 10.x idiom — taken from /thraka/sadconsole README.
using SadConsole.Configuration;

Settings.WindowTitle = "Dungeon Descent";

Builder startup = new Builder()
    .SetScreenSize(Layout.WindowWidth, Layout.WindowHeight)        // 60 x 26 cells
    .ConfigureFonts((fontConfig, gameHost) =>
    {
        fontConfig.UseCustomFont("assets/fonts/<name>/<name>.font");
    })
    .SetDefaultFontSize(IFont.Sizes.Two)                            // 2x upscale → 32x32 cell
    .OnStart((sender, host) =>
    {
        var game = new DungeonDescent.Game();
        var root = new DungeonDescent.RootScreen(game);
        SadConsole.Game.Instance.Screen = root;
        SadConsole.Game.Instance.DestroyDefaultStartingConsole();
    });

SadConsole.Game.Create(startup);
SadConsole.Game.Instance.Run();
SadConsole.Game.Instance.Dispose();
```

**Available `ConfigureFonts` overloads** (XML doc lines 2656-2680):
- `ConfigureFonts(Builder, Action<FontConfig, GameHost>)` — full control via `FontConfig` (recommended, used above).
- `ConfigureFonts(Builder, string customDefaultFont, string[] extraFonts)` — single-font shortcut.
- `ConfigureFonts(Builder, bool useExtendedDefault)` — built-in fonts only (default if not called).

**FontConfig methods** (XML lines 2898-2920):
- `UseBuiltinFont()`, `UseBuiltinFontExtended()` — opt into bundled IBM fonts.
- `UseCustomFont(string path)` — load custom `.font` file as the default.
- `AddExtraFonts(string[] paths)` — additional fonts available via `Game.Instance.Fonts[name]`.
- `SetDefaultFontSize(IFont.Sizes size)` — same effect as the Builder-level `.SetDefaultFontSize(...)`.

**Plan impact:** Technical Approach code block + M2 step 7 must be rewritten to use the Builder pattern. The current `Program.cs` (using `SadGame.Create(int, int, Action)`) keeps working **only for the placeholder-font-not-yet-loaded path** — to load a custom font, the Builder rewrite is mandatory.

---

## Assumption B — `ScreenSurface.FontSize` is a `Point`-typed settable property

**Verdict: CONFIRMED.**

**Evidence:**
- `P:SadConsole.IScreenSurface.FontSize` declared at XML line 8510, typed via interface contract that the implementing types satisfy with `Point` (per `Nullable<Point>` usage in `ScreenSurface(ICellSurface, IFont, Nullable<Point>)` ctor at line 10462).
- `P:SadConsole.ScreenSurface.FontSize` at line 10377 uses `<inheritdoc/>` so it's the same type and contract.
- Setting fires the change-notify hook (`OnFontChanged`-style, line 10510).

**Plan impact:** the per-surface `s.FontSize = new Point(32, 32)` plan assumption is technically correct, but **becomes redundant** once we use `.SetDefaultFontSize(IFont.Sizes.Two)` in the Builder — see Assumption C below. Recommended path: drop per-surface `FontSize` writes, use the Builder-level default.

---

## Assumption C — Child `ScreenSurface` instances inherit the default font automatically

**Verdict: CONFIRMED, with the important nuance that inheritance happens at *construction* time, not dynamically.**

**Evidence:**
- `ScreenSurface.#ctor(int width, int height)` (XML line 10420) takes no font/size — surface implicitly uses `GameHost.DefaultFont` and `DefaultFontSize` at construction.
- The Builder's `OnStart` callback runs after `ConfigureFonts(...)` has set the defaults, so any surface constructed in that callback (like our `RootScreen` in `Program.cs`) inherits the custom font automatically.
- Existing surfaces created BEFORE `DefaultFont` changes do NOT retroactively pick up the new font (this is normal object instantiation behavior).

**`IFont.Sizes` enum** (XML lines 6513-6543):
- `Quarter` = 0.25× source, `Half` = 0.5×, `One` = 1.0× (default), `Two` = 2.0×, `Three` = 3.0×, `Four` = 4.0×.
- `Two` is exactly the brief-mandated B2 path: 16×16 source → 32×32 rendered.

**Plan impact:** **Task 5 (Conditional overlay FontSize) becomes unnecessary**. With `.SetDefaultFontSize(IFont.Sizes.Two)` set in the Builder before `RootScreen` is constructed in `OnStart`, all 4 game surfaces AND all 4 overlay surfaces inherit both font and size at construction. No per-surface or per-overlay `FontSize` writes are needed in `RootScreen.cs` or any of the overlay classes. This eliminates the conditional Task 5 entirely.

---

## Assumption D — `.font` JSON schema field names

**Verdict: PARTIALLY REFUTED.** Most fields match the plan's pre-verification template, but **one field name was wrong: `IsSadFontFormat` should be `IsSadExtended`**.

**Evidence:** confirmed via `P:SadConsole.SadFont.*` entries in XML (lines 9775-9836):

| Field name              | XML line | Type      | Notes                                            |
|-------------------------|----------|-----------|--------------------------------------------------|
| `Name`                  | 9794     | string    | font display name                                |
| `FilePath`              | 9797     | string    | PNG path relative to .font file                  |
| `GlyphHeight`           | 9802     | int       | glyph cell height in source pixels               |
| `GlyphWidth`            | 9805     | int       | glyph cell width in source pixels                |
| `GlyphPadding`          | 9808     | int       | inter-cell padding in source pixels              |
| `Columns`               | 9783     | int       | grid columns (16 for CP437)                      |
| `Rows`                  | 9786     | int       | grid rows (16 for CP437)                         |
| `SolidGlyphIndex`       | 9775     | int       | typically 219 (block)                            |
| `UnsupportedGlyphIndex` | 9813     | int       | typically 0 or 219                               |
| `IsSadExtended`         | 9826     | bool      | **NOT `IsSadFontFormat`** (plan template error)  |

**Plan impact:** the `.font` JSON template in plan M2 step 4a needs `IsSadFontFormat` → `IsSadExtended`. All other fields stay.

---

## Plan changes required (downstream of these findings)

1. **`docs/plans/pixel-font-pass.md`** — Technical Approach code block: rewrite `Program.cs` snippet to use `Builder` pattern with `.ConfigureFonts((cfg, _) => cfg.UseCustomFont(path))` + `.SetDefaultFontSize(IFont.Sizes.Two)`. Remove the per-surface `FontSize` snippet.
2. **`docs/plans/pixel-font-pass.md`** — M2 step 7: replace "use `Game.Create` fontPath overload" with "rewrite Program.cs to Builder pattern".
3. **`docs/plans/pixel-font-pass.md`** — M2 step 8: remove the per-surface `FontSize` setter requirement (now handled by `.SetDefaultFontSize` globally).
4. **`docs/plans/pixel-font-pass.md`** — M2 step 9 (overlay conditional): obsolete — overlays inherit via construction. Remove or reword to "no action needed".
5. **`docs/plans/pixel-font-pass.md`** — M2 step 4a `.font` template: rename `IsSadFontFormat` → `IsSadExtended`.
6. **`docs/tasks/pixel-font-pass-tasks.md`** — Task 4: drop per-surface FontSize requirement, keep Builder rewrite as the main work.
7. **`docs/tasks/pixel-font-pass-tasks.md`** — Task 5: mark **OBSOLETE** (Builder + SetDefaultFontSize handles overlays via inheritance). Convert to `Status: SKIPPED` with rationale, or remove entirely. Task 5 → SKIPPED is the cleaner option since other tasks may reference Task 5 numbering.

The Builder rewrite is more invasive than the plan's "fontPath overload" assumption suggested, but in exchange:
- The 4 per-surface `FontSize` writes go away.
- The conditional Task 5 (4 overlay edits) goes away entirely.
- The whole font + size configuration lives in one place (Program.cs Builder), making future font swaps trivial.

Net code-touch reduction: ~10 lines avoided across 5 files.

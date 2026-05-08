# Pixel Font Pass — Task Document

## Context

Decompose visual-polish step 2 of 3 (custom 16×16 Brogue-adjacent pixel font, rendered at 32×32 cell via SadConsole 10.9 path B2). Drives the 60×26 cell grid into a 1920×832 physical window with palette-brogue-pass already landed.

Related plan: `docs/plans/pixel-font-pass.md`  
Related upstream brief: `docs/briefs/pixel-font-pass.md`

**Cross-phase dependency note**: Task 1 and Task 2 are grounded in the existing codebase (palette-brogue-pass landed at commit e0119f5). Tasks 3-9 depend on outputs from prior tasks rather than direct codebase reads — see each task's `Depends on` line. Task 5 is **conditional** on Task 2's finding about font inheritance and may be skipped if SadConsole sub-surfaces inherit the default font automatically.

**F1 narrative discipline (from upstream brief)**: every commit message must call this out as "step 2 of 3" and explicitly say "does not complete visual polish". Animation remains step 3 and is out of scope here.

## Tasks

### Task 1: Capture pre-change baseline screenshot (M1)

**Status:** TODO

In the current `git HEAD` state (palette-brogue-pass landed, default 8×16 font), launch the game, play floor 1 until a single screenshot can capture all 8 audit-relevant element types simultaneously, save the screenshot, and commit it.

**Files to create:**
- `docs/screenshots/font-pass/before.png`

**Acceptance criteria:**
- `before.png` exists at the path above and is a PNG (not JPEG — lossy compression breaks pixel audit)
- The screenshot contains all 8 element types simultaneously visible: player `@`, at least one monster, at least one item, at least one `FloorMossy` `,`, at least one `FloorCracked` `'`, the `StairsDown` `>`, the status row text, and the log row text
- Image dimensions are approximately 480×416 (consistent with default 8×16 font × 60×26 grid)
- File is committed to git (audit material)

---

### Task 2: Verify SadConsole 10.9 font API via Context7

**Status:** TODO

**Depends on:** Task 1

Use the Context7 MCP server (`/thraka/sadconsole` or the equivalent SadConsole 10.x library id) to confirm three plan assumptions before any code change. Write findings to a notes file. If any assumption is refuted, update the plan's Technical Approach section before proceeding to Task 3.

**Files to create:**
- `docs/screenshots/font-pass/sadconsole-api-notes.md`

**Acceptance criteria:**
- Notes file explicitly records, for each of the three assumptions, whether it is confirmed or refuted with a short citation:
  - Assumption A: `SadConsole.Game.Create(int width, int height, string fontPath, EventHandler<GameHost> startingObject)` (or equivalent fontPath-accepting overload) exists in 10.9
  - Assumption B: `ScreenSurface.FontSize` is a `Point`-typed settable property
  - Assumption C: when the default font is replaced before child surfaces are constructed, child `ScreenSurface` instances inherit the new default font automatically (or do not — note which)
- Notes file also records the canonical `.font` JSON schema field names for SadConsole 10.9 (Name, FilePath, GlyphHeight, GlyphWidth, GlyphPadding, Columns, SolidGlyphIndex, UnsupportedGlyphIndex, IsSadFontFormat or whatever the actual names are)
- If any assumption is refuted, the plan's Technical Approach section is updated by editing `docs/plans/pixel-font-pass.md` before this task is marked done

---

### Task 3: Build Unifont placeholder font asset (M2 steps 2-5)

**Status:** TODO

**Depends on:** Task 2

Source the GNU Unifont 16×16 glyph data, build a 256×256 px PNG glyph table (16 columns × 16 rows = exactly 256 glyphs covering Basic Latin / Latin-1 / CP437 subset), author the matching `.font` JSON descriptor using the schema confirmed in Task 2, copy in the GNU Unifont OFL/GPL license text verbatim, and create the assets-fonts top-level README. If the PNG cannot be assembled within a 1-hour timebox, fall back per plan M2 step 3 to extracting SadConsole's bundled IBM 8×16 font instead — keeping placeholder semantics intact (M2's purpose is pipeline validation, not visual quality).

**Files to create:**
- `assets/fonts/unifont/unifont.font`
- `assets/fonts/unifont/unifont.png`
- `assets/fonts/unifont/LICENSE.txt`
- `assets/fonts/README.md`

**Acceptance criteria:**
- `unifont.png` is 256×256 px (or matches the cell-size × 16×16-grid product if Unifont is replaced by IBM 8×16 fallback, in which case 128×256), uses 1-bit / no-AA pixel rendering
- `unifont.font` JSON validates against the schema captured in Task 2's notes file (field names match Task 2's findings, not the plan's pre-verification template)
- `LICENSE.txt` contains the verbatim GNU Unifont license text (OFL or GPL — copy from the upstream source page)
- `assets/fonts/README.md` exists and contains at minimum one row in the format `| font name | source URL | license | designer |` for the placeholder font
- If fallback to IBM 8×16 is used, `assets/fonts/README.md` notes the fallback explicitly with the SadConsole upstream URL

---

### Task 4: Wire font + 32×32 cell rendering on 4 game surfaces (M2 steps 6-8, 10)

**Status:** TODO

**Depends on:** Task 3

Add the assets/fonts content include to the csproj, switch `Program.cs` to use the `Game.Create` fontPath overload (loading the placeholder font), and modify `RootScreen.cs` so each of the 4 game surfaces (`_titleSurface`, `_mapSurface`, `_statusSurface`, `_logSurface`) renders at 32×32 cell. Run the game to confirm end-to-end correctness — 4 surfaces render, status text fits, game plays through floor 2 without crashing.

**Files to create or modify:**
- `DungeonDescent.csproj` (add `<Content Include="assets/fonts/**/*.font;assets/fonts/**/*.png" CopyToOutputDirectory="PreserveNewest" />` ItemGroup)
- `Program.cs` (replace the existing `SadGame.Create(width, height, lambda)` call with the fontPath-accepting overload, pointing at `assets/fonts/unifont/unifont.font`)
- `src/UI/RootScreen.cs` (set `FontSize = new Point(32, 32)` on each of the 4 game surfaces after construction; the `_gameSurfaces` foreach pattern already in place is the natural insertion point)

**Acceptance criteria:**
- `dotnet build` succeeds without warnings introduced by the font work
- Game launches, opens a window with measured dimensions of 1920×832 px (use Snipping Tool / WSLg native screenshot / `gnome-screenshot -w` per platform per plan M2 acceptance). **WSLg HiDPI relaxed rule**: if Windows host is at 125%/150% scaling, the reported window dimensions may differ; the actual acceptance becomes — magnify a single `@` glyph 4× in a screenshot and verify pixel edges remain pure-color (no anti-aliased gray pixels at glyph boundaries)
- All 4 game surfaces (title / map / status / log) render at the same 32×32 cell size with no visible cell-size mismatch between adjacent surfaces
- Status row content `HP:NN/NN ATK:NN DEF:NN LV:N EXP:NN/NN G:NNN Sc:NNNN` is fully visible (none of the trailing fields are silently truncated by SadConsole's Print)
- Game can be played from floor 1 to floor 2 (move, fight, pick item, descend stairs) without crashing
- During the playthrough, `i` opens the inventory overlay and `?` opens the help overlay without crashing — this is the runtime check for plan M2 step 10's overlay smoke test, and is the empirical evidence used by Task 5 to decide skip-vs-execute when Task 2's finding C is ambiguous. (Visual cell-size verification on overlays remains Task 5's responsibility.)
- `git status` is clean before commit; commit message contains "step 2 of 3" and "M2: pipeline only, placeholder font"

---

### Task 5: (Conditional) Wire FontSize on 4 overlays if inheritance fails (M2 step 9)

**Status:** TODO

**Depends on:** Task 4

**Conditional execution:** If Task 2's notes file recorded that child `ScreenSurface` instances **automatically inherit** the new default font, this task is skipped — mark Status: SKIPPED with a one-line note pointing at the relevant Task 2 finding. If Task 2's notes recorded that they **do not inherit**, execute the task body below.

If executed: each of the 4 overlay classes (`InventoryScreen`, `HelpScreen`, `GameOverScreen`, `VictoryScreen`) currently extends `ScreenSurface` with a `: base(Layout.WindowWidth, Layout.WindowHeight)` constructor call. Add `FontSize = new Point(32, 32)` immediately after the `: base(...)` body in each overlay's constructor.

**Files to modify (only if executed):**
- `src/UI/InventoryScreen.cs`
- `src/UI/HelpScreen.cs`
- `src/UI/GameOverScreen.cs`
- `src/UI/VictoryScreen.cs`

**Acceptance criteria:**
- If skipped: `Status: SKIPPED` is written into this task's body in the task document with a one-line note referencing the Task 2 finding that justified the skip
- If executed: each of the 4 overlays, when opened during a play session at the 32×32-cell game window, renders at 32×32 cell with no visible cell-size mismatch against the underlying game surfaces
- If executed: opening overlay → closing overlay round-trip preserves the 32×32 cell size on the underlying game surfaces (no regression to the default font size after overlay close)

---

### Task 6: Source and install 3-5 candidate fonts (M3 steps 1-2)

**Status:** TODO

**Depends on:** Task 5

Research and select 3-5 candidate fonts meeting all of: (a) source 16×16 monospace bitmap pixel font, (b) license is OFL / MIT / BSD / ISC (or trigger explicit license-list extension review per plan R6), (c) Brogue-adjacent thick-stroke style, (d) full CP437 or Latin-1 glyph coverage. Sourcing leads: int10h.org Oldschool PC Fonts (VileR), GNU Unifont (already installed in Task 3), MxPlus IBM series, Press Start 2P 16×16 variants, Cherry / Curses-style fonts. For each selected candidate, create the same 3-file asset bundle as Task 3 and add a row to the assets-fonts README.

**Files to create (parametrized over N candidates, N = 3-5):**
- `assets/fonts/<candidate-name>/<candidate-name>.font` × N
- `assets/fonts/<candidate-name>/<candidate-name>.png` × N
- `assets/fonts/<candidate-name>/LICENSE.txt` × N

**Files to modify:**
- `assets/fonts/README.md` (append one row per candidate)

**File count justification:** the upper bound 16 files (5 candidates × 3 files + 1 README) exceeds the default 8-file guideline. Justification: this is a structurally repetitive bulk operation — each candidate follows an identical 3-file template, not 16 independent design decisions. The single decision is "which 3-5 candidates"; the file work is mechanical replication.

**Acceptance criteria:**
- Short list contains at least 3 and at most 5 candidates (Unifont may or may not be counted as one — if counted, do not re-install since Task 3 already placed it)
- Each candidate's `.font` JSON loads without runtime error when temporarily wired into `Program.cs` (ad-hoc validation per candidate, not a permanent test)
- Each candidate's `LICENSE.txt` contains the verbatim license text from the upstream source — no paraphrasing
- `assets/fonts/README.md` lists every candidate in the `| font name | source URL | license | designer |` table format with no missing fields
- Any candidate carrying a license outside the brief's OFL/MIT/BSD/ISC allow-list (e.g., CC BY-SA 4.0 from int10h.org) is explicitly flagged in the README row, and the decision to extend the allow-list is recorded inline (either approved or rejected — if rejected, the candidate is removed before this task is marked done)

---

### Task 7: Pin map seed, capture candidate screenshots, restore seed (M3 steps 3-5)

**Status:** TODO

**Depends on:** Task 6

Temporarily pin the 4 `new Map(_rng.Next())` call sites in `src/Game.cs` (lines 19, 27, 209, 222) to a fixed seed constant (recommend `42`; if floor 2 must also be deterministic, use 4 distinct constants and document each in `seed.txt`). Use `git stash` to safely park the change. Document the seed in `seed.txt`. For each candidate from Task 6, temporarily edit `Program.cs` to point at that candidate's `.font` file, launch the game, navigate to the same scene as Task 1's baseline (matching player position, monster placement, item placement enabled by the seed determinism), and capture a screenshot. After all candidates are screenshot-captured, `git stash pop` to restore the original `Game.cs`.

**Files to create:**
- `docs/screenshots/font-pass/seed.txt`
- `docs/screenshots/font-pass/cand-<candidate-name>.png` × N (one per Task 6 candidate)

**Files to modify temporarily (must be reverted by task end):**
- `src/Game.cs` (4 lines: 19, 27, 209, 222 — each `new Map(_rng.Next())` → `new Map(42)` or distinct constants)
- `Program.cs` (font path string — final value depends on Task 9, but during this task it cycles through candidates)

**Acceptance criteria:**
- `seed.txt` exists with the actual seed integer(s) used in plain text — committed to git as audit record
- Each candidate from Task 6 has a corresponding `cand-<candidate-name>.png` screenshot in `docs/screenshots/font-pass/`
- Each candidate screenshot is rendered against the same scene as Task 1's `before.png` (same map layout, same player position, same monster/item placement) — verifiable by overlaying or side-by-side comparison
- After task completion, `git diff src/Game.cs` does **not** contain the literal substring `new Map(42)` or any other fixed-seed constant — the four call sites have been restored to `_rng.Next()`. Verify with: `grep -n "new Map(" src/Game.cs` returns exactly the original 4 lines unchanged
- `Program.cs`'s font path is left in whatever state Task 6 last set it to — Task 9 will finalize it; this task does not constrain it

---

### Task 8: Run readability audit + select font + write decision.md (M4)

**Status:** TODO

**Depends on:** Task 7

For each candidate from Task 6, evaluate the screenshots from Task 7 against the 11-item readability checklist defined in plan M4 (variant identification × 3, combat readability × 2, UI digit legibility × 2, Brogue anchor × 3, plus the implicit "score-card pass" gate). Any candidate failing any item is rejected. Among survivors, pick the one closest to Brogue anchor and write `decision.md` documenting the selection and the rejection reasons for each rejected candidate.

**Files to create:**
- `docs/screenshots/font-pass/decision.md`

**Files to modify:**
- `assets/fonts/README.md` (add `Selected: <font-name>` line at the top, before the candidate table)

**Acceptance criteria:**
- `decision.md` contains a section for the selected font with: full font name, source URL, license, designer
- `decision.md` contains one rejection section per rejected candidate, listing each failed checklist item explicitly (e.g., "FAILED: variant identification 1 — `,` and `'` indistinguishable at 32×32 zoom")
- `assets/fonts/README.md` has a `Selected: <font-name>` marker at the top of the file
- Exactly one candidate is selected (or, if all candidates are rejected, this task triggers plan M4 escalation — see escalation note below)

**Escalation (if all 3-5 candidates are rejected):** Stop this task as INCOMPLETE. Do not proceed to Task 9. Choose one of the plan M4 escalation paths: (1) expand candidate pool to 8 max and return to Task 6, (2) rewrite the plan to pursue path C (self-edited CP437), (3) rewrite the plan to pursue path B1 (source 32×32 font). Whichever path is chosen, update `docs/plans/pixel-font-pass.md` accordingly and re-emit the relevant tasks before continuing.

---

### Task 9: Cleanup unused candidates + final play-test + commit (M5)

**Status:** TODO

**Depends on:** Task 8

Delete every unused candidate font directory under `assets/fonts/` (including the Unifont placeholder unless it happens to be the selected font). Update `Program.cs` to point at the selected font. Update `assets/fonts/README.md` to keep only the selected font row plus the `Selected:` header. Verify Task 7's seed cleanup is intact. Play one full session covering all overlays. Commit with the F1-compliant commit message.

**Files to delete:**
- `assets/fonts/<rejected-candidate>/` × (N - 1) (every directory except the one selected in Task 8)

**Files to modify:**
- `Program.cs` (font path → selected font's `.font` path)
- `assets/fonts/README.md` (remove rejected-candidate rows; keep selected font + `Selected:` marker)

**Files to verify (no modification, just check):**
- `src/Game.cs` (must already be free of Task 7's seed pin — `grep -n "new Map(" src/Game.cs` returns exactly 4 unchanged `_rng.Next()` lines)

**Acceptance criteria:**
- `assets/fonts/` contains exactly one font directory (the selected font) plus `README.md`
- `docs/screenshots/font-pass/` retains all audit material: `before.png`, `cand-*.png` for every candidate (rejected and selected), `decision.md`, `seed.txt`, `sadconsole-api-notes.md`
- `git diff src/Game.cs` is empty (no leftover from Task 7)
- Play-test covers: floor 1 → combat with at least one monster → pick at least one item → descend to floor 2 → open inventory overlay → open help overlay → trigger either game-over or victory overlay
- All 4 overlays render at 32×32 cell with no visual regression
- `git status` is clean before the commit
- Commit message body explicitly contains both phrases: `step 2 of 3` and `does not complete visual polish` (capitalization flexible; phrasing exact)

---

## Notes

- All candidate font work happens under `assets/fonts/`. License files (`LICENSE.txt`), audit records (`README.md`), and seed records (`seed.txt`) are intentionally **not** in the `<Content>` include — they are repository-internal audit material, not runtime assets.
- M2's API verification (Task 2) is the single point at which plan assumptions about SadConsole 10.9 are checked. If verification refutes any assumption, the plan must be updated before Task 3 begins; the task list itself may not need to change, but Task 3's `.font` schema and Task 4's `Game.Create` call signature track Task 2's findings.
- Task 5's conditional execution depends entirely on Task 2's finding C (font inheritance). Resolve Task 2 fully before estimating Task 5 effort.
- The temporary `Program.cs` font-path edits in Task 7 are uncommitted — they cycle through candidates. Task 9 finalizes the path. There is no "git stash" requirement on `Program.cs` because the final state is determined by Task 8's selection.

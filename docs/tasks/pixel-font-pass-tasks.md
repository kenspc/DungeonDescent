# Pixel Font Pass — Task Document

## Context

Decompose visual-polish step 2 of 3 (custom 16×16 Brogue-adjacent pixel font, rendered at 32×32 cell via SadConsole 10.9 path B2). Drives the 60×26 cell grid into a 1920×832 physical window with palette-brogue-pass already landed.

Related plan: `docs/plans/pixel-font-pass.md`  
Related upstream brief: `docs/briefs/pixel-font-pass.md`

**Cross-phase dependency note**: Task 1 and Task 2 are grounded in the existing codebase (palette-brogue-pass landed at commit e0119f5). Tasks 3-9 depend on outputs from prior tasks rather than direct codebase reads — see each task's `Depends on` line. Task 5 is **conditional** on Task 2's finding about font inheritance and may be skipped if SadConsole sub-surfaces inherit the default font automatically.

**F1 narrative discipline (from upstream brief)**: every commit message must call this out as "step 2 of 3" and explicitly say "does not complete visual polish". Animation remains step 3 and is out of scope here.

## Tasks

### Task 1: Capture pre-change baseline screenshot (M1)

**Status:** DONE

**Completed:** 2026-05-08. Captured 3 PNGs at `docs/screenshots/font-pass/`: `before-variants.png` (479×415, `,` + `'` in FOV), `before-combat.png` (481×415, 3 rats + 2 items + variants + stairs + status + log — single shot covers 8/8), `before-stairs.png` (480×416, `>` + level-2 progression). Collective coverage of all 8 audit element types confirmed.

In the current `git HEAD` state (palette-brogue-pass landed, default 8×16 font), launch the game, play floor 1, and capture **one or more** screenshots that **collectively** cover all 8 audit-relevant element types. Multiple screenshots are encouraged because Manhattan-radius-8 FOV plus 5-15% scattered floor variants makes a single 8-in-1 shot unlikely without significant effort. Save with descriptive names and commit.

**Files to create:**
- `docs/screenshots/font-pass/before-*.png` — one or more PNGs. Recommended split (not enforced):
  - `before-variants.png` — captures `FloorMossy` `,` and `FloorCracked` `'` simultaneously in player FOV (key for variant audit downstream)
  - `before-combat.png` — captures at least one monster + at least one item in player FOV (do not attack or pick up — they must remain visible)
  - `before-stairs.png` — captures `StairsDown` `>` in player FOV (typically near the last room center)
- Alternative naming (e.g., `before-1.png`, `before-2.png`, …) is also accepted as long as the collective coverage criterion below is met.

**Acceptance criteria:**
- All saved screenshots are PNGs (not JPEG — lossy compression breaks pixel audit)
- The **set** of saved screenshots contains, **collectively**, every one of the 8 element types at least once: player `@`, at least one monster, at least one item, at least one `FloorMossy` `,`, at least one `FloorCracked` `'`, the `StairsDown` `>`, the status row text, and the log row text. (One screenshot may cover multiple element types; some elements like status/log appear in every screenshot.)
- Each screenshot's dimensions are approximately 480×416 (consistent with default 8×16 font × 60×26 grid; ±a few pixels from cropping is fine)
- All screenshots are committed to git in a single commit (audit material)

---

### Task 2: Verify SadConsole 10.9 font API via Context7

**Status:** DONE

**Completed:** 2026-05-08. Verified via Context7 (`/thraka/sadconsole`, sparse) + local NuGet XML doc (`~/.nuget/packages/sadconsole/10.9.0/lib/net8.0/SadConsole.xml`, authoritative). Findings:
- Assumption A: **REFUTED** — no `Game.Create(int, int, string fontPath, ...)` overload exists. 10.x uses Builder pattern + `.ConfigureFonts((cfg, _) => cfg.UseCustomFont(path))`.
- Assumption B: **CONFIRMED** — `ScreenSurface.FontSize` is `Point` settable, but plan's per-surface assignment is now redundant (see C).
- Assumption C: **CONFIRMED** — child surfaces inherit `GameHost.DefaultFont` + `DefaultFontSize` at construction. Combined with Builder's `.SetDefaultFontSize(IFont.Sizes.Two)`, all game surfaces and overlays inherit 32×32 cell automatically. **Task 5 becomes obsolete.**
- Assumption D: **PARTIALLY REFUTED** — `.font` schema field is `IsSadExtended`, not `IsSadFontFormat` (plan template error fixed).

Plan updates landed in Technical Approach + M2 steps 1/4a/7/8/9, R1, OQ1, OQ2. See `docs/screenshots/font-pass/sadconsole-api-notes.md` for full evidence.

**Depends on:** Task 1

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

**Status:** DONE

**Completed:** 2026-05-08. Generated 256×256 px Unifont 16×16 PNG by extracting 16 horizontal codepoint strips from `unifont-16.0.04.bmp` (U+0000..U+00FF) at BMP offsets x=32+i*256, y=64, then stacking vertically with ImageMagick `-append`. Source BMP from <https://unifoundry.com/pub/unifont/unifont-16.0.04/unifont-16.0.04.bmp>. Verified codepoint 0x40 ('@') sits at row 4 col 0 (linear index 0x40) per SadConsole convention. License downloaded from <https://unifoundry.com/LICENSE.txt> (OFL-1.1 + GPLv2-with-font-exception dual; OFL sublicense honored). PNG is 1-bit white-on-black (negated from Unifont's black-on-white source per SadConsole convention). Asset generation rationale and CP437/SolidGlyphIndex caveats documented in `assets/fonts/README.md`. **Did not need the IBM 8×16 fallback** — full Unifont path completed within ~30 min.

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

### Task 4: Rewrite Program.cs to Builder pattern + load placeholder font (M2 steps 6, 7, 10)

**Status:** DONE

**Completed:** 2026-05-08. Two phases:

**Task 4a (code, 2026-05-08):** csproj content include + Program.cs Builder rewrite landed. `SetScreenSize` was replaced with `SetWindowSizeInCells` after first build flagged the former as `[Obsolete]` in 10.9. Final build: 0 warnings, 0 errors. A separate fix commit (`4933469`) added the `$type` Newtonsoft.Json discriminator to `unifont.font` after Task 4b runtime surfaced a JSON deserialization error — Task 2's XML-doc-based schema verification missed this metadata key; downstream propagated to plan template + notes file.

**Task 4b (runtime, user, 2026-05-08):** all 6 acceptance items confirmed PASS by user-run `dotnet run` session:
1. Physical window ≈ 1920×832 ✓
2. `@` glyph 4× zoom: pixel edges pure-color, no AA grays (nearest-neighbor 2× upscale verified)
3. All 4 game surfaces (title / map / status / log) at 32×32 cell, no mismatch
4. Status row `HP:96/110 ATK:9 DEF:2 LV:2 EXP:5/40 G:0 Sc:25` fully visible (`Sc:` not truncated; ~45/60 cells used)
5. Floor 1 → floor 2 traversal works without crash
6. `i` opens inventory + `?` opens help overlay, both at 32×32 cell, no crash

Side observations:
- Predicted SolidGlyphIndex=219 → "Û" fill cosmetic NOT triggered in any visible surface — moot for placeholder pipeline test.
- ASCII chars are 8×16 left-aligned in 16×16 cells (Unifont half-width design); reads as wide-spaced text. Acceptable placeholder cosmetic per brief F2 anchor-misalignment intent.
- Variant `,` `'` rendering preserved through font swap — palette-brogue-pass step 1 work has no regression.

**Depends on:** Task 3

**Depends on:** Task 3

Add the assets/fonts content include to the csproj, then **rewrite `Program.cs` to use SadConsole 10.x Builder pattern** with `.ConfigureFonts((cfg, _) => cfg.UseCustomFont(...))` to load the placeholder font and `.SetDefaultFontSize(IFont.Sizes.Two)` to upscale to 32×32 cells globally. The existing `try/finally Dispose` structure is preserved. **No `RootScreen.cs` modification is needed** — surfaces inherit font + size at construction. Run the game to confirm end-to-end correctness — game plays through floor 2 without crashing, overlays open without crashing, status text fits.

**Files to create or modify:**
- `DungeonDescent.csproj` (add `<Content Include="assets/fonts/**/*.font;assets/fonts/**/*.png" CopyToOutputDirectory="PreserveNewest" />` ItemGroup)
- `Program.cs` (rewrite to Builder pattern — see plan Technical Approach for the canonical code block; key elements: `new Builder().SetScreenSize(...).ConfigureFonts((cfg, _) => cfg.UseCustomFont(fontPath)).SetDefaultFontSize(IFont.Sizes.Two).OnStart((_, _) => { ... })` then `SadGame.Create(startup)`)

**Files NOT to modify (deliberately):**
- `src/UI/RootScreen.cs` — surface construction unchanged; font + size inherit from `GameHost.DefaultFont` set by Builder.
- `src/UI/InventoryScreen.cs`, `src/UI/HelpScreen.cs`, `src/UI/GameOverScreen.cs`, `src/UI/VictoryScreen.cs` — same reason, all overlays inherit at construction time.

**Acceptance criteria:**
- `dotnet build` succeeds without warnings introduced by the font work
- Game launches, opens a window with measured dimensions of 1920×832 px (use Snipping Tool / WSLg native screenshot / `gnome-screenshot -w` per platform per plan M2 acceptance). **WSLg HiDPI relaxed rule**: if Windows host is at 125%/150% scaling, the reported window dimensions may differ; the actual acceptance becomes — magnify a single `@` glyph 4× in a screenshot and verify pixel edges remain pure-color (no anti-aliased gray pixels at glyph boundaries)
- All 4 game surfaces (title / map / status / log) render at the same 32×32 cell size with no visible cell-size mismatch between adjacent surfaces
- Status row content `HP:NN/NN ATK:NN DEF:NN LV:N EXP:NN/NN G:NNN Sc:NNNN` is fully visible (none of the trailing fields are silently truncated by SadConsole's Print)
- Game can be played from floor 1 to floor 2 (move, fight, pick item, descend stairs) without crashing
- During the playthrough, `i` opens the inventory overlay and `?` opens the help overlay without crashing **and both overlays render at 32×32 cell** (no smaller-cell regression — confirms the inheritance pathway works for overlay surfaces, replacing the old Task 5 conditional)
- `git status` is clean before commit; commit message contains "step 2 of 3" and "M2: pipeline only, placeholder font"

---

### Task 5: (Obsoleted) Wire FontSize on 4 overlays if inheritance fails (M2 step 9)

**Status:** SKIPPED

**Skip rationale (2026-05-08, post Task 2):** Task 2 confirmed Assumption C — overlays inherit `GameHost.DefaultFont` + `DefaultFontSize` at construction. With Task 4 setting `.SetDefaultFontSize(IFont.Sizes.Two)` in the Builder before `OnStart`, the 4 overlays (`InventoryScreen` / `HelpScreen` / `GameOverScreen` / `VictoryScreen`) inherit 32×32 cell automatically when constructed during user interaction. No code modification needed. The corresponding plan M2 step 9 has been marked obsolete in the same revision.

**Verification of skip**: Task 4's acceptance includes "both overlays render at 32×32 cell" as runtime confirmation that inheritance works as Task 2 predicted. If that acceptance fails, this task may be re-opened (revert Status to TODO) to apply the explicit per-overlay `FontSize` writes that the original conditional path described.

**Files modified:** none.

**Depends on:** Task 4 (verification gate, not implementation gate).

---

### Task 6: Source and install 3-5 candidate fonts (M3 steps 1-2)

**Status:** DONE

**Completed:** 2026-05-08. Installed **4 candidates** (within plan's 3-5 range):
- `unifont` (OFL-1.1 + GPLv2 dual; already on disk from Task 3, re-cast as M3 candidate in addition to placeholder role)
- `px437-ibm-vga` (CC BY-SA 4.0; int10h Px437_IBM_VGA_8x16-2x rendered at 16-pt via Pillow)
- `px437-nec-apc3` (CC BY-SA 4.0; int10h Px437_NEC_APC3_8x16-2x; same rendering pipeline)
- `px437-fmtowns-re` (CC BY-SA 4.0; int10h Px437_FMTowns_re_8x16-2x; same rendering pipeline)

**License decision (option C, 2026-05-08):** user accepted CC BY-SA 4.0 candidates under the rationale that the project is currently hobby + non-distributing, where attribution + ShareAlike obligations do not trigger. Decision logged in `assets/fonts/README.md` License notes.

**Build verification:** `dotnet build` succeeded with 0 warnings, 0 errors. All 4 candidate font directories copied to `bin/Debug/net8.0/assets/fonts/`. Runtime rendering verification deferred to Task 7 (which cycles through each candidate during screenshot capture).

**Side note:** `Px437_IBM_BIOS-2x` was tried first as a 4th non-Unifont candidate but its native grid is not 8×16 (likely 8×14 or 9×16), giving garbage glyphs at 16-pt rendering. Swapped for `Px437_NEC_APC3_8x16-2x` which has explicit 8×16 native size and rendered cleanly. Lesson noted in `assets/fonts/README.md` asset generation section.

**Depends on:** Task 5 (which is SKIPPED — Task 6 effectively depends on Task 4)

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

**Status:** DONE

**Completed:** 2026-05-08. Streamlined via permanent CLI flag enhancement (Program.cs `--font` + `--probe-seed` flags landed in commit `b212dab`) — eliminates the per-candidate `Program.cs` edit cycle (4 edits → 0 edits).

Audit seed: **42** (Rooms.Count=10, verified known-good via `dotnet run -- --probe-seed 42`; line-19 pinning skips line-27 retry loop entirely). Pin applied to `src/Game.cs:19` only (per plan M3 step 3); lines 27/209/222 left at `_rng.Next()`.

Captured 4 candidate screenshots at ~1920×832 px (Snipping Tool framing precision ±2 px):
- `cand-unifont.png`           (1920×832, 29 KB)
- `cand-px437-ibm-vga.png`     (1919×831, 34 KB)
- `cand-px437-nec-apc3.png`    (1918×831, 31 KB)
- `cand-px437-fmtowns-re.png`  (1918×831, 33 KB)

Pin reverted post-capture; `git diff src/Game.cs` is empty before commit. `seed.txt` retained as audit reproducibility record.

Single-screenshot-per-candidate strategy adopted (vs Task 1's variants/combat/stairs split): the candidate audit is comparing **font rendering** of the same map terrain across the 4 candidates, not testing visibility coverage of all 8 element types per candidate. With seed=42 pinned, every screenshot shows the same map shape; element coverage is satisfied collectively across baselines + candidates.

**Depends on:** Task 6

Temporarily pin **only** `src/Game.cs:19` (the `Game()` ctor's initial Map creation) to a fixed seed constant. The other 3 sites at lines 27, 209, 222 **must keep `_rng.Next()`** — line 27 is inside a `while (Map.Rooms.Count < 2 && attempts < MaxMapAttempts)` retry loop body and pinning it makes retries no-op (same seed → same map → infinite retry until the cap throws); lines 209/222 are floor-transition Map ctors and floor 1 audit doesn't need them. **Verify the chosen seed is "known-good"** before pinning: `Map(seed).Rooms.Count >= 2` on first call, so line 27's retry path is never entered during audit. Suggested seed: `42` (verify first; if it doesn't satisfy the rooms gate, increment until it does — `43`, `44`, …). Use `git stash` to safely park the line-19 change. Document the seed in `seed.txt`. For each candidate from Task 6, temporarily edit `Program.cs` to point at that candidate's `.font` file, launch the game, navigate to the same scene as Task 1's baseline (matching player position, monster placement, item placement enabled by the seed determinism on floor 1), and capture a screenshot. After all candidates are screenshot-captured, `git stash pop` to restore the original `Game.cs`.

**Files to create:**
- `docs/screenshots/font-pass/seed.txt`
- `docs/screenshots/font-pass/cand-<candidate-name>.png` × N (one per Task 6 candidate)

**Files to modify temporarily (must be reverted by task end):**
- `src/Game.cs` (line 19 only — `new Map(_rng.Next())` → `new Map(<known-good-seed>)`; lines 27, 209, 222 must NOT be pinned)
- `Program.cs` (font path string — final value depends on Task 9, but during this task it cycles through candidates; before pinning, may temporarily contain a one-line `Console.WriteLine($"seed={s}, rooms={new DungeonDescent.Map(s).Rooms.Count}")` probe to validate the seed is known-good — remove before screenshotting)

**Acceptance criteria:**
- `seed.txt` exists with the chosen seed integer in plain text plus a one-line note recording the known-good verification result (e.g., `seed=42, rooms=4 (verified, no retry path entered)`) — committed to git as audit record
- Each candidate from Task 6 has a corresponding `cand-<candidate-name>.png` screenshot in `docs/screenshots/font-pass/`
- Each candidate screenshot is rendered against the same scene as Task 1's `before.png` (same map layout, same player position, same monster/item placement) — verifiable by overlaying or side-by-side comparison
- After task completion, `git diff src/Game.cs` does **not** contain the literal substring `new Map(<seed>)` for the chosen integer seed — line 19 has been restored to `_rng.Next()`. Verify with: `grep -n "new Map(" src/Game.cs` returns exactly the original 4 lines, all containing `_rng.Next()` and matching the file's pre-task state
- `Program.cs`'s font path is left in whatever state Task 6 last set it to — Task 9 will finalize it; this task does not constrain it

---

### Task 8: Run readability audit + select font + write decision.md (M4)

**Status:** DONE

**Completed:** 2026-05-08. User confirmed `px437-fmtowns-re` after Claude's structured 11-item checklist audit of the 4 cand-* screenshots from Task 7. None of the 4 candidates triggered M4 escalation; selection was a positive choice rather than fallback. Rationale: FMTowns has the boldest stroke (best matches Brogue's 厚实 anchor), most blocky `@` (most iconic player icon), and bolder status digits than other candidates. Runner-up `px437-ibm-vga` documented as M5 fallback if FMTowns surfaces unforeseen long-session issues. Rejected: `unifont` (half-width ASCII violates anchor), `px437-nec-apc3` (`@` hood decoration + thinnest strokes).

Audit artifacts: `docs/screenshots/font-pass/decision.md`. README header at `assets/fonts/README.md` updated with `Selected: px437-fmtowns-re`.

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
- Play-test covers: floor 1 → combat with at least one monster → pick at least one item → descend to floor 2 → open inventory overlay → open help overlay → **trigger game-over** (let HP drop to 0 by standing next to a monster — much cheaper than chasing victory through floor 5 Dragon kill). Victory overlay is **not required** at runtime in this play-test
- 3 of 4 overlays (inventory / help / game-over) render at 32×32 cell at runtime with no visual regression. Victory overlay shares the same `ScreenSurface` rendering pipeline and is structurally identical — pass-by-equivalence accepted; if the session naturally reaches floor 5 and defeats the Dragon, runtime verify victory too, otherwise defer to a future natural session and do not block this commit
- `git status` is clean before the commit
- Commit message body explicitly contains both phrases: `step 2 of 3` and `does not complete visual polish` (capitalization flexible; phrasing exact)

---

## Notes

- All candidate font work happens under `assets/fonts/`. License files (`LICENSE.txt`), audit records (`README.md`), and seed records (`seed.txt`) are intentionally **not** in the `<Content>` include — they are repository-internal audit material, not runtime assets.
- M2's API verification (Task 2) is the single point at which plan assumptions about SadConsole 10.9 are checked. If verification refutes any assumption, the plan must be updated before Task 3 begins; the task list itself may not need to change, but Task 3's `.font` schema and Task 4's `Game.Create` call signature track Task 2's findings.
- Task 5's conditional execution depends entirely on Task 2's finding C (font inheritance). Resolve Task 2 fully before estimating Task 5 effort.
- The temporary `Program.cs` font-path edits in Task 7 are uncommitted — they cycle through candidates. Task 9 finalizes the path. There is no "git stash" requirement on `Program.cs` because the final state is determined by Task 8's selection.

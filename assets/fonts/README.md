# Fonts

This directory holds the custom pixel font loaded by `Program.cs` via SadConsole's `Builder.ConfigureFonts((cfg, _) => cfg.UseCustomFont(path))`.

## Selected font: `px437-fmtowns-re`

| Field      | Value                                                              |
|------------|--------------------------------------------------------------------|
| Source URL | <https://int10h.org/oldschool-pc-fonts/fontlist/>                  |
| TTF source | int10h pack v2.2 — `Px437_FMTowns_re_8x16-2x.ttf`                   |
| License    | CC BY-SA 4.0                                                       |
| Designer   | VileR @ int10h.org (recreation of Fujitsu FM Towns workstation BIOS font) |
| Cell size  | 16×16 source × `IFont.Sizes.Two` = 32×32 cell at runtime           |

Selected 2026-05-08 after the M3/M4 audit. Full audit log + per-candidate rejection reasons: `docs/screenshots/font-pass/decision.md`. Fallback path documented there if FMTowns surfaces unforeseen long-session issues post-M5.

## License notes

`px437-fmtowns-re` is licensed under CC BY-SA 4.0 (full text in `px437-fmtowns-re/LICENSE.txt`). The brief originally allowed only OFL / MIT / BSD / ISC, but the user adopted **option C (2026-05-08)**: at the current hobby + non-distributing project stage, CC BY-SA 4.0 imposes no obligations (attribution + ShareAlike kick in only on redistribution). If/when the project starts being distributed publicly, attribution must be added (typically a "Fonts" credits screen or `CREDITS.md`) and ShareAlike must be considered for any derivative works. Re-evaluating this decision is a one-line `Program.cs` font-path edit + asset swap if a stricter license is needed.

## Asset generation

Generated 2026-05-08 by rendering `Px437_FMTowns_re_8x16-2x.ttf` (from int10h pack `oldschool_pc_font_pack_v2.2_FULL.zip`) at 16-pt Pillow `ImageFont.truetype` with `mode="1"` 1-bit canvas (no anti-aliasing). For each codepoint 0x00-0xFF, `Draw.text` paints the glyph at position `(col*16, row*16)` where `col = codepoint % 16, row = codepoint // 16` — matching SadConsole's linear-cell-index addressing. The Px series TTFs are int10h's pixel-outline rendering specifically designed to render exactly at certain point sizes; 16-pt matches the `8x16-2x` doubled native size.

## SadConsole 10.x .font JSON schema

`px437-fmtowns-re.font` includes the **`$type` Newtonsoft.Json discriminator** (`"SadConsole.SadFont, SadConsole"`) as the first key. Without this, `GameHost.LoadFont` throws `JsonSerializationException`. See `docs/screenshots/font-pass/sadconsole-api-notes.md` Assumption D for full citation.

## Adding more fonts later (e.g., for a future visual-polish iteration)

1. Render TTF (or other source) to a 256×256 PNG glyph table at 16×16 per cell
2. Author `<name>.font` JSON using the canonical template (see `sadconsole-api-notes.md` Assumption D)
3. Drop the font's `LICENSE.txt` into `<name>/`
4. Update this README with a new row
5. Either (a) point `Program.cs:string defaultFont` at the new font, or (b) use the `--font` CLI flag for ad-hoc testing without committing

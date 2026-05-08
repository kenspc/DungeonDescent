# Fonts

This directory holds custom pixel fonts loaded by `Program.cs` via SadConsole's `Builder.ConfigureFonts((cfg, _) => cfg.UseCustomFont(path))`.

## Selected (M4): `px437-fmtowns-re`

Decided 2026-05-08 after structured 11-item readability audit. Full rationale + per-candidate audit log: `docs/screenshots/font-pass/decision.md`. Runner-up `px437-ibm-vga` remains documented as fallback. Other candidates will be removed in Task 9 cleanup.

## Candidates (M3 short list, 4 fonts)

| Font name           | Source URL                                             | License                                            | Designer / Maintainer                       | Role                                                                  | Native size | Cell size after 16-pt rendering |
|---------------------|--------------------------------------------------------|----------------------------------------------------|---------------------------------------------|-----------------------------------------------------------------------|-------------|---------------------------------|
| `unifont`           | <https://unifoundry.com/unifont/index.html>            | OFL-1.1 + GPLv2-with-font-exception (dual)         | Roman Czyborra et al. (Unifoundry / GNU)    | M2 placeholder; M3 candidate (will be evaluated then likely rejected) | 16×16       | 16×16 (half-width ASCII)        |
| `px437-ibm-vga`     | <https://int10h.org/oldschool-pc-fonts/fontlist/>      | CC BY-SA 4.0                                       | VileR @ int10h.org                          | M3 candidate                                                          | 8×16 ×2     | 16×16 (full-width)              |
| `px437-nec-apc3`    | <https://int10h.org/oldschool-pc-fonts/fontlist/>      | CC BY-SA 4.0                                       | VileR @ int10h.org                          | M3 candidate                                                          | 8×16 ×2     | 16×16 (full-width)              |
| `px437-fmtowns-re`  | <https://int10h.org/oldschool-pc-fonts/fontlist/>      | CC BY-SA 4.0                                       | VileR @ int10h.org                          | M3 candidate                                                          | 8×16 ×2     | 16×16 (full-width)              |

## License notes

- **Unifont** is dual-licensed under OFL-1.1 and GPLv2 with font exception. We honor the OFL-1.1 sublicense, which matches the brief's original allow-list (OFL / MIT / BSD / ISC).
- **px437-* (int10h.org)** are licensed under CC BY-SA 4.0. The plan brief originally called for OFL/MIT/BSD/ISC only, but the user adopted **option C (2026-05-08)**: at the current hobby + non-distributing project stage, CC BY-SA 4.0 imposes no obligations (attribution + ShareAlike kick in only on redistribution). The decision to either keep or replace these fonts is deferred until the project actually ships. If/when the project starts being distributed publicly, attribution must be added (typically a "Fonts" credits screen or `CREDITS.md`) and ShareAlike must be considered for any derivative works.
- License full text for each font is in `<font-name>/LICENSE.txt`.

## Asset generation

### `unifont` (placeholder + candidate)

Generated 2026-05-08 from `unifont-16.0.04.bmp` (downloaded from Unifoundry on the same day). Extracted 16 horizontal strips of 256 px × 16 px each from BMP plane row 0 (codepoints U+0000-U+00FF) at offsets `x=32+i*256, y=64`, then negated (Unifont is black-on-white, SadConsole expects white-on-black) and stacked vertically with `convert -append`. Glyphs are Unifont's 8×16 half-width designs left-aligned in 16×16 cells — the unfilled right half of each cell is the visual cue that this is a placeholder, not a real candidate.

### `px437-*` (int10h candidates)

Generated 2026-05-08 by rendering int10h's "Px (pixel outline)" TTF series at 16-point Pillow `ImageFont.truetype` with `mode="1"` 1-bit canvas (no anti-aliasing). For each codepoint 0x00-0xFF, `Draw.text` paints the glyph at position `(col*16, row*16)` where `col = codepoint % 16, row = codepoint // 16`. The Px series TTFs are int10h's pixel-outline rendering specifically designed to render exactly at certain point sizes, and 16-pt matches the `8x16-2x` doubled native size. `IBM_BIOS-2x` was tried initially but gave garbage at 16-pt (BIOS variant is not native 8×16 — likely 8×14 or 9×16); replaced with NEC APC3 8x16-2x which renders cleanly. Source TTFs from `oldschool_pc_font_pack_v2.2_FULL.zip`.

The Python rendering script is in the project's git history (commits referencing `render_font.py`); the script is not committed — it was a one-off tool.

## SolidGlyphIndex

All four `.font` files declare `SolidGlyphIndex: 219` (CP437 convention for ▓). Behavior:

- **`unifont`**: codepoint 219 maps to U+00DB ("Û"); SadConsole solid-fill operations would render Û glyphs (none observed in current Dungeon Descent UI surfaces).
- **`px437-*`**: codepoint 219 is properly the CP437 ▓ medium-shade block — fills work as expected.

## SadConsole 10.x .font JSON schema

All four `.font` files include the **`$type` Newtonsoft.Json discriminator** (`"SadConsole.SadFont, SadConsole"`) as the first key. Without this, `GameHost.LoadFont` throws `JsonSerializationException: Could not create an instance of type SadConsole.IFont. Type is an interface or abstract class and cannot be instantiated.` This requirement was missed by the XML-doc-based schema verification in Task 2 and discovered at runtime in Task 4b — the canonical `.font` template now carries this field. See `docs/screenshots/font-pass/sadconsole-api-notes.md` Assumption D for full citation.

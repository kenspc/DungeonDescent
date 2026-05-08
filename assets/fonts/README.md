# Fonts

This directory holds custom pixel fonts loaded by `Program.cs` via SadConsole's `Builder.ConfigureFonts((cfg, _) => cfg.UseCustomFont(path))`.

| Font name | Source URL | License | Designer / Maintainer | Role | Size |
|---|---|---|---|---|---|
| unifont | <https://unifoundry.com/unifont/index.html> | OFL-1.1 + GPLv2-with-font-exception (dual) | Roman Czyborra et al. (Unifoundry / GNU Project) | M2 placeholder — pipeline validation only, not a real candidate | 16×16 |

**License notes:** Unifont is dual-licensed under OFL-1.1 and GPLv2 with font exception. We use the OFL-1.1 sublicense, which matches the brief's allow-list (OFL / MIT / BSD / ISC). License text in `unifont/LICENSE.txt`.

**Asset generation note (unifont):** The 256×256 PNG glyph table was generated from `unifont-16.0.04.bmp` (downloaded from Unifoundry on 2026-05-08) by extracting 16 horizontal strips of 256 codepoints (offset x=32, y=64+i*0; bands 256×16 px each), inverting black-on-white to white-on-black, and stacking vertically. ImageMagick `convert` did the work. Strips were taken from BMP plane row 0 (codepoints U+0000-U+00FF), then re-arranged into 16-cols × 16-rows grid to satisfy SadConsole's linear-cell-index addressing. The control-character glyphs (U+0000-U+001F) appear as Unifont's text labels ("NUL", "SOH", etc.) — this is correct Unifont behavior, not a layout bug. CP437 box-drawing glyphs (0x80-0xFF range) do NOT match because Unifont indexes by Unicode codepoint, not CP437; for a placeholder this is acceptable since the game uses only ASCII printable glyphs (`@`, monsters, items, `,`, `'`, `.`, `<`, `>`, `#`).

**SolidGlyphIndex:** `.font` declares 219 (CP437 convention for ▓). In Unifont this maps to U+00DB ("Û") — SadConsole's solid-fill operations will render Û glyphs instead of solid blocks. Acceptable for a deliberately-ugly placeholder; real candidates should provide a proper solid block at 219.

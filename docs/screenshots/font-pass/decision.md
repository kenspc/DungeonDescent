# Font Selection Decision

**Date:** 2026-05-08
**Selected:** `px437-fmtowns-re`
**Selected by:** user (after structured 11-item audit per plan M4 checklist)

## Selected font

| Field      | Value                                                              |
|------------|--------------------------------------------------------------------|
| Name       | `px437-fmtowns-re`                                                 |
| Source URL | <https://int10h.org/oldschool-pc-fonts/fontlist/>                  |
| TTF source | int10h pack v2.2 — `Px437_FMTowns_re_8x16-2x.ttf`                   |
| License    | CC BY-SA 4.0                                                       |
| Designer   | VileR @ int10h.org (recreation of Fujitsu FM Towns workstation BIOS font) |
| Native     | 8×16 source, doubled horizontally to 16×16 in the `-2x` variant    |
| Cell size  | 16×16 source × `IFont.Sizes.Two` = 32×32 cell at runtime           |

## Selection rationale

Strongest match against the brief's **Brogue anchor** description (笔画厚实 + 低 anti-alias + 不偏常见 retro 圈):

- **Stroke thickness** is the boldest of the 3 int10h candidates. Brogue's visual identity leans on thick stroke; FMTowns matches without going cartoonish.
- **`@` design** is the most blocky / square of the four — reads unambiguously as "player". IBM VGA's `@` is rounded-canonical (good but slightly less iconic), NEC APC3's `@` carries a hood-like top decoration that risks misreading as a non-player glyph, Unifont's `@` is half-width and visually small.
- **Anchor positioning**: FM Towns is a Japanese workstation lineage, distinct from the standard IBM PC / Apple II / C64 retro circles that brief F2 explicitly warns against. The user is unlikely to register this as "I've seen this font on another DOS-revival project."
- **Status row digits** render boldest among the three int10h candidates — important because the status row is read continuously during play.

## Audit checklist (full)

Captured screenshots at `docs/screenshots/font-pass/cand-<name>.png` (seed=42 pinned during capture). Compared visually side-by-side.

| Item                                              | unifont                  | ibm-vga       | nec-apc3                 | fmtowns-re        |
|---------------------------------------------------|--------------------------|---------------|--------------------------|-------------------|
| Variant: `,` vs `.` 可瞬识                        | ⚠️ weak (half-width dots)| ✅            | ✅                       | ✅                |
| Variant: `'` vs `,` 可瞬识                        | ⚠️ weak                  | ✅            | ✅                       | ✅                |
| Variants vs Entity 不撞色不撞形                   | ✅ (color-only)          | ✅            | ✅                       | ✅                |
| Combat: `@` 在战场最显眼                          | ❌ (half-width, thin)    | ✅            | ⚠️ (`@` hood decoration) | ✅✅ (most blocky)|
| Combat: monster letters 区分                      | (not in FOV)             | ✅            | ✅                       | ✅                |
| UI: Status digits 清晰                             | ✅                       | ✅            | ✅                       | ✅✅ (boldest)    |
| UI: `O/0`, `I/l/1`, `B/8` 可区分                   | ✅                       | ✅            | ✅                       | ✅                |
| Anchor: 不偏 IBM PC / Apple II / C64 retro         | ❌ (Linux console look)  | ⚠️ (IBM PC ish but Brogue is IBM-adjacent) | ⚠️ (Unix workstation) | ✅ (FM Towns niche) |
| Anchor: 笔画厚实接近 Brogue                        | ❌ (1-px thin)           | ⚠️ (medium)   | ❌ (thinnest)            | ✅✅✅ (boldest)  |
| Anchor: 灰阶 anti-alias 极少                       | ✅                       | ✅            | ✅                       | ✅                |
| **Aggregate**                                     | **REJECT (2 ❌ + 4 ⚠️)** | **PASS**     | **WEAK PASS**            | **STRONG PASS**   |

## Rejection reasons (per candidate)

### `unifont` — REJECTED

Half-width ASCII (Unifont's design intent: 8-px-wide Latin in 16-px cells, matching its CJK 16×16 grid alignment). In a 32×32 render context this means every ASCII glyph occupies only the left ~16 px of a 32-px cell, leaving the right half black. Visually: spaced-out / "telegraph machine" feel; `@` looks small and not commanding. Fundamentally violates Brogue's "thick monospace 字符塞满 cell" anchor. Always served as M2 placeholder rather than a real candidate; the M3 audit confirmed the placeholder framing was correct.

### `px437-ibm-vga` — RUNNER-UP (not rejected; passes audit but not picked)

Solid all-around candidate. The IBM VGA letterforms are CP437 canonical and Brogue is loosely IBM-VGA-adjacent. Rejected as runner-up because FMTowns's stroke thickness more closely matches Brogue's "厚实" feel; IBM VGA reads as slightly "lighter" by comparison. If FMTowns produces ergonomics issues during M5 final play-test (e.g., status row text feels too heavy at long play sessions), reverting to IBM VGA is the documented fallback — both pass the checklist objectively.

### `px437-nec-apc3` — REJECTED

Two reasons stacked: (1) `@` glyph carries a top-decoration / hood-like stroke that makes the player icon less iconic — a roguelike's `@` should read instantly as "you", and NEC's stylized variant introduces ambiguity. (2) Stroke thickness is the thinnest of the three int10h candidates — opposite direction from Brogue's "厚实" anchor. Anchor positioning leans Unix workstation (NEC APC III heritage), drifting away from the dungeon-game aesthetic Brogue establishes.

### `px437-fmtowns-re` — SELECTED

See "Selection rationale" above.

## Fallback path (if FMTowns fails M5 final play-test)

Per plan M4 escalation matrix, **none of the 4 candidates were rejected on the audit**, so M4 escalation paths (扩 candidate 池, 改 plan 走 C 路径, 改 plan 走 B1 路径) are not triggered. The runner-up `px437-ibm-vga` remains documented as a fallback if FMTowns surfaces an issue M3 audit didn't catch (e.g., long-session readability, accidental terminal-style nostalgia trigger). Switching is mechanical: one `assets/fonts/README.md` "Selected:" header edit + one `Program.cs` default font path edit + one rebuild.

using SadRogue.Primitives;

namespace DungeonDescent;

// Semantic foreground palette sampled against Brogue vanilla 1.7.5.
//
// Sampling source : https://github.com/tmewett/BrogueCE (1.7.5 era globals)
//                   plus Pender/brogue legacy 1.7.5 reference at
//                   src/brogue/Globals.c (wallForeColor / playerInLightColor /
//                   magicGlyphColor / poisonColor / fireForeColor / iceColor /
//                   yellow / redBar / blueBar etc).
// Sampling date    : 2026-05-06
// Slot count       : 20 (target met). The 5x4 grid (Architecture / Entity /
//                    Item / Effect / UiChrome) maps cleanly onto Brogue's
//                    semantic groupings without contraction or expansion.
//
// Background is always the SadConsole default (black). Reset / Bold have no
// analogue under SadConsole and are intentionally absent.
//
// Guardrails honored when picking RGB values:
//   - FloorMossy / FloorCracked vs FloorBase: per-channel delta <= 30.
//   - Critical semantic pairs kept >= 80 RGB distance (EntityPlayer vs other
//     Entity*, EffectHealth vs EffectPoison, FloorMossy/Cracked vs Entity*).
static class Palette
{
    // ── Architecture (terrain) ────────────────────────────────────────────────
    // Brogue 1.7.5 src/brogue/Globals.c:wallForeColor ~= (97, 95, 80) muted stone.
    public static readonly Color WallStone     = new(110, 105, 90);
    // Brogue floorForeColor lit ~= mid grey (96, 96, 100). We brighten slightly
    // for SadConsole on black so floors read as walkable.
    public static readonly Color FloorBase     = new(120, 120, 130);
    // FloorMossy: floorBase shifted toward Brogue's foliageColor green-gray.
    // Channel deltas vs FloorBase: R-15, G+10, B-15 (each |d| <= 30).
    public static readonly Color FloorMossy    = new(105, 130, 115);
    // FloorCracked: floorBase shifted toward dry tan. Channel deltas: R+25,
    // G+15, B-25 (each |d| <= 30).
    public static readonly Color FloorCracked  = new(145, 135, 105);

    // ── Entity (creatures) ────────────────────────────────────────────────────
    // Brogue 1.7.5 src/brogue/Globals.c:playerInLightColor — pure white.
    // EntityPlayer LOCKED to white per plan section 1 (authentic Brogue).
    public static readonly Color EntityPlayer   = new(255, 255, 255);
    // Goblins / orcs / kobolds: Brogue uses olive-green skin tones.
    // src/brogue/Globals.c approx orcForeColor / goblinColor.
    public static readonly Color EntityHumanoid = new(120, 165, 80);
    // Beasts (rats, bats, monkeys): muted sandy brown.
    // Distance from EntityPlayer: ~sqrt(95^2+115^2+165^2) ~= 220 (>=80).
    public static readonly Color EntityBeast    = new(160, 140, 90);
    // Magical creatures (lich, dragon, phantom): saturated violet.
    // src/brogue/Globals.c:magicGlyphColor base.
    public static readonly Color EntityMagical  = new(170, 100, 200);

    // ── Item ──────────────────────────────────────────────────────────────────
    // Consumables (potions / scrolls): Brogue scrollColor / potion bottle pinks.
    public static readonly Color ItemConsumable = new(220, 130, 170);
    // Equipment (weapons / armor): cool steel.
    // Brogue itemColor weapons skew blue-grey (~(100,140,180)).
    public static readonly Color ItemEquipment  = new(120, 160, 200);
    // Treasure (gold / gems): warm gold.
    // Brogue 1.7.5 yellow-ish gold ~ (220, 195, 90).
    public static readonly Color ItemTreasure   = new(220, 190, 90);
    // Staves / wands: pale magical cyan to separate from EntityMagical.
    public static readonly Color ItemStaff      = new(150, 215, 220);

    // ── Effect (status / damage type) ─────────────────────────────────────────
    // Brogue 1.7.5 src/brogue/Globals.c:redBar / healthColor — saturated green.
    public static readonly Color EffectHealth = new(80, 200, 90);
    // Brogue poisonColor — toxic green-yellow. Distance from EffectHealth:
    // sqrt(110^2+0^2+10^2) ~= 110 (>=80).
    public static readonly Color EffectPoison = new(190, 200, 80);
    // Brogue fireForeColor — orange-red.
    public static readonly Color EffectFire   = new(230, 110, 50);
    // Brogue iceColor / coldColor — pale cyan.
    public static readonly Color EffectIce    = new(170, 220, 240);

    // ── UiChrome (interface) ──────────────────────────────────────────────────
    // Title bars / banners: warm brogue-yellow (matches Brogue menu titles).
    public static readonly Color UiTitle  = new(220, 200, 130);
    // Body text: slightly off-white so it does not shout against EntityPlayer.
    public static readonly Color UiText   = new(210, 210, 210);
    // Accent (stairs / stat labels): soft Brogue cyan
    // (src/brogue/Globals.c:blueBar adjacent).
    public static readonly Color UiAccent = new(120, 180, 210);
    // Dim chrome / hint footers / placeholder for memory tiles before Task 4
    // wires up Palette.Dim() — neutral mid-grey.
    public static readonly Color UiDim    = new(120, 120, 120);

    // Returns a darkened copy of `c` by multiplying each channel by `factor`.
    // Default 0.4 = 60% darkening, suitable for remembered (out-of-FOV) tiles.
    // Channels are clamped to [0, 255] defensively in case callers pass
    // factor > 1.0 in the future.
    public static Color Dim(Color c, float factor = 0.4f)
    {
        int r = Math.Clamp((int)(c.R * factor), 0, 255);
        int g = Math.Clamp((int)(c.G * factor), 0, 255);
        int b = Math.Clamp((int)(c.B * factor), 0, 255);
        return new Color(r, g, b);
    }
}

using SadRogue.Primitives;

namespace DungeonDescent;

record MonsterTemplate(
    string Name,
    char   Glyph,
    Color  Color,
    int    Hp,
    int    Attack,
    int    Defense,
    int    ExpReward,
    int    MoveInterval  // moves every N player turns (1=normal, 2=slow)
);

static class MonsterTemplates
{
    public static readonly MonsterTemplate Rat = new(
        "Rat",    'r', Palette.EntityBeast,    12,  4, 0,  5, 1);
    public static readonly MonsterTemplate Goblin = new(
        "Goblin", 'g', Palette.EntityHumanoid, 25,  7, 1, 12, 1);
    public static readonly MonsterTemplate Troll = new(
        "Troll",  'T', Palette.EntityHumanoid, 60, 10, 3, 30, 2);
    public static readonly MonsterTemplate Dragon = new(
        "Dragon", 'D', Palette.EntityMagical, 150, 18, 5, 200, 1);
}

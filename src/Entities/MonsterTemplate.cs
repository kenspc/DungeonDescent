namespace DungeonDescent;

record MonsterTemplate(
    string Name,
    char   Glyph,
    string Color,
    int    Hp,
    int    Attack,
    int    Defense,
    int    ExpReward,
    int    MoveInterval  // moves every N player turns (1=normal, 2=slow)
);

static class MonsterTemplates
{
    public static readonly MonsterTemplate Rat = new(
        "Rat",    'r', GameColors.Gray,    12,  4, 0,  5, 1);
    public static readonly MonsterTemplate Goblin = new(
        "Goblin", 'g', GameColors.Green,   25,  7, 1, 12, 1);
    public static readonly MonsterTemplate Troll = new(
        "Troll",  'T', GameColors.DarkRed, 60, 10, 3, 30, 2);
    public static readonly MonsterTemplate Dragon = new(
        "Dragon", 'D', GameColors.Red,    150, 18, 5, 200, 1);
}

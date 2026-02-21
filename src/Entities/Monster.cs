namespace DungeonDescent;

class Monster : Entity
{
    public int ExpReward    { get; init; }
    public int MoveInterval { get; init; }
    public int TurnCounter  { get; set; }

    public static Monster From(MonsterTemplate t, Point pos) => new()
    {
        Name         = t.Name,
        Glyph        = t.Glyph,
        Color        = t.Color,
        MaxHp        = t.Hp,
        Hp           = t.Hp,
        Attack       = t.Attack,
        Defense      = t.Defense,
        ExpReward    = t.ExpReward,
        MoveInterval = t.MoveInterval,
        Position     = pos,
    };
}

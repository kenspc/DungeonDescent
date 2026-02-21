namespace DungeonDescent;

abstract class Entity
{
    public Point Position { get; set; }
    public string Name    { get; init; } = "";
    public char   Glyph   { get; init; }
    public string Color   { get; init; } = GameColors.White;

    public int Hp    { get; set; }
    public int MaxHp { get; set; }
    public int Attack  { get; set; }
    public int Defense { get; set; }

    public bool IsAlive => Hp > 0;

    public int TakeDamage(int rawDamage)
    {
        int dmg = Math.Max(1, rawDamage - Defense);
        Hp = Math.Max(0, Hp - dmg);
        return dmg;
    }
}

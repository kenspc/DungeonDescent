namespace DungeonDescent;

class Item
{
    public string   Name     { get; set; } = "";
    public char     Glyph    { get; set; }
    public string   Color    { get; set; } = GameColors.White;
    public ItemType Type     { get; set; }

    public Func<Player, string> Apply { get; set; } = _ => "";

    public static Item Potion() => new()
    {
        Name  = "Health Potion",
        Glyph = '!',
        Color = GameColors.Magenta,
        Type  = ItemType.Potion,
        Apply = p =>
        {
            int healed = Math.Min(30, p.MaxHp - p.Hp);
            p.Hp += healed;
            return $"You drink the potion. Restored {healed} HP.";
        }
    };

    public static Item Sword() => new()
    {
        Name  = "Iron Sword",
        Glyph = '+',
        Color = GameColors.Cyan,
        Type  = ItemType.Sword,
        Apply = p =>
        {
            p.Attack += 3;
            return "You equip the sword. ATK +3.";
        }
    };

    public static Item Armor() => new()
    {
        Name  = "Leather Armor",
        Glyph = '[',
        Color = GameColors.Blue,
        Type  = ItemType.Armor,
        Apply = p =>
        {
            p.Defense += 2;
            return "You put on armor. DEF +2.";
        }
    };

    public static Item GoldPile(int amount) => new()
    {
        Name  = $"{amount} Gold",
        Glyph = '$',
        Color = GameColors.Yellow,
        Type  = ItemType.Gold,
        Apply = p =>
        {
            p.Gold  += amount;
            p.Score += amount;
            return $"You collect {amount} gold.";
        }
    };
}

class PositionedItem : Item
{
    public Point Position { get; }

    public PositionedItem(Item source, Point position)
    {
        Name     = source.Name;
        Glyph    = source.Glyph;
        Color    = source.Color;
        Type     = source.Type;
        Apply    = source.Apply;
        Position = position;
    }
}

namespace DungeonDescent;

class Player : Entity
{
    public int Level    { get; private set; } = 1;
    public int Exp      { get; private set; }
    public int ExpNext  => Level * 20;
    public int Gold     { get; set; }
    public int Score    { get; set; }

    public List<Item> Inventory { get; } = [];
    public const int MaxInventory = 10;

    public Player(Point start)
    {
        Name     = "Hero";
        Glyph    = '@';
        Color    = GameColors.Yellow;
        Position = start;
        MaxHp    = 100;
        Hp       = 100;
        Attack   = 8;
        Defense  = 2;
    }

    public string GainExp(int amount)
    {
        Exp += amount;
        if (Exp >= ExpNext)
        {
            Exp -= ExpNext;
            Level++;
            MaxHp   += 10;
            Hp       = Math.Min(Hp + 10, MaxHp);
            Attack  += 1;
            return $"Level up! Now level {Level}. HP +10, ATK +1.";
        }
        return "";
    }

    public bool TryPickup(Item item, out string msg)
    {
        if (Inventory.Count >= MaxInventory)
        {
            msg = "Inventory full!";
            return false;
        }
        Inventory.Add(item);
        msg = $"Picked up {item.Name}.";
        return true;
    }

    public string UseItem(int index)
    {
        if (index < 0 || index >= Inventory.Count) return "Invalid slot.";
        var item = Inventory[index];
        var result = item.Apply(this);
        Inventory.RemoveAt(index);
        return result;
    }
}

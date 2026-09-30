public class ShopSlot
{
    public ShopItemType Type { get; }
    public BlockDefinition Block { get; }
    public WeaponDefinition Weapon { get; }
    public PassiveItemDefinition Passive { get; }
    public int Price { get; }
    public int Value { get; }
    public bool IsSold { get; private set; }

    public ShopSlot(BlockDefinition block, int price, int value)
    {
        Type = ShopItemType.Block;
        Block = block; Price = price; Value = value;
    }

    public ShopSlot(WeaponDefinition weapon, int price)
    {
        Type = ShopItemType.Weapon;
        Weapon = weapon; Price = price;
    }

    public ShopSlot(PassiveItemDefinition passive, int price)
    {
        Type = ShopItemType.Passive;
        Passive = passive; Price = price;
    }

    public void MarkSold() => IsSold = true;
    public void SetSold(bool sold) => IsSold = sold;

    public string GetDisplayName() => Type switch
    {
        ShopItemType.Block => Block.hasValue ? $"{Block.displayText}{Value}" : Block.displayText,
        ShopItemType.Weapon => Weapon.weaponName,
        ShopItemType.Passive => Passive.itemName,
        _ => "???"
    };
}
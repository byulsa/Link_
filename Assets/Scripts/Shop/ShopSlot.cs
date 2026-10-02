using UnityEngine;
public class ShopSlot
{
    public ShopItemType Type { get; }
    public BlockDefinition Block { get; }
    public PassiveItemDefinition Passive { get; }
    public int Price { get; }
    public int Value { get; }
    public bool IsSold { get; private set; }

    public ShopSlot(BlockDefinition block, int price, int value = 0)
    {
        Type = block.targetWeapon != null ? ShopItemType.Weapon : ShopItemType.Block;
        Block = block;
        Price = price;
        Value = value;
    }

    public ShopSlot(PassiveItemDefinition passive, int price)
    {
        Type = ShopItemType.Passive;
        Passive = passive;
        Price = price;
    }

    public void MarkSold() => IsSold = true;
    public void SetSold(bool sold) => IsSold = sold;

    public string GetDisplayName() => Type switch
    {
        ShopItemType.Block or ShopItemType.Weapon =>
            Block.hasValue ? $"{Block.displayText}{Value}" : Block.displayText,
        ShopItemType.Passive => Passive.itemName,
        _ => "???"
    };
    public Sprite GetIcon()
    {
        return Type switch
        {
            ShopItemType.Block or ShopItemType.Weapon =>
                Block.icon,

            ShopItemType.Passive =>
                Passive.icon,

            _ => null
        };
    }
}
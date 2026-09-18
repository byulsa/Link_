public class ShopSlot
{
    public BlockDefinition Block { get; }
    public int Price { get; }
    public int Value { get; }
    public bool IsSold { get; private set; }

    public ShopSlot(BlockDefinition block, int price, int value)
    {
        Block = block;
        Price = price;
        Value = value;
    }

    public void MarkSold()
    {
        IsSold = true;
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;

public enum ItemRarity { Common, Rare, Epic, Legendary }

[Serializable]
public class ShopValueTier
{
    public int minValue = 2;
    public int maxValue = 2;
    public ItemRarity rarity = ItemRarity.Common;
    [Min(0)] public int price = 20;
}

[Serializable]
public class ShopItemSettings
{
    public bool available = true;
    public ItemRarity rarity = ItemRarity.Common;
    [Min(0)] public int basePrice = 100;
    [Min(0f)] public float selectionWeight = 1f;
    [Min(0)] public int pricePerValue;
    [Tooltip("Value blocks only: use a fixed rarity and price for each value range.")]
    public bool useValueTiers = false;
    public List<ShopValueTier> valueTiers = new()
    {
        new ShopValueTier { minValue = 2, maxValue = 2, rarity = ItemRarity.Common, price = 20 },
        new ShopValueTier { minValue = 3, maxValue = 4, rarity = ItemRarity.Rare, price = 50 },
        new ShopValueTier { minValue = 5, maxValue = int.MaxValue, rarity = ItemRarity.Epic, price = 150 }
    };
}

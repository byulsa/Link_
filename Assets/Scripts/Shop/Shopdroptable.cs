using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CodeEditor/Shop Drop Table", fileName = "ShopDropTable")]
public class ShopDropTable : ScriptableObject
{
    [Serializable]
    public class RarityWeight
    {
        public ItemRarity rarity;
        [Min(0f)] public float weight;
        public RarityWeight(ItemRarity rarity, float weight)
        { this.rarity = rarity; this.weight = weight; }
    }

    public ShopItemDatabase database;

    [Header("Top Slot Category Weights")]
    [Min(0f)] public float valueBlockWeight = 70f;
    [Min(0f)] public float codeBlockWeight = 20f;
    [Min(0f)] public float passiveWeight = 10f;
    public List<RarityWeight> rarityWeights = new()
    {
        new(ItemRarity.Common, 70f), new(ItemRarity.Rare, 25f),
        new(ItemRarity.Epic, 4f), new(ItemRarity.Legendary, 1f)
    };

    public struct ShopDropResult
    {
        public ItemRarity rarity;
        public ShopItemType type;
        public BlockDefinition block;
        public PassiveItemDefinition passive;
        public int value;
        public int price;
    }

    private struct Candidate
    {
        public BlockDefinition block;
        public PassiveItemDefinition passive;
        public ShopItemSettings settings;
        public ItemRarity rarity;
        public int minValue;
        public int maxValue;
        public bool fixedPrice;
        public int price;
        public ShopItemType type;
    }

    public bool TrySelectTopItem(ISet<PassiveItemDefinition> excludedPassives,
        out ShopDropResult result)
        => TrySelect(null, excludedPassives, out result, true);

    public bool TrySelectBlockOnly(out ShopDropResult result)
        => TrySelect(ShopItemType.Block, null, out result);
    public bool TrySelectWeaponOnly(out ShopDropResult result)
        => TrySelect(ShopItemType.Weapon, null, out result);
    public bool TrySelectPassiveOnly(ISet<PassiveItemDefinition> excludedPassives,
        out ShopDropResult result)
        => TrySelect(ShopItemType.Passive, excludedPassives, out result);
    public bool TrySelectRandomItem(out ShopDropResult result)
        => TrySelect(null, null, out result);

    private bool TrySelect(ShopItemType? type, ISet<PassiveItemDefinition> excluded,
        out ShopDropResult result, bool topOnly = false)
    {
        result = default;
        if (database == null) return false;
        var candidates = new List<Candidate>();
        var seenBlocks = new HashSet<BlockDefinition>();
        var seenPassives = new HashSet<PassiveItemDefinition>();
        foreach (BlockDefinition block in database.Blocks)
        {
            if (block == null || !seenBlocks.Add(block) || !Eligible(block.shop)) continue;
            ShopItemType blockType = block.targetWeapon != null
                ? ShopItemType.Weapon : ShopItemType.Block;
            if (topOnly && blockType == ShopItemType.Weapon) continue;
            if (type.HasValue && type.Value != blockType) continue;
            // Invalid ranges are excluded instead of silently producing unintended values.
            if (block.hasValue && block.minValue > block.maxValue) continue;
            if (block.hasValue && block.shop.useValueTiers)
            {
                if (!ValidTiers(block)) continue;
                foreach (ShopValueTier tier in block.shop.valueTiers)
                {
                    if (tier == null) continue;
                    int min = Math.Max(block.minValue, tier.minValue);
                    int max = Math.Min(block.maxValue, tier.maxValue);
                    if (min > max) continue;
                    candidates.Add(new Candidate
                    {
                        block = block, settings = block.shop, type = blockType,
                        rarity = tier.rarity, minValue = min, maxValue = max,
                        fixedPrice = true, price = tier.price
                    });
                }
            }
            else
                candidates.Add(new Candidate
                {
                    block = block, settings = block.shop, type = blockType,
                    rarity = block.shop.rarity,
                    minValue = block.minValue, maxValue = block.maxValue
                });
        }
        foreach (PassiveItemDefinition passive in database.Passives)
        {
            if (type.HasValue && type.Value != ShopItemType.Passive) break;
            if (passive == null || !seenPassives.Add(passive) || !Eligible(passive.shop)
                || (excluded != null && excluded.Contains(passive))) continue;
            candidates.Add(new Candidate
            { passive = passive, settings = passive.shop, type = ShopItemType.Passive, rarity = passive.shop.rarity });
        }

        // Aggregate duplicate rarity rows. Empty or excluded rarities carry no probability.
        var weights = new Dictionary<ItemRarity, double>();
        if (rarityWeights != null)
            foreach (RarityWeight row in rarityWeights)
                if (row != null && PositiveFinite(row.weight))
                {
                    weights.TryGetValue(row.rarity, out double previous);
                    weights[row.rarity] = previous + row.weight;
                }
        if (topOnly)
        {
            // Category probabilities must not depend on the number of items or rarity tiers.
            candidates.RemoveAll(c => !weights.TryGetValue(c.rarity, out double w) || w <= 0);
            bool[] hasCategory = new bool[3];
            foreach (Candidate c in candidates) hasCategory[TopCategory(c)] = true;
            float[] categoryWeights = { valueBlockWeight, codeBlockWeight, passiveWeight };
            double categoryTotal = 0;
            int selectedCategory = -1;
            for (int i = 0; i < categoryWeights.Length; i++)
                if (hasCategory[i] && PositiveFinite(categoryWeights[i]))
                {
                    categoryTotal += categoryWeights[i];
                    selectedCategory = i;
                }
            if (categoryTotal <= 0) return false;
            double categoryRoll = UnityEngine.Random.value * categoryTotal;
            for (int i = 0; i < categoryWeights.Length; i++)
            {
                if (!hasCategory[i] || !PositiveFinite(categoryWeights[i])) continue;
                categoryRoll -= categoryWeights[i];
                if (categoryRoll < 0) { selectedCategory = i; break; }
            }
            candidates.RemoveAll(c => TopCategory(c) != selectedCategory);
        }
        var available = new List<ItemRarity>();
        double total = 0;
        foreach (Candidate c in candidates)
            if (!available.Contains(c.rarity)
                && weights.TryGetValue(c.rarity, out double weight) && weight > 0)
            { available.Add(c.rarity); total += weight; }
        if (total <= 0) return false;

        double roll = UnityEngine.Random.value * total;
        ItemRarity rarity = available[available.Count - 1];
        foreach (ItemRarity option in available)
        {
            roll -= weights[option];
            if (roll < 0) { rarity = option; break; }
        }
        double itemTotal = 0;
        foreach (Candidate c in candidates)
            if (c.rarity == rarity) itemTotal += c.settings.selectionWeight;
        roll = UnityEngine.Random.value * itemTotal;
        Candidate selected = default;
        foreach (Candidate c in candidates)
        {
            if (c.rarity != rarity) continue;
            selected = c;
            roll -= c.settings.selectionWeight;
            if (roll < 0) break;
        }
        int value = 0;
        if (selected.block != null && selected.block.hasValue)
        {
            long span = (long)selected.maxValue - selected.minValue + 1;
            long offset = Math.Min(span - 1, (long)(UnityEngine.Random.value * (double)span));
            value = (int)(selected.minValue + offset);
        }
        long price = Math.Max(0, selected.fixedPrice ? selected.price : selected.settings.basePrice);
        if (!selected.fixedPrice && selected.block != null && selected.block.hasValue)
            price += ((long)value - selected.block.minValue)
                * Math.Max(0, selected.settings.pricePerValue);
        result = new ShopDropResult
        {
            type = selected.type, block = selected.block, passive = selected.passive,
            value = value, price = (int)Math.Min(int.MaxValue, price), rarity = selected.rarity
        };
        return true;
    }

    private static int TopCategory(Candidate candidate)
        => candidate.type == ShopItemType.Passive ? 2 : candidate.block.hasValue ? 0 : 1;

    // Reject ambiguous overlapping ranges instead of double-counting an item.
    private static bool ValidTiers(BlockDefinition block)
    {
        var tiers = block.shop.valueTiers;
        if (tiers == null) return false;
        for (int i = 0; i < tiers.Count; i++)
        {
            var a = tiers[i];
            if (a == null) continue;
            if (a.minValue > a.maxValue || a.price < 0) return false;
            for (int j = i + 1; j < tiers.Count; j++)
            {
                var b = tiers[j];
                if (b == null) continue;
                int min = Math.Max(block.minValue, Math.Max(a.minValue, b.minValue));
                int max = Math.Min(block.maxValue, Math.Min(a.maxValue, b.maxValue));
                if (min <= max) return false;
            }
        }
        return true;
    }

    private static bool Eligible(ShopItemSettings settings)
        => settings != null && settings.available && PositiveFinite(settings.selectionWeight);
    private static bool PositiveFinite(float value)
        => value > 0 && !float.IsNaN(value) && !float.IsInfinity(value);
}





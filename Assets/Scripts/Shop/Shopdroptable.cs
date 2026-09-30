using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "CodeEditor/Shop Drop Table",
    fileName = "ShopDropTable"
)]
public class ShopDropTable : ScriptableObject
{
    [System.Serializable]
    public class ValueOption
    {
        public int value = 1;

        [Min(0)]
        public int price = 100;

        [Min(0f)]
        public float weight = 1f;
    }

    [System.Serializable]
    public class SimpleEntry
    {
        public BlockDefinition block;

        [Min(0)]
        public int price = 100;

        [Min(0f)]
        public float weight = 1f;
    }
    [System.Serializable]
    public class WeaponEntry
    {
        public WeaponDefinition weapon;
        [Min(0)] public int price = 300;
        [Min(0f)] public float weight = 1f;
    }

    [System.Serializable]
    public class PassiveEntry
    {
        public PassiveItemDefinition passive;
        [Min(0)] public int price = 250;
        [Min(0f)] public float weight = 1f;
    }

    [System.Serializable]
    public class ValueEntry
    {
        public BlockDefinition block;
        public List<ValueOption> valueOptions = new();
    }

    public List<SimpleEntry> simpleEntries = new();
    public List<ValueEntry> valueEntries = new();
    public List<WeaponEntry> weaponEntries = new();
    public List<PassiveEntry> passiveEntries = new();
    public struct ShopDropResult
    {
        public ShopItemType type;
        public BlockDefinition block;
        public WeaponDefinition weapon;
        public PassiveItemDefinition passive;
        public int value;
        public int price;
    }

    public bool TrySelectRandomItem(out ShopDropResult result)
    {
        result = default;

        float simpleTotal = GetSimpleTotalWeight();
        float valueTotal = GetValueTotalWeight();
        float weaponTotal = GetWeaponTotalWeight();
        float passiveTotal = GetPassiveTotalWeight();
        float grandTotal = simpleTotal + valueTotal + weaponTotal + passiveTotal;

        if (grandTotal <= 0f) return false;

        float random = Random.Range(0f, grandTotal);

        if (random < simpleTotal)
        {
            SimpleEntry entry = SelectSimpleEntry(random);
            if (entry == null) return false;
            result.type = ShopItemType.Block;
            result.block = entry.block;
            result.price = entry.price;
            return true;
        }
        random -= simpleTotal;

        if (random < valueTotal)
        {
            ValueEntry ve = SelectValueEntry(random);
            if (ve == null) return false;
            ValueOption opt = SelectValueOption(ve);
            if (opt == null) return false;
            result.type = ShopItemType.Block;
            result.block = ve.block;
            result.value = opt.value;
            result.price = opt.price;
            return true;
        }
        random -= valueTotal;

        if (random < weaponTotal)
        {
            WeaponEntry we = SelectWeaponEntry(random);
            if (we == null) return false;
            result.type = ShopItemType.Weapon;
            result.weapon = we.weapon;
            result.price = we.price;
            return true;
        }
        random -= weaponTotal;

        PassiveEntry pe = SelectPassiveEntry(random);
        if (pe == null) return false;
        result.type = ShopItemType.Passive;
        result.passive = pe.passive;
        result.price = pe.price;
        return true;
    }

    private float GetSimpleTotalWeight()
    {
        float sum = 0f;

        foreach (SimpleEntry entry in simpleEntries)
        {
            if (entry == null || entry.block == null || entry.weight <= 0f)
                continue;

            sum += entry.weight;
        }

        return sum;
    }

    private float GetValueTotalWeight()
    {
        float sum = 0f;

        foreach (ValueEntry entry in valueEntries)
        {
            if (entry == null || entry.block == null)
                continue;

            sum += GetValueEntryWeight(entry);
        }

        return sum;
    }

    private float GetValueEntryWeight(ValueEntry entry)
    {
        float sum = 0f;

        foreach (ValueOption option in entry.valueOptions)
        {
            if (option == null || option.weight <= 0f)
                continue;

            sum += option.weight;
        }

        return sum;
    }

    private SimpleEntry SelectSimpleEntry(float random)
    {
        foreach (SimpleEntry entry in simpleEntries)
        {
            if (entry == null || entry.block == null || entry.weight <= 0f)
                continue;

            random -= entry.weight;

            if (random <= 0f)
                return entry;
        }

        return null;
    }

    private ValueEntry SelectValueEntry(float random)
    {
        foreach (ValueEntry entry in valueEntries)
        {
            if (entry == null || entry.block == null)
                continue;

            random -= GetValueEntryWeight(entry);

            if (random <= 0f)
                return entry;
        }

        return null;
    }

    private ValueOption SelectValueOption(ValueEntry entry)
    {
        float totalWeight = GetValueEntryWeight(entry);

        if (totalWeight <= 0f)
            return null;

        float random = Random.Range(0f, totalWeight);

        foreach (ValueOption option in entry.valueOptions)
        {
            if (option == null || option.weight <= 0f)
                continue;

            random -= option.weight;

            if (random <= 0f)
                return option;
        }

        return null;
    }
    private float GetWeaponTotalWeight()
    {
        float sum = 0f;
        foreach (var e in weaponEntries)
            if (e != null && e.weapon != null && e.weight > 0f) sum += e.weight;
        return sum;
    }

    private float GetPassiveTotalWeight()
    {
        float sum = 0f;
        foreach (var e in passiveEntries)
            if (e != null && e.passive != null && e.weight > 0f) sum += e.weight;
        return sum;
    }

    private WeaponEntry SelectWeaponEntry(float random)
    {
        foreach (var e in weaponEntries)
        {
            if (e == null || e.weapon == null || e.weight <= 0f) continue;
            random -= e.weight;
            if (random <= 0f) return e;
        }
        return null;
    }

    private PassiveEntry SelectPassiveEntry(float random)
    {
        foreach (var e in passiveEntries)
        {
            if (e == null || e.passive == null || e.weight <= 0f) continue;
            random -= e.weight;
            if (random <= 0f) return e;
        }
        return null;
    }
}
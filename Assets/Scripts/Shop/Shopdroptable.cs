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
    public class ValueEntry
    {
        public BlockDefinition block;
        public List<ValueOption> valueOptions = new();
    }

    public List<SimpleEntry> simpleEntries = new();
    public List<ValueEntry> valueEntries = new();

    public bool TrySelectRandomItem(
        out BlockDefinition block,
        out int value,
        out int price)
    {
        block = null;
        value = 0;
        price = 0;

        float simpleTotal = GetSimpleTotalWeight();
        float valueTotal = GetValueTotalWeight();
        float grandTotal = simpleTotal + valueTotal;

        if (grandTotal <= 0f)
            return false;
        
        float random = Random.Range(0f, grandTotal);

        if (random < simpleTotal)
        {
            SimpleEntry entry = SelectSimpleEntry(random);

            if (entry == null)
                return false;

            block = entry.block;
            price = entry.price;
            return true;
        }

        ValueEntry valueEntry = SelectValueEntry(random - simpleTotal);

        if (valueEntry == null)
            return false;
        
        ValueOption option = SelectValueOption(valueEntry);

        if (option == null)
            return false;
        
        block = valueEntry.block;
        value = option.value;
        price = option.price;
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
}
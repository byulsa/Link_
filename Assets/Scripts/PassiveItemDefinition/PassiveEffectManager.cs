using System.Collections.Generic;
using UnityEngine;

public class PassiveEffectManager : MonoBehaviour
{
    public static PassiveEffectManager Instance { get; private set; }

    private readonly List<PassiveItemDefinition> acquired = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Acquire(PassiveItemDefinition passive)
    {
        if (passive == null)
            return;
        acquired.Add(passive);
    }

    public float GetModifierBonus(BlockType modifierType)
    {
        float bonus = 0f;

        foreach (var p in acquired)
        {
            if (p == null || p.effectType != PassiveEffectType.ModifierBonus)
                continue;
            if (p.targetBlockType != modifierType)
                continue;
            bonus += p.bonusValue;
        }

        return bonus;
    }
}

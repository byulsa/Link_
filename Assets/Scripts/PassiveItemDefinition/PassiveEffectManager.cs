using System;
using System.Collections.Generic;
using UnityEngine;

public class PassiveEffectManager : MonoBehaviour
{
    public static PassiveEffectManager Instance { get; private set; }

    private readonly List<PassiveItemDefinition> acquired = new();
    public IReadOnlyList<PassiveItemDefinition> AcquiredPassives => acquired;
    public event Action OnPassivesChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Acquire(PassiveItemDefinition passive)
    {
        if (passive == null || acquired.Contains(passive))
            return;
        acquired.Add(passive);
        OnPassivesChanged?.Invoke();
    }

    public bool HasAcquired(PassiveItemDefinition passive)
    {
        return passive != null && acquired.Contains(passive);
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

    public float GetWeaponDamageBonus(WeaponDefinition weapon)
    {
        if (weapon == null)
            return 0f;

        float bonus = 0f;
        foreach (PassiveItemDefinition passive in acquired)
        {
            if (
                passive == null
                || passive.effectType != PassiveEffectType.WeaponDamageBonus
                || passive.targetWeapon != weapon
            )
                continue;

            bonus += passive.bonusValue;
        }

        return bonus;
    }

    public float GetPointRewardBonus() => GetBonus(PassiveEffectType.PointRewardBonus);

    public float GetBonus(PassiveEffectType effectType)
    {
        float bonus = 0f;
        foreach (PassiveItemDefinition passive in acquired)
        {
            if (passive != null && passive.effectType == effectType)
                bonus += passive.bonusValue;
        }

        return bonus;
    }
}

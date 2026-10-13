using UnityEngine;

public enum PassiveEffectType
{
    ModifierBonus,
    WeaponDamageBonus,
    PointRewardBonus
}

[CreateAssetMenu(menuName = "Shop/Passive Item Definition", fileName = "NewPassiveItem")]
public class PassiveItemDefinition : ScriptableObject
{
    public string itemName;

    [TextArea]
    public string description;
    public Sprite icon;

    [Header("Effect")]
    public PassiveEffectType effectType;
    public BlockType targetBlockType = BlockType.PLUS; // ModifierBonus일 때 대상
    public float bonusValue = 1f; // 예: PLUS 블록 +1
    public WeaponDefinition targetWeapon;

    public string GetFormattedDescription()
    {
        string value = effectType switch
        {
            PassiveEffectType.PointRewardBonus => $"{bonusValue * 100f:0.##}%",
            _ => $"{bonusValue:0.##}"
        };

        return description.Replace("[value]", $"<color=#FFD166>{value}</color>");
    }
}

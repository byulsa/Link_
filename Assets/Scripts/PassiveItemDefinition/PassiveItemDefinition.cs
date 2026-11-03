using UnityEngine;

public enum PassiveEffectType { ModifierBonus, WeaponDamageBonus, PointRewardBonus }

[CreateAssetMenu(menuName = "Shop/Passive Item Definition", fileName = "NewPassiveItem")]
public class PassiveItemDefinition : ScriptableObject
{
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;
    [Header("Effect")]
    public PassiveEffectType effectType;
    public BlockType targetBlockType = BlockType.PLUS;
    public float bonusValue = 1f;
    public WeaponDefinition targetWeapon;
    [Header("Shop")]
    public ShopItemSettings shop = new ShopItemSettings();

    public string GetFormattedDescription()
    {
        string value = effectType switch
        {
            PassiveEffectType.PointRewardBonus => $"{bonusValue * 100f:0.##}%",
            _ => $"{bonusValue:0.##}"
        };
        return (description ?? "").Replace("[value]", $"<color=#FFD166>{value}</color>");
    }
}

using UnityEngine;

[CreateAssetMenu(menuName = "CodeEditor/Block Definition", fileName = "BlockDefinition")]
public class BlockDefinition : ScriptableObject
{
    public BlockType blockType;
    public BlockCategory category;
    public Sprite icon;
    public string displayText;
    public string fullName;
    [TextArea(2, 5)] public string description;
    [Header("Value")]
    public bool hasValue;
    public int minValue = 1;
    public int maxValue = 1;
    [Header("Weapon Scope (WEAP trigger)")]
    public WeaponDefinition targetWeapon;
    [Header("Shop")]
    public ShopItemSettings shop = new ShopItemSettings();
}

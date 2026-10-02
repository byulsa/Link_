using UnityEngine;

[CreateAssetMenu(menuName = "CodeEditor/Block Definition", fileName = "BlockDefinition")]
public class BlockDefinition : ScriptableObject
{
    public BlockType blockType;
    public BlockCategory category;
    public Sprite icon;
    public string displayText;

    [Header("Value")]
    public bool hasValue;
    public int minValue = 1;
    public int maxValue = 1;

    [Header("Weapon Scope (WEAP 트리거 전용)")]
    public WeaponDefinition targetWeapon; // 비워두면 "모든 무기"(WEAP), 지정하면 그 무기만
}

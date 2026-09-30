using UnityEngine;

public enum PassiveEffectType
{
    ModifierBonus // 특정 연산자 블록(PLUS/MINUS/MULT/DIV)의 수치에 보너스를 더함
}

[CreateAssetMenu(menuName = "Shop/Passive Item Definition", fileName = "NewPassiveItem")]
public class PassiveItemDefinition : ScriptableObject
{
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Effect")]
    public PassiveEffectType effectType;
    public BlockType targetBlockType = BlockType.PLUS; // ModifierBonus일 때 대상
    public float bonusValue = 1f;                       // 예: PLUS 블록 +1
}
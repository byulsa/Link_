using UnityEngine;

[CreateAssetMenu(
    menuName = "CodeEditor/Block Definition",
    fileName = "BlockDefinition"
)]
public class BlockDefinition : ScriptableObject
{
    public BlockType blockType;
    public BlockCategory category;
    public string displayText;


    [Header("Value")]
    public bool hasValue;

    public int minValue = 1;
    public int maxValue = 1;
}
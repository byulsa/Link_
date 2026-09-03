using UnityEngine;

[CreateAssetMenu(menuName = "CodeEditor/Block Definition", fileName = "NewBlockDefinition")]
public class BlockDefinition : ScriptableObject
{
    public BlockType blockType;
    public BlockCategory category;
    public string displayText = "XXX";
    public float value = 0f;
}

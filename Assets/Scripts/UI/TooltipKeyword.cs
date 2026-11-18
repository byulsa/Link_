using UnityEngine;

[CreateAssetMenu(menuName = "LINK/Tooltip Keyword", fileName = "Tooltip Keyword")]
public sealed class TooltipKeyword : ScriptableObject
{
    public string keyword;
    [TextArea] public string description;
    public Sprite icon;
    public Color highlightColor = new Color32(255, 112, 128, 255);
}

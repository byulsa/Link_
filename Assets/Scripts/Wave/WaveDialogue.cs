using System;
using TMPro;
using UnityEngine;

[Flags]
public enum DialogueReward
{
    None = 0,
    UnlockCodingLog = 1 << 0,
    UnlockShop = 1 << 1,
    UnlockCodeCollection = 1 << 2
}

[CreateAssetMenu(fileName = "WaveDialogue", menuName = "Wave/Dialogue")]
public class WaveDialogue : ScriptableObject
{
    public Line[] lines;
    public DialogueReward completionReward;

    [Serializable]
    public class Line
    {
        public string speaker;
        [TextArea(2, 6)] public string text;
        public TMP_FontAsset font;
        [Min(0f)] public float fontSize;
        public Sprite portrait;
    }
}

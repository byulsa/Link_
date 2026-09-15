using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "CodeEditor/Code Drop Table",
    fileName = "CodeDropTable"
)]
public class CodeDropTable : ScriptableObject
{
    [System.Serializable]
    public class DropEntry
    {
        public BlockDefinition block;

        [Min(0f)]
        public float weight = 1f;
    }

    [Range(0f, 100f)]
    public float dropChance = 30f;

    public List<DropEntry> drops = new();
}
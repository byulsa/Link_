using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CodeDropTable))]
public class CodeDropTableEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        CodeDropTable table = (CodeDropTable)target;
        float total = 0f;
        if (table.drops == null) return;
        foreach (CodeDropTable.DropEntry entry in table.drops)
            if (entry != null && entry.block != null && entry.weight > 0f) total += entry.weight;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Actual Drop Probabilities", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("On drop = chance among successful drops. Per kill = includes the table drop chance (before the wave reward limit).", MessageType.Info);
        if (total <= 0f) { EditorGUILayout.LabelField("No eligible blocks."); return; }
        foreach (CodeDropTable.DropEntry entry in table.drops)
        {
            if (entry == null || entry.block == null || entry.weight <= 0f) continue;
            float chance = entry.weight / total;
            string range = entry.block.hasValue ? $" [{entry.block.minValue}–{entry.block.maxValue}]" : "";
            EditorGUILayout.LabelField(entry.block.name + range,
                $"On drop {chance:P2} / Per kill {chance * Mathf.Clamp01(table.dropChance / 100f):P2}");
        }
    }
}

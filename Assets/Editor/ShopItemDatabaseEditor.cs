using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ShopItemDatabase))]
public class ShopItemDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.HelpBox("Collects all saved item assets in Assets/. Disable Shop Available on items that should not be sold.", MessageType.Info);
        if (GUILayout.Button("Refresh Item List"))
        {
            serializedObject.Update();
            Fill("blocks", "t:BlockDefinition");
            Fill("passives", "t:PassiveItemDefinition");
            serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }
    }

    private void Fill(string propertyName, string filter)
    {
        string[] guids = AssetDatabase.FindAssets(filter, new[] { "Assets" });
        System.Array.Sort(guids, System.StringComparer.Ordinal);
        var assets = new System.Collections.Generic.List<Object>();
        var seen = new System.Collections.Generic.HashSet<Object>();
        foreach (string guid in guids)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)))
            {
                bool matches = propertyName == "blocks"
                    ? asset is BlockDefinition : asset is PassiveItemDefinition;
                if (matches && seen.Add(asset)) assets.Add(asset);
            }
        }
        SerializedProperty list = serializedObject.FindProperty(propertyName);
        list.arraySize = assets.Count;
        for (int i = 0; i < assets.Count; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = assets[i];
    }
}

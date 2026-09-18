using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "CodeEditor/Shop Drop Table",
    fileName = "ShopDropTable"
)]
public class ShopDropTable : ScriptableObject
{
    [System.Serializable]
    public class ShopEntry
    {
        public BlockDefinition block;

        [Min(0)]
        public int price = 100;

        [Min(0f)]
        public float weight = 1f;
    }

    public List<ShopEntry> entries = new();
}
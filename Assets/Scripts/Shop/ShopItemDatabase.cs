using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Item Database", fileName = "ShopItemDatabase")]
public class ShopItemDatabase : ScriptableObject
{
    [SerializeField] private List<BlockDefinition> blocks = new();
    [SerializeField] private List<PassiveItemDefinition> passives = new();
    public IReadOnlyList<BlockDefinition> Blocks => blocks;
    public IReadOnlyList<PassiveItemDefinition> Passives => passives;
}

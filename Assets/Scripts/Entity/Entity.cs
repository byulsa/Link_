using System.Collections.Generic;
using UnityEngine;

public enum EntityType
{
    Player,
    Enemy,
    Weapon,
}

public enum EntityTag
{
    Normal,
    Boss,
    Undead,
    Ground
}

public class Entity : MonoBehaviour
{
    [Header("Entity")]
    [SerializeField]
    private EntityType entityType;

    [SerializeField]
    private List<EntityTag> tags =
        new List<EntityTag>();

    public EntityType Type =>
        entityType;

    public IReadOnlyList<EntityTag> Tags =>
        tags;

    public bool Is(EntityType type)
    {
        return entityType == type;
    }

    public bool HasTag(EntityTag tag)
    {
        return tags.Contains(tag);
    }
}
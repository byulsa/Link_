using System.Collections.Generic;
using UnityEngine;

public class EntityManager : MonoBehaviour
{
    public static EntityManager Instance { get; private set; }

    private readonly List<Entity> entities =
        new List<Entity>();

    public IReadOnlyList<Entity> Entities =>
        entities;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void Register(Entity entity)
    {
        if (entity == null)
            return;

        if (entities.Contains(entity))
            return;

        entities.Add(entity);
    }

    public void Unregister(Entity entity)
    {
        if (entity == null)
            return;

        entities.Remove(entity);
    }

    public List<Entity> FindByType(
        EntityType type)
    {
        List<Entity> result =
            new List<Entity>();

        foreach (Entity entity in entities)
        {
            if (entity == null)
                continue;

            if (entity.Is(type))
            {
                result.Add(entity);
            }
        }

        return result;
    }

    public List<Entity> FindByTag(
        EntityTag tag)
    {
        List<Entity> result =
            new List<Entity>();

        foreach (Entity entity in entities)
        {
            if (entity == null)
                continue;

            if (entity.HasTag(tag))
            {
                result.Add(entity);
            }
        }

        return result;
    }

    public List<Entity> Find(
        EntityType type,
        EntityTag tag)
    {
        List<Entity> result =
            new List<Entity>();

        foreach (Entity entity in entities)
        {
            if (entity == null)
                continue;

            if (!entity.Is(type))
                continue;

            if (!entity.HasTag(tag))
                continue;

            result.Add(entity);
        }

        return result;
    }
}
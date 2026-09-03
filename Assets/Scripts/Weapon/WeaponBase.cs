using System.Collections.Generic;
using UnityEngine;

public class WeaponBase : MonoBehaviour
{
    [Header("Weapon")]
    [SerializeField] private WeaponDefinition definition;

    [Header("Owner")]
    [SerializeField] private Transform ownerTransform;

    private float damage;
    private float distance;
    private float rotationSpeed;
    private float size;

    private float currentAngle;

    public WeaponDefinition Definition => definition;
    public float Damage => damage;
    public float Distance => distance;
    public float RotationSpeed => rotationSpeed;
    public float Size => size;

    private void Awake()
    {
        ResetStats();
    }

    private void OnEnable()
    {
        CodeEditor.OnCodeRefreshed += HandleCodeRefreshed;
    }

    private void OnDisable()
    {
        CodeEditor.OnCodeRefreshed -= HandleCodeRefreshed;
    }

    private void HandleCodeRefreshed(IReadOnlyList<CodeChain> chains)
    {
        ResetStats();

        if (chains == null) return;

        foreach (CodeChain chain in chains)
        {
            if (chain == null || chain.nodes == null || chain.nodes.Count < 3) continue;

            if (chain.nodes[0].blockType != BlockType.WEAP) continue;

            BlockType action = chain.nodes[1].blockType;

            if (action == BlockType.SPD || action == BlockType.DST || action == BlockType.SZ)
            {
                float baseValue = GetBaseValue(action);
                float finalValue = CalculateValue(baseValue, chain, 2);
                SetStat(action, Mathf.Max(0f, finalValue));
            }
        }
    }

    private float GetBaseValue(BlockType action)
    {
        if (definition == null)
            return 0f;

        switch (action)
        {
            case BlockType.DMG:
                return definition.damage;

            case BlockType.SPD:
                return definition.rotationSpeed;

            case BlockType.DST:
                return definition.distance;

            case BlockType.SZ:
                return definition.size;

            default:
                return 0f;
        }
    }

    private float CalculateValue(float value, CodeChain chain, int startIndex)
    {
        for (int i = startIndex; i < chain.nodes.Count; i++)
        {
            CodeNode node = chain.nodes[i];
            if (node == null) continue;

            switch (node.blockType)
            {
                case BlockType.PLUS: value += node.value; break;
                case BlockType.MINUS: value -= node.value; break;
                case BlockType.MULT: value *= node.value; break;
                case BlockType.DIV: if (node.value != 0f) value /= node.value; break;
            }
        }
        return value;
    }

    public void ResetStats()
    {
        if (definition == null) return;

        damage = definition.damage;
        distance = definition.distance;
        rotationSpeed = definition.rotationSpeed;
        size = definition.size;

        transform.localScale = Vector3.one * (1f + (size * 0.1f));
    }

    public void SetStat(BlockType action, float value)
    {
        switch (action)
        {
            case BlockType.DMG:
                damage = value;
                break;

            case BlockType.SPD:
                rotationSpeed = value;
                break;

            case BlockType.DST:
                distance = value;
                break;

            case BlockType.SZ:
                size = value;
                transform.localScale = Vector3.one * (1f + (size * 0.1f));
                break;
        }
    }

    private void Update()
    {
        if (ownerTransform == null) return;

        currentAngle += rotationSpeed * Time.deltaTime;

        if (currentAngle >= 360f) currentAngle -= 360f;

        float radian = currentAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(radian), Mathf.Sin(radian), 0f) * distance;

        transform.position = ownerTransform.position + offset;
    }

    public void Hit(Entity target)
    {
        if (target == null || !target.Is(EntityType.Enemy))
            return;

        Enemy enemy = target.GetComponent<Enemy>();

        if (enemy == null)
            return;

        enemy.TakeDamage(damage);
    }
}
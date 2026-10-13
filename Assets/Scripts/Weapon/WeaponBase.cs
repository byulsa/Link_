using System.Collections.Generic;
using UnityEngine;

public class WeaponBase : MonoBehaviour
{
    [Header("Weapon")]
    [SerializeField]
    private WeaponDefinition definition;

    [Header("Owner")]
    [SerializeField]
    private Transform ownerTransform;

    private float damage;
    private float distance;
    private float rotationSpeed;
    private float size;

    private float currentAngle;

    public WeaponDefinition Definition => definition;
    public float Damage =>
        damage
        + (PassiveEffectManager.Instance != null
            ? PassiveEffectManager.Instance.GetWeaponDamageBonus(definition)
            : 0f);
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
        if (chains == null)
            return;

        foreach (CodeChain chain in chains)
        {
            if (!CodeChainEvaluator.TryGetWeaponActions(chain, out CodeNode actions))
                continue;

            WeaponDefinition scope = chain.FirstBlock?.Definition?.targetWeapon;
            if (scope != null && scope != definition)
                continue;

            int actionCount = CodeChainEvaluator.GetWeaponActionCount(actions);
            for (int i = 0; i < actionCount; i++)
            {
                BlockType action = CodeChainEvaluator.GetWeaponAction(actions, i);
                if (action == BlockType.DMG)
                    continue;

                float baseValue = CodeChainEvaluator.GetWeaponBaseValue(definition, action);
                float finalValue = CodeChainEvaluator.CalculateValue(baseValue, chain, 2);
                SetStat(action, Mathf.Max(0f, finalValue));
            }
        }
    }

    public void Initialize(WeaponDefinition newDefinition, Transform owner, float startAngle = 0f)
    {
        definition = newDefinition;
        ownerTransform = owner;
        currentAngle = startAngle;
        ResetStats();
    }

    public void ResetStats()
    {
        if (definition == null)
            return;

        damage = definition.damage;
        distance = definition.distance;
        rotationSpeed = definition.rotationSpeed;
        size = definition.size;

        transform.localScale = Vector3.one * (1f + (size * 0.1f));
    }

    public void SetDefinition(WeaponDefinition newDefinition)
    {
        definition = newDefinition;
        ResetStats();
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
        if (ownerTransform == null)
            return;

        currentAngle += rotationSpeed * Time.deltaTime;

        if (currentAngle >= 360f)
            currentAngle -= 360f;

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

        enemy.TakeDamage(Damage);
    }
}

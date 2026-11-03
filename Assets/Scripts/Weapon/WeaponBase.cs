using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class WeaponBase : MonoBehaviour
{
    [Header("Weapon")]
    [SerializeField]
    private WeaponDefinition definition;

    [Header("Owner")]
    [SerializeField]
    private Transform ownerTransform;

    private float currentRotationSpeed;
    private Tween rotationTween;
    private float rotatedAngleSinceResonance;
    private bool isDecelerating;
    private bool isAccelerating;
    private float rotationAccelerationDuration;
    private Ease rotationAccelerationEase;
    private float rotationDecelerationDuration;
    private Ease rotationDecelerationEase;
    private float decelerationDistanceMultiplier;

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
    public bool IsContinuousCut =>
        definition != null && definition.specialEffect == WeaponSpecialEffect.ContinuousCut;
    public float ContinuousCutInterval =>
        definition != null ? Mathf.Max(0.01f, definition.continuousCutInterval) : 0.25f;

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
        size = definition.size;

        UpdateRotationSpeed(definition.rotationSpeed);

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
                UpdateRotationSpeed(value);
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

        float deltaAngle = currentRotationSpeed * Time.deltaTime;
        currentAngle = Mathf.Repeat(currentAngle + deltaAngle, 360f);
        rotatedAngleSinceResonance += Mathf.Abs(deltaAngle);

        float remainingAngle = Mathf.Abs(rotationSpeed) - rotatedAngleSinceResonance;
        if (!isAccelerating
            && !isDecelerating
            && !Mathf.Approximately(rotationSpeed, 0f)
            && remainingAngle <= GetDecelerationAngle())
        {
            DecelerateForResonance();
        }

        float radian = currentAngle * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            Mathf.Cos(radian),
            Mathf.Sin(radian),
            0f
        ) * distance;

        transform.position = ownerTransform.position + offset;
    }
    private void UpdateRotationSpeed(float targetSpeed)
    {
        rotationTween?.Kill();

        rotationSpeed = targetSpeed;
        rotatedAngleSinceResonance = 0f;
        isDecelerating = false;
        isAccelerating = false;
        rotationAccelerationDuration = definition != null
            ? definition.rotationAccelerationDuration
            : 0.4f;
        rotationAccelerationEase = definition != null
            ? definition.rotationAccelerationEase
            : Ease.OutCubic;
        rotationDecelerationDuration = definition != null
            ? definition.rotationDecelerationDuration
            : 0.4f;
        rotationDecelerationEase = definition != null
            ? definition.rotationDecelerationEase
            : Ease.OutCubic;
        decelerationDistanceMultiplier = CalculateDecelerationDistanceMultiplier(rotationDecelerationEase);

        if (rotationAccelerationDuration <= 0f || Mathf.Approximately(targetSpeed, 0f))
        {
            currentRotationSpeed = targetSpeed;
            return;
        }

        isAccelerating = true;
        StartRotationTween(
            targetSpeed,
            rotationAccelerationDuration,
            rotationAccelerationEase,
            () => isAccelerating = false
        );
    }

    private void DecelerateForResonance()
    {
        isDecelerating = true;
        rotationTween?.Kill();

        if (rotationDecelerationDuration <= 0f)
        {
            CompleteResonanceDeceleration();
            return;
        }

        StartRotationTween(
            0f,
            rotationDecelerationDuration,
            rotationDecelerationEase,
            CompleteResonanceDeceleration
        );
    }

    private void CompleteResonanceDeceleration()
    {
        currentRotationSpeed = 0f;
        rotatedAngleSinceResonance = 0f;
        isDecelerating = false;

        if (rotationAccelerationDuration <= 0f)
        {
            currentRotationSpeed = rotationSpeed;
            return;
        }

        isAccelerating = true;
        StartRotationTween(
            rotationSpeed,
            rotationAccelerationDuration,
            rotationAccelerationEase,
            () => isAccelerating = false
        );
    }

    private float GetDecelerationAngle()
    {
        return Mathf.Abs(currentRotationSpeed)
            * rotationDecelerationDuration
            * decelerationDistanceMultiplier;
    }

    private static float CalculateDecelerationDistanceMultiplier(Ease ease)
    {
        const int steps = 32;
        float remainingSpeedFractionSum = 0f;
        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            float eased = DOVirtual.EasedValue(0f, 1f, t, ease);
            float weight = i == 0 || i == steps ? 0.5f : 1f;
            remainingSpeedFractionSum += (1f - eased) * weight;
        }

        return remainingSpeedFractionSum / steps;
    }

    private void StartRotationTween(float targetSpeed, float duration, Ease ease, TweenCallback onComplete = null)
    {
        rotationTween?.Kill();
        rotationTween = DOTween.To(
            () => currentRotationSpeed,
            value => currentRotationSpeed = value,
            targetSpeed,
            duration
        ).SetEase(ease)
         .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        if (onComplete != null)
            rotationTween.OnComplete(onComplete);
    }

    public void Hit(Entity target, bool applyBaseDamage = true)
    {
        if (target == null || !target.Is(EntityType.Enemy))
            return;

        Enemy enemy = target.GetComponent<Enemy>();
        if (enemy == null)
            enemy = target.GetComponentInParent<Enemy>();

        if (enemy == null)
            return;

        if (applyBaseDamage)
            enemy.TakeDamage(Damage);

        if (definition == null)
            return;

        switch (definition.specialEffect)
        {
            case WeaponSpecialEffect.Shockwave:
                ApplyShockwave(enemy);
                break;
            case WeaponSpecialEffect.ChainLightning:
                ApplyChainLightning(enemy);
                break;
        }
    }

    public bool TryBlockProjectile(Projectile projectile)
    {
        if (projectile == null
            || definition == null
            || definition.specialEffect != WeaponSpecialEffect.ProjectileDefense)
        {
            return false;
        }

        projectile.Block();
        return true;
    }

    private void ApplyShockwave(Enemy primaryTarget)
    {
        float radius = Mathf.Max(0f, definition.shockwaveRadius);
        if (radius <= 0f)
            return;

        HashSet<Enemy> damagedEnemies = new() { primaryTarget };
        Collider2D[] hits = Physics2D.OverlapCircleAll(primaryTarget.transform.position, radius);
        float splashDamage = Damage * Mathf.Max(0f, definition.shockwaveDamageMultiplier);

        foreach (Collider2D hit in hits)
        {
            Enemy enemy = hit.GetComponentInParent<Enemy>();
            if (enemy == null || !damagedEnemies.Add(enemy) || enemy.CurrentHealth <= 0f)
                continue;

            enemy.TakeDamage(splashDamage);
        }
    }

    private void ApplyChainLightning(Enemy primaryTarget)
    {
        int maxTargets = Mathf.Max(0, definition.chainLightningTargets);
        float radius = Mathf.Max(0f, definition.chainLightningRadius);
        if (maxTargets <= 0 || radius <= 0f)
            return;

        HashSet<Enemy> hitEnemies = new() { primaryTarget };
        Enemy previousTarget = primaryTarget;
        float damageMultiplier = Mathf.Clamp01(definition.chainLightningDamageMultiplier);
        float chainDamage = Damage * damageMultiplier;

        for (int i = 0; i < maxTargets; i++)
        {
            Enemy nextTarget = FindNearestEnemy(previousTarget.transform.position, radius, hitEnemies);
            if (nextTarget == null)
                break;

            hitEnemies.Add(nextTarget);
            nextTarget.TakeDamage(chainDamage);
            previousTarget = nextTarget;
            chainDamage *= damageMultiplier;
        }
    }

    private static Enemy FindNearestEnemy(Vector2 position, float radius, HashSet<Enemy> excluded)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, radius);
        Enemy nearestEnemy = null;
        float nearestDistanceSquared = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            Enemy enemy = hit.GetComponentInParent<Enemy>();
            if (enemy == null || enemy.CurrentHealth <= 0f || excluded.Contains(enemy))
                continue;

            float distanceSquared = ((Vector2)enemy.transform.position - position).sqrMagnitude;
            if (distanceSquared >= nearestDistanceSquared)
                continue;

            nearestEnemy = enemy;
            nearestDistanceSquared = distanceSquared;
        }

        return nearestEnemy;
    }
}

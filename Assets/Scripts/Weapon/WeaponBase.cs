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
    private BlockType? orbitModifier;

    public WeaponDefinition Definition => definition;
    public float Damage => ClampDamage(
        damage
        + (PassiveEffectManager.Instance != null
            ? PassiveEffectManager.Instance.GetWeaponDamageBonus(definition)
            : 0f));

    public static float ClampDamage(float value) => value >= 1f ? CombatStat.Round(value) : 1f;
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

    internal void HandleCodeRefreshed(IReadOnlyList<CodeChain> chains)
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

            orbitModifier = CodeChainEvaluator.GetOrbitModifier(chain) ?? orbitModifier;
            int actionCount = CodeChainEvaluator.GetWeaponActionCount(actions);
            for (int i = 0; i < actionCount; i++)
            {
                BlockType action = CodeChainEvaluator.GetWeaponAction(actions, i);
                if (action == BlockType.DMG)
                    continue;

                float baseValue = CodeChainEvaluator.GetWeaponBaseValue(definition, action);
                float finalValue = CodeChainEvaluator.CalculateValue(baseValue, chain, 2, action);
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
        orbitModifier = null;

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
                damage = CombatStat.Round(value);
                break;

            case BlockType.SPD:
                UpdateRotationSpeed(value);
                break;

            case BlockType.DST:
                distance = CombatStat.Round(value * 10f) / 10f;
                break;

            case BlockType.SZ:
                size = CombatStat.Round(value * 10f) / 10f;
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

        float cycleMultiplier = definition != null ? Mathf.Max(0.1f, definition.rotationCycleMultiplier) : 1f;
        float remainingAngle = Mathf.Abs(rotationSpeed) * cycleMultiplier - rotatedAngleSinceResonance;
        if (!isAccelerating
            && !isDecelerating
            && !Mathf.Approximately(rotationSpeed, 0f)
            && remainingAngle <= GetDecelerationAngle())
        {
            DecelerateForResonance();
        }

        float radian = currentAngle * Mathf.Deg2Rad;
        Vector2 orbit = orbitModifier switch
        {
            BlockType.SIN => new Vector2(Mathf.Sin(radian), 0.55f * Mathf.Sin(2f * radian)),
            BlockType.COS => new Vector2(Mathf.Cos(radian), 0.55f * Mathf.Sin(radian)),
            _ => new Vector2(Mathf.Cos(radian), Mathf.Sin(radian))
        };
        Vector3 offset = (Vector3)(orbit * distance);

        transform.position = ownerTransform.position + offset;
    }
    private void UpdateRotationSpeed(float targetSpeed)
    {
        rotationTween?.Kill();

        targetSpeed = CombatStat.Round(targetSpeed);
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
            case WeaponSpecialEffect.Bleed:
                enemy.ApplyBleed(definition.bleedMaxHealthFraction, definition.bleedDurationSeconds);
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

            enemy.TakeDamage(ClampDamage(splashDamage));
        }
    }
}

using System.Collections;
using UnityEngine;

public class RangedEnemy : Enemy
{
    [Header("Ranged Data")]
    [SerializeField]
    private RangedEnemyData rangedData;

    [Header("Animation")]
    [SerializeField]
    private Animator animator;

    [Header("Laser")]
    [SerializeField]
    private Transform laserFirePoint;

    private LineRenderer laserLineRenderer;
    private Material laserMaterial;
    private Coroutine laserVisualCoroutine;

    private Coroutine recoveryCoroutine;
    private bool hasFired;
    private float attackTimer;

    private bool isRetreating;
    private bool isAttacking;
    private bool isAttackRecovering;

    protected override void Awake()
    {
        base.Awake();

        if (rangedData != null)
        {
            InitializeFromData(rangedData);
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    public override void InitializeFromData(EnemyData data)
    {
        base.InitializeFromData(data);
        rangedData = data as RangedEnemyData;
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        attackTimer = 0f;
        hasFired = false;

        isRetreating = false;
        isAttacking = false;
        isAttackRecovering = false;
    }

    protected override void OnDisable()
    {
        if (recoveryCoroutine != null) StopCoroutine(recoveryCoroutine);
        recoveryCoroutine = null;
        base.OnDisable();

        if (laserVisualCoroutine != null)
        {
            StopCoroutine(laserVisualCoroutine);
            laserVisualCoroutine = null;
        }

        if (laserLineRenderer != null)
            laserLineRenderer.enabled = false;
    }

    private void OnDestroy()
    {
        if (laserMaterial != null)
            Destroy(laserMaterial);
    }

    protected override void Update()
    {
        if (IsDead)
            return;

        if (rangedData == null)
            return;

        if (IsKnockedBack)
            return;

        // 공격 또는 공격 후 Recovery 중
        if (isAttacking || isAttackRecovering)
        {
            StopMovement();
            return;
        }

        attackTimer -= Time.deltaTime;

        switch (rangedData.behavior)
        {
            case RangedBehavior.ApproachAndShoot:
                UpdateApproachAndShoot();
                break;

            case RangedBehavior.MaintainDistance:
                UpdateMaintainDistance();
                break;
        }
    }

    // -------------------------
    // Behavior
    // -------------------------

    private void UpdateApproachAndShoot()
    {
        float distance = PlayerDistance();

        if (distance > rangedData.attackRange)
        {
            MoveToPlayer();
        }
        else
        {
            StopMovement();
            TryAttack();
        }
    }

    private void UpdateMaintainDistance()
    {
        float distance = PlayerDistance();

        if (isRetreating)
        {
            if (distance >= rangedData.retreatEndRange)
            {
                isRetreating = false;
                StopMovement();
            }
            else
            {
                MoveAwayFromPlayer();
            }

            return;
        }

        if (distance <= rangedData.retreatRange)
        {
            isRetreating = true;

            MoveAwayFromPlayer();

            return;
        }

        if (distance > rangedData.attackRange)
        {
            MoveToPlayer();
        }
        else
        {
            StopMovement();
            TryAttack();
        }
    }

    // -------------------------
    // Attack
    // -------------------------

    private float PlayerDistance()
    {
        if (PlayerTransform == null)
            return Mathf.Infinity;

        return Vector2.Distance(transform.position, PlayerTransform.position);
    }

    private void TryAttack()
    {
        if (attackTimer > 0f)
            return;

        StartAttack();
    }

    private void StartAttack()
    {
        isAttacking = true;
        hasFired = false;

        StopMovement();
        FacePlayer();
        Debug.Log($"[RangedEnemy] {name} 공격 준비");

        if (animator != null)
        {
            animator.speed = StatMultiplier;
            animator.SetTrigger("Attack");
        }
        else
        {
            Debug.LogWarning($"[RangedEnemy] {name} Animator가 없습니다.");

            AnimationEvent_Fire();
            FinishAttack();
        }
    }

    // -------------------------
    // Animation Event
    // -------------------------

    public void AnimationEvent_Fire()
    {
        if (IsDead || !isAttacking || hasFired || rangedData == null) return;
        if (rangedData.attackType == RangedAttackType.Projectile) FireProjectile();
        else if (rangedData.attackType == RangedAttackType.Laser) FireLaser();
    }

    public void FireProjectile()
    {
        if (IsDead || !isAttacking || hasFired || rangedData == null ||
            rangedData.attackType != RangedAttackType.Projectile || PlayerTransform == null ||
            rangedData.projectilePrefab == null) return;
        Vector2 direction = (PlayerTransform.position - transform.position).normalized;
        if (direction == Vector2.zero) return;
        hasFired = true;
        int count = Mathf.Max(1, rangedData.projectileCount);
        for (int i = 0; i < count; i++)
        {
            float angle = rangedData.projectileAngles != null && i < rangedData.projectileAngles.Length
                ? rangedData.projectileAngles[i] : 0f;
            Vector2 shotDirection = Quaternion.Euler(0f, 0f, angle) * direction;
            GameObject shot = Instantiate(rangedData.projectilePrefab, GetLaserFirePosition(direction), Quaternion.identity);
            if (shot.TryGetComponent<Projectile>(out var projectile))
                projectile.Initialize(shotDirection, BaseDamage, PlayerTransform, StatMultiplier);
            else
            {
                Debug.LogError("[RangedEnemy] Projectile component is missing from projectilePrefab.", shot);
                Destroy(shot);
            }
        }
    }

    public void FireLaser()
    {
        if (IsDead || !isAttacking || hasFired || rangedData == null
            || rangedData.attackType != RangedAttackType.Laser
            || PlayerTransform == null)
            return;

        Vector2 direction = (PlayerTransform.position - transform.position).normalized;
        if (direction == Vector2.zero)
            return;

        hasFired = true;
        Vector3 start = GetLaserFirePosition(direction);
        float maxDistance = Mathf.Max(0.01f, rangedData.laserMaxDistance);
        Vector3 end = start + (Vector3)(direction * maxDistance);
        RaycastHit2D[] hits = Physics2D.RaycastAll(start, direction, maxDistance);
        RaycastHit2D nearestHit = default;
        float nearestHitDistance = float.MaxValue;

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null
                || hit.collider.transform == transform
                || hit.collider.transform.IsChildOf(transform))
                continue;

            if (hit.distance >= nearestHitDistance)
                continue;

            nearestHitDistance = hit.distance;
            nearestHit = hit;
            end = hit.point;
        }

        if (nearestHit.collider != null)
        {
            Entity hitEntity = nearestHit.collider.GetComponentInParent<Entity>();
            if (hitEntity != null && hitEntity.Is(EntityType.Player))
            {
                IDamageable damageable = nearestHit.collider.GetComponentInParent<IDamageable>();
                damageable?.TakeDamage(BaseDamage * Mathf.Max(0f, rangedData.laserDamageMultiplier));
            }
        }

        ShowLaser(start, end);
    }

    private Vector3 GetLaserFirePosition(Vector2 direction)
    {
        if (laserFirePoint == null)
            return transform.position + new Vector3(direction.x >= 0f ? 0.45f : -0.45f, 0.15f, 0f);

        Vector3 localPosition = laserFirePoint.localPosition;
        localPosition.x = Mathf.Abs(localPosition.x) * (direction.x >= 0f ? 1f : -1f);
        laserFirePoint.localPosition = localPosition;
        return laserFirePoint.position;
    }

    private void ShowLaser(Vector3 start, Vector3 end)
    {
        if (!EnsureLaserLineRenderer())
            return;

        if (laserVisualCoroutine != null)
            StopCoroutine(laserVisualCoroutine);

        laserLineRenderer.startWidth = rangedData.laserWidth;
        laserLineRenderer.endWidth = rangedData.laserWidth * 0.35f;
        laserLineRenderer.startColor = rangedData.laserColor;
        Color endColor = rangedData.laserColor;
        endColor.a = 0f;
        laserLineRenderer.endColor = endColor;
        laserLineRenderer.SetPosition(0, start);
        laserLineRenderer.SetPosition(1, end);
        laserLineRenderer.enabled = true;

        laserVisualCoroutine = StartCoroutine(HideLaserAfterDelay(rangedData.laserDuration));
    }

    private bool EnsureLaserLineRenderer()
    {
        if (laserLineRenderer != null)
            return true;

        GameObject laserObject = new("LaserLine");
        laserObject.transform.SetParent(transform, false);
        laserLineRenderer = laserObject.AddComponent<LineRenderer>();
        laserLineRenderer.positionCount = 2;
        laserLineRenderer.useWorldSpace = true;
        laserLineRenderer.alignment = LineAlignment.View;
        laserLineRenderer.numCapVertices = 4;
        laserLineRenderer.sortingOrder = 10;
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            laserLineRenderer.sortingLayerID = spriteRenderer.sortingLayerID;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogError("[RangedEnemy] 레이저 LineRenderer용 Sprites/Default 셰이더를 찾을 수 없습니다.", this);
            Destroy(laserObject);
            laserLineRenderer = null;
            return false;
        }

        laserMaterial = new Material(shader);
        laserLineRenderer.material = laserMaterial;
        return true;
    }

    private IEnumerator HideLaserAfterDelay(float duration)
    {
        yield return new WaitForSeconds(duration);

        if (laserLineRenderer != null)
            laserLineRenderer.enabled = false;

        laserVisualCoroutine = null;
    }

    // Animation 마지막 프레임
    public void FinishAttack()
    {
        if (IsDead || !isAttacking) return;
        if (animator != null) animator.speed = 1f;
        isAttacking = false;

        StartAttackRecovery();
    }

    // -------------------------
    // Attack Recovery
    // -------------------------

    private void StartAttackRecovery()
    {
        if (rangedData.attackRecoveryTime <= 0f)
        {
            attackTimer = ScaleAttackDuration(rangedData.attackCooldown);

            return;
        }

        recoveryCoroutine = StartCoroutine(AttackRecoveryRoutine());
    }

    private IEnumerator AttackRecoveryRoutine()
    {
        isAttackRecovering = true;

        StopMovement();

        yield return new WaitForSeconds(ScaleAttackDuration(rangedData.attackRecoveryTime));

        isAttackRecovering = false;
        recoveryCoroutine = null;

        attackTimer = ScaleAttackDuration(rangedData.attackCooldown);
    }
}

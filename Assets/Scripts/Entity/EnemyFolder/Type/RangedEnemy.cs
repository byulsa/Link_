using UnityEngine;

public class RangedEnemy : Enemy
{
    [Header("Ranged Data")]
    [SerializeField] private RangedEnemyData rangedData;

    private float attackTimer;
    private bool isRetreating;

    protected override void Awake()
    {
        base.Awake();

        if (rangedData != null)
        {
            InitializeFromData(rangedData);
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        attackTimer = 0f;
        isRetreating = false;
    }

    protected override void Update()
    {
        if (IsDead)
            return;

        if (rangedData == null)
            return;

        if (IsKnockedBack)
            return;

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

    private float PlayerDistance()
    {
        if (PlayerTransform == null)
            return Mathf.Infinity;

        return Vector2.Distance(
            transform.position,
            PlayerTransform.position
        );
    }

    private void MoveAwayFromPlayer()
    {
        if (PlayerTransform == null)
            return;

        Vector2 direction =
            (transform.position - PlayerTransform.position).normalized;

        if (direction == Vector2.zero)
            return;

        // 현재 Enemy의 이동 방식과 맞춰서 처리
        // MoveToPlayer()와 반대 방향
        GetComponent<Rigidbody2D>().linearVelocity =
            direction * GetMoveSpeed();
    }

    private void TryAttack()
    {
        if (attackTimer > 0f)
            return;

        Attack();

        attackTimer = rangedData.attackCooldown;
    }

    private void Attack()
    {
        Debug.Log(
            $"[RangedEnemy] {name} 공격! / Type: {rangedData.attackType}"
        );

        if (rangedData.attackType != RangedAttackType.Projectile)
            return;

        FireProjectile();
    }
    private void FireProjectile()
    {
        if (rangedData.projectilePrefab == null)
        {
            Debug.LogWarning(
                $"[RangedEnemy] {name}의 Projectile Prefab이 없습니다."
            );
            return;
        }

        if (PlayerTransform == null)
            return;

        Vector2 baseDirection =
            (PlayerTransform.position - transform.position).normalized;

        int count = rangedData.projectileCount;

        if (count <= 0)
            return;

        for (int i = 0; i < count; i++)
        {
            float angle = 0f;

            if (rangedData.projectileAngles != null &&
                i < rangedData.projectileAngles.Length)
            {
                angle = rangedData.projectileAngles[i];
            }

            Vector2 direction =
                Quaternion.Euler(0f, 0f, angle) * baseDirection;

            GameObject projectileObj =
                Instantiate(
                    rangedData.projectilePrefab,
                    transform.position,
                    Quaternion.identity
                );

            Projectile projectile =
                projectileObj.GetComponent<Projectile>();

            if (projectile == null)
            {
                Debug.LogWarning(
                    "[RangedEnemy] Projectile 컴포넌트가 없습니다."
                );

                Destroy(projectileObj);
                continue;
            }

            projectile.Initialize(
                direction,
                BaseDamage,
                PlayerTransform
            );
        }
    }

    private float GetMoveSpeed()
    {
        // 현재 Enemy에서 moveSpeed가 private이므로
        // 나중에 protected 프로퍼티로 노출하는 게 좋음.
        return 3f;
    }
}
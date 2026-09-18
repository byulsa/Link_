using UnityEngine;
using System.Collections;

public class RangedEnemy : Enemy
{
    [Header("Ranged Data")]
    [SerializeField] private RangedEnemyData rangedData;

    [Header("Animation")]
    [SerializeField] private Animator animator;

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

    protected override void OnEnable()
    {
        base.OnEnable();

        attackTimer = 0f;

        isRetreating = false;
        isAttacking = false;
        isAttackRecovering = false;
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

        return Vector2.Distance(
            transform.position,
            PlayerTransform.position
        );
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

        StopMovement();
        FacePlayer();
        Debug.Log(
            $"[RangedEnemy] {name} 공격 준비"
        );

        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        else
        {
            Debug.LogWarning(
                $"[RangedEnemy] {name} Animator가 없습니다."
            );

            FinishAttack();
        }
    }

    // -------------------------
    // Animation Event
    // -------------------------

    public void FireLaser()
    {
        if (PlayerTransform == null)
            return;

        Debug.Log(
            $"[RangedEnemy] {name} 레이저 발사!"
        );

        // TODO:
        // 실제 레이저 판정
    }

    // Animation 마지막 프레임
    public void FinishAttack()
    {
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
            attackTimer =
                rangedData.attackCooldown;

            return;
        }

        StartCoroutine(
            AttackRecoveryRoutine()
        );
    }

    private IEnumerator AttackRecoveryRoutine()
    {
        isAttackRecovering = true;

        StopMovement();

        yield return new WaitForSeconds(
            rangedData.attackRecoveryTime
        );

        isAttackRecovering = false;

        attackTimer =
            rangedData.attackCooldown;
    }
}
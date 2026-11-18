using UnityEngine;

public class MeleeEnemy : Enemy
{
    [Header("Melee Data")]
    [SerializeField]
    private MeleeEnemyData meleeData;
    [SerializeField] protected Animator animator;
    private bool isAttacking;
    protected bool IsAttacking => isAttacking;
    private float attackTimer;

    protected override void Awake()
    {
        base.Awake();

        if (meleeData != null)
        {
            InitializeFromData(meleeData);
        }
    }

    public override void InitializeFromData(EnemyData data)
    {
        base.InitializeFromData(data);
        meleeData = data as MeleeEnemyData;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        isAttacking = false;
        attackTimer = 0f;
    }

    protected override void Update()
    {
        if (IsDead || meleeData == null)
            return;

        if (IsKnockedBack)
            return;

        attackTimer -= Time.deltaTime;

        if (isAttacking)
        {
            StopMovement();
            return;
        }

        if (PlayerDistance() <= meleeData.attackRange)
        {
            StopMovement();
            TryAttack();
        }
        else
        {
            MoveToPlayer();
        }
    }

    private float PlayerDistance()
    {
        if (PlayerTransform == null)
            return Mathf.Infinity;

        return Vector2.Distance(transform.position, PlayerTransform.position);
    }

    private void TryAttack()
    {
        if (attackTimer > 0f || isAttacking)
            return;

        isAttacking = true;
        attackTimer = meleeData.attackCooldown;
        StopMovement();
        animator.SetTrigger("Attack");
    }

    public void AnimationEvent_Attack()
    {
        if (IsDead || !isAttacking || PlayerTransform == null)
            return;

        GameplaySfx.Play(GameplaySound.MeleeAttack);

        if (PlayerDistance() > meleeData.attackRange)
            return;

        HitPlayer(PlayerTarget, BaseDamage);
    }

    protected virtual void HitPlayer(Player player, float damage)
    {
        player.TakeDamage(damage);
    }

    protected void CancelMeleeAttack()
    {
        isAttacking = false;
        animator.ResetTrigger("Attack");
    }
    public void AnimationEvent_AttackEnd()
    {
        if (IsDead) return;
        if (animator != null) animator.speed = 1f;
        isAttacking = false;
    }
}

using UnityEngine;

public class MeleeEnemy : Enemy
{
    [Header("Melee Data")]
    [SerializeField] private MeleeEnemyData meleeData;

    private float attackTimer;

    protected override void Awake()
    {
        base.Awake();

        if (meleeData != null)
        {
            InitializeFromData(meleeData);
        }
    }

    protected override void Update()
    {
        if (IsDead)
            return;

        if (meleeData == null)
            return;

        if (IsKnockedBack)
            return;

        attackTimer -= Time.deltaTime;

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

        return Vector2.Distance(
            transform.position,
            PlayerTransform.position
        );
    }

    private void TryAttack()
    {
        if (attackTimer > 0f)
            return;

        Attack();

        attackTimer = meleeData.attackCooldown;
    }

    private void Attack()
    {
        Debug.Log($"[MeleeEnemy] {name} 공격!");

        if (PlayerTransform == null)
            return;

        IDamageable damageable =
            PlayerTransform.GetComponent<IDamageable>();

        if (damageable == null)
            return;

        damageable.TakeDamage(BaseDamage);
    }
}
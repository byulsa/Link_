using UnityEngine;
using System.Collections;
using System;

enum EnmeyTrigger
{
    Alive,
    Dead
}

public enum DeathType
{
    Normal,
    SelfDestruct
}

public abstract class Enemy : MonoBehaviour, IDamageable, IPointDrop
{
    [Header("Data Reference")]
    [SerializeField] private EnemyData enemyData;
    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Movement")]
    [SerializeField] private string playerTag = "Player";

    [Header("Knockback Settings")]
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackDuration = 0.2f;
    [SerializeField] private int pointReward = 10;

    public static event Action<Enemy, DeathType> OnEnemyDeath;

    private float currentHealth;
    private float maxHealth;
    private float baseDamage;
    private float moveSpeed;

    private Transform playerTransform;
    protected Transform PlayerTransform => playerTransform;

    private Rigidbody2D rb;

    private bool isKnockedBack;
    protected bool IsKnockedBack => isKnockedBack;

    private bool isDead;
    protected bool IsDead => isDead;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float BaseDamage => baseDamage;

    [SerializeField] private CodeDropTable codeDropTable;
    public CodeDropTable CodeDropTable => codeDropTable;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null)
            //spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (enemyData != null)
            {
                InitializeFromData(enemyData);
            }
    }

    protected virtual void OnEnable()
    {
        currentHealth = maxHealth;

        isKnockedBack = false;
        isDead = false;

        if (playerTransform == null)
        {
            GameObject playerObj =
                GameObject.FindGameObjectWithTag(playerTag);

            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }
    }

    public void InitializeFromData(EnemyData data)
    {
        enemyData = data;

        maxHealth = data.maxHealth;
        baseDamage = data.baseDamage;
        moveSpeed = data.moveSpeed;

        currentHealth = maxHealth;

        knockbackForce = data.knockbackForce;
        knockbackDuration = data.knockbackDuration;

        pointReward = data.PointDropNum;
    }

    // -------------------------
    // Damage
    // -------------------------

    public void TakeDamage(float damage)
    {
        Vector2 knockbackDir = Vector2.zero;

        if (playerTransform != null)
        {
            knockbackDir =
                (transform.position - playerTransform.position)
                .normalized;
        }

        TakeDamage(damage, knockbackDir);
    }

    public virtual void TakeDamage(
        float damage,
        Vector2 hitDirection
    )
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0f, currentHealth);

        Debug.Log(
            $"[Enemy] 피해: {damage} " +
            $"HP: {currentHealth}/{maxHealth}"
        );

        if (hitDirection != Vector2.zero)
        {
            ApplyKnockback(hitDirection);
        }

        if (currentHealth <= 0f)
        {
            Debug.Log(
                "[Enemy] currentHealth <= 0, Die() 호출 준비"
            );

            Die();
        }
    }
    protected void UpdateFacing(Vector2 movementDirection)
    {
        if (spriteRenderer == null)
            return;

        if (movementDirection.x > 0.01f)
        {
            // 오른쪽 이동
            spriteRenderer.flipX = true;
        }
        else if (movementDirection.x < -0.01f)
        {
            // 왼쪽 이동
            spriteRenderer.flipX = false;
        }
    }
    protected void FacePlayer()
    {
        if (playerTransform == null || spriteRenderer == null)
            return;

        float directionX =
            playerTransform.position.x - transform.position.x;

        if (directionX > 0f)
        {
            // 플레이어가 오른쪽
            spriteRenderer.flipX = true;
        }
        else if (directionX < 0f)
        {
            // 플레이어가 왼쪽
            spriteRenderer.flipX = false;
        }
    }

    // -------------------------
    // Knockback
    // -------------------------

    private Coroutine coroutineKnockback;

    private void ApplyKnockback(Vector2 direction)
    {
        if (coroutineKnockback != null)
        {
            StopCoroutine(coroutineKnockback);
        }

        coroutineKnockback =
            StartCoroutine(
                KnockbackRoutine(direction)
            );
    }

    private IEnumerator KnockbackRoutine(
        Vector2 direction
    )
    {
        isKnockedBack = true;

        Vector2 dir = direction.normalized;
        float t = 0f;

        while (t < knockbackDuration)
        {
            float ratio =
                1f - (t / knockbackDuration);

            if (rb != null)
            {
                rb.linearVelocity =
                    dir * knockbackForce * ratio;
            }

            t += Time.fixedDeltaTime;

            yield return new WaitForFixedUpdate();
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        isKnockedBack = false;
    }

    // -------------------------
    // Movement
    // -------------------------

    protected virtual void Update()
    {
        if (!isKnockedBack)
        {
            MoveToPlayer();
        }
    }

    protected void MoveToPlayer()
    {
        if (playerTransform == null || rb == null)
            return;

        Vector2 direction =
            (
                playerTransform.position
                - transform.position
            ).normalized;
        UpdateFacing(direction);
        rb.linearVelocity =
            direction * moveSpeed;
    }

    protected void MoveAwayFromPlayer()
    {
        if (playerTransform == null || rb == null)
            return;

        Vector2 direction =
            (
                transform.position
                - playerTransform.position
            ).normalized;
        UpdateFacing(direction);
        rb.linearVelocity =
            direction * moveSpeed;
    }

    protected void StopMovement()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    // -------------------------
    // Attack Recovery
    // -------------------------

    protected IEnumerator AttackRecoveryRoutine(float recoveryTime)
    {
        if (recoveryTime <= 0f)
            yield break;

        StopMovement();

        yield return new WaitForSeconds(
            recoveryTime
        );
    }

    // -------------------------
    // Death
    // -------------------------

    protected virtual void Die(
        DeathType deathType = DeathType.Normal
    )
    {
        if (isDead)
            return;

        isDead = true;

        Debug.Log(
            $"[Enemy] {name} 사망 / DeathType: {deathType}"
        );

        try
        {
            OnEnemyDeath?.Invoke(
                this,
                deathType
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                $"[Enemy] OnEnemyDeath 예외: {ex.Message}"
            );

            Debug.LogError(ex.StackTrace);
        }

        gameObject.SetActive(false);
    }

    public int GetPointAmount()
    {
        return pointReward;
    }
}
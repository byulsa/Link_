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
            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null) playerTransform = playerObj.transform;
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

    // 기본 TakeDamage (방향 정보가 없을 때는 플레이어 반대 방향으로 넉백)
    public void TakeDamage(float damage)
    {
        Vector2 knockbackDir = Vector2.zero;
        if (playerTransform != null)
        {
            knockbackDir = (transform.position - playerTransform.position).normalized;
        }

        TakeDamage(damage, knockbackDir);
    }

    // 넉백 방향을 직접 전달받는 TakeDamage Overload
    public virtual void TakeDamage(float damage, Vector2 hitDirection)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0f, currentHealth);

        Debug.Log($"[Enemy] 피해: {damage} HP: {currentHealth}/{maxHealth}");

        // 넉백 처리
        if (hitDirection != Vector2.zero)
        {
            ApplyKnockback(hitDirection);
        }

        if (currentHealth <= 0f)
        {
            Debug.Log($"[Enemy] currentHealth <= 0, Die() 호출 준비");
            Die();
        }
    }

    private void ApplyKnockback(Vector2 direction)
    {
        if (coroutineKnockback != null)
        {
            StopCoroutine(coroutineKnockback);
        }

        coroutineKnockback = StartCoroutine(KnockbackRoutine(direction));
    }

    private Coroutine coroutineKnockback;

    private IEnumerator KnockbackRoutine(Vector2 direction)
    {
        isKnockedBack = true;

        Vector2 dir = direction.normalized;
        float t = 0f;

        while (t < knockbackDuration)
        {
            float ratio = 1f - (t / knockbackDuration);
            rb.linearVelocity = dir * knockbackForce * ratio;

            t += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity = Vector2.zero;
        isKnockedBack = false;
    }

    protected virtual void Update()
    {
        // 넉백 중일 때는 플레이어를 향해 자력 이동하지 않음
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
            (playerTransform.position - transform.position).normalized;

        rb.linearVelocity = direction * moveSpeed;
    }
    protected void StopMovement()
    {
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    protected virtual void Die(DeathType deathType = DeathType.Normal)
    {
        if (isDead)
        {
            // Debug.Log(
            //     "[Enemy] 이미 사망 처리됨, Die() 중복 호출 무시"
            // );

            return;
        }

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
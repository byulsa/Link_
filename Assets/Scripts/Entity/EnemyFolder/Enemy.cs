using UnityEngine;
using System.Collections;
using System;
enum EnmeyTrigger
{
    Alive,
    Dead
}
public class Enemy : MonoBehaviour, IDamageable, IPointDrop
{
    [Header("Data Reference")]
    [SerializeField] private EnemyData enemyData;

    [Header("Movement")]
    [SerializeField] private string playerTag = "Player";

    [Header("Knockback Settings")]
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackDuration = 0.2f;
    [SerializeField] private int pointReward = 10;
    public static event Action<Enemy> OnEnemyDeath;

    private float currentHealth;
    private float maxHealth;
    private float baseDamage;
    private float moveSpeed;

    private Transform playerTransform;
    private Rigidbody2D rb;
    private bool isKnockedBack;
    private bool isDead;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float BaseDamage => baseDamage;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (enemyData != null)
        {
            InitializeFromData(enemyData);
        }
    }

    private void OnEnable()
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
    public void TakeDamage(float damage, Vector2 hitDirection)
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

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(direction.normalized * knockbackForce, ForceMode2D.Impulse);
        }

        yield return new WaitForSeconds(knockbackDuration);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        isKnockedBack = false;
    }

    private void Update()
    {
        // 넉백 중일 때는 플레이어를 향해 자력 이동하지 않음
        if (!isKnockedBack)
        {
            MoveToPlayer();
        }
    }

    private void MoveToPlayer()
    {
        if (playerTransform == null) return;

        Vector2 direction = (playerTransform.position - transform.position).normalized;

        if (rb != null)
        {
            rb.MovePosition(rb.position + direction * moveSpeed * Time.deltaTime);
        }
        else
        {
            transform.position += (Vector3)direction * moveSpeed * Time.deltaTime;
        }
    }

    private void Die()
    {
        // 같은 프레임에 여러 트리거가 겹쳐서 TakeDamage가
        // 중복 호출돼도 Die()가 두 번 실행되지 않도록 방지
        if (isDead)
        {
            Debug.Log("[Enemy] 이미 사망 처리됨, Die() 중복 호출 무시");
            return;
        }

        isDead = true;

        Debug.Log($"[Enemy] ===== Die() 시작 =====");
        Debug.Log($"[Enemy] {name} 사망");
        Debug.Log($"[Enemy] Point Reward: {pointReward}");

        Debug.Log($"[Enemy] OnEnemyDeath 이벤트 발생 전");

        try
        {
            OnEnemyDeath?.Invoke(this);
            Debug.Log($"[Enemy] OnEnemyDeath 이벤트 발생 완료 (예외 없음)");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Enemy] OnEnemyDeath 이벤트 중 예외 발생!!!");
            Debug.LogError($"[Enemy] 예외 메시지: {ex.Message}");
            Debug.LogError($"[Enemy] 스택 트레이스:\n{ex.StackTrace}");
        }

        Debug.Log($"[Enemy] SetActive(false) 호출 전 - gameObject.activeSelf: {gameObject.activeSelf}");

        gameObject.SetActive(false);

        Debug.Log($"[Enemy] SetActive(false) 호출 후 - gameObject.activeSelf: {gameObject.activeSelf}");
        Debug.Log($"[Enemy] ===== Die() 종료 =====");
    }

    public int GetPointAmount()
    {
        return pointReward;
    }
}
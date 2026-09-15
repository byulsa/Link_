using UnityEngine;

public class Player : MonoBehaviour, IDamageable
{
    [Header("References")]
    [SerializeField]
    private Entity entity;

    [SerializeField]
    private CodeController codeController;

    [Header("Health")]
    [SerializeField]
    private float maxHealth = 100f;

    private float currentHealth;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;

    private void Awake()
    {
        if (entity == null)
        {
            entity = GetComponent<Entity>();
        }

        if (codeController == null)
        {
            codeController = GetComponent<CodeController>();
        }

        currentHealth = maxHealth;
    }

    private void OnEnable()
    {
        currentHealth = maxHealth;

        Debug.Log("[Player] OnEnemyDeath 구독");
        Enemy.OnEnemyDeath += HandleEnemyDeath;
    }

    private void OnDisable()
    {
        Enemy.OnEnemyDeath -= HandleEnemyDeath;
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0f)
            return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0f, currentHealth);

        Debug.Log(
            $"[Player] 피해: {damage} / HP: {currentHealth}/{maxHealth}"
        );

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("[Player] 사망");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Entity target =
            other.GetComponent<Entity>();

        if (target == null)
            return;

        if (!target.Is(EntityType.Enemy))
            return;

        Debug.Log(
            $"[Player] Enemy 충돌 감지: {target.name}"
        );

        codeController.ExecuteTouch(target);
    }

    private void HandleEnemyDeath(
        Enemy enemy,
        DeathType deathType)
    {
        if (enemy == null)
            return;

        Debug.Log(
            $"[Player] Enemy 사망 감지: {enemy.name} / {deathType}"
        );

        if (deathType == DeathType.Normal)
        {
            codeController.ExecuteDeath(
                enemy.GetComponent<Entity>()
            );

            CodeDropManager.Instance.TryDrop(
                enemy.CodeDropTable
            );
        }
    }
}
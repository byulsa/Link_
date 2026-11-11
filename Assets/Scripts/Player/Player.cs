using System;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour, IDamageable
{
    [Header("References")]
    [SerializeField]
    private Entity entity;

    [SerializeField]
    private CodeController codeController;

    [SerializeField]
    private CodeDropManager codeDropManager;

    [Header("Health")]
    [SerializeField]
    private float maxHealth = 100f;

    private float currentHealth;
    private float baseMaxHealth;
    private float baseMoveSpeed;
    private PlayerMovement movement;
    private PassiveEffectManager passiveEffects;
    private bool started;
    public event Action<float> OnDamaged;

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

        movement = GetComponent<PlayerMovement>();
        baseMaxHealth = maxHealth;
        baseMoveSpeed = movement != null ? movement.Speed : 0f;
        currentHealth = maxHealth;
    }

    private void Start()
    {
        started = true;
        BindPassives();
        RefreshStatsFromCode();
    }

    private void BindPassives()
    {
        if (passiveEffects != null) passiveEffects.OnPassivesChanged -= RefreshStatsFromCode;
        passiveEffects = PassiveEffectManager.Instance;
        if (passiveEffects != null) passiveEffects.OnPassivesChanged += RefreshStatsFromCode;
    }

    public void RefreshStatsFromCode()
    {
        CodeEditor editor = codeController != null ? codeController.Editor : null;
        if (editor != null && editor.IsReady) HandleCodeRefreshed(editor.Chains);
    }

    private void HandleCodeRefreshed(IReadOnlyList<CodeChain> chains)
    {
        CodeEditor editor = codeController != null ? codeController.Editor : null;
        if (editor == null || !ReferenceEquals(chains, editor.Chains)) return;
        float health = baseMaxHealth;
        float speed = baseMoveSpeed;
        foreach (CodeChain chain in chains)
        {
            if (chain == null || !CodeValidator.IsValidPlayerStatChain(chain.nodes)) continue;
            CodeNode actions = chain.nodes[1];
            int count = actions.blockType == BlockType.PARENTHESIS_BUNDLE ? actions.groupTargets.Count : 1;
            for (int i = 0; i < count; i++)
            {
                BlockType action = actions.blockType == BlockType.PARENTHESIS_BUNDLE ? actions.groupTargets[i] : actions.blockType;
                if (action == BlockType.HP) health = CodeChainEvaluator.CalculateValue(health, chain, 2);
                else if (action == BlockType.SPD) speed = CodeChainEvaluator.CalculateValue(speed, chain, 2);
            }
        }
        // Keep the health ratio so repeatedly editing/reapplying code cannot heal or resurrect the player.
        float ratio = maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 1f;
        maxHealth = float.IsNaN(health) || float.IsInfinity(health) ? baseMaxHealth : Mathf.Max(1f, health);
        currentHealth = maxHealth * ratio;
        if (movement != null) movement.SetCodeSpeed(float.IsNaN(speed) || float.IsInfinity(speed) ? baseMoveSpeed : speed);
    }

    private void OnEnable()
    {
        currentHealth = maxHealth;

        Debug.Log("[Player] OnEnemyDeath 구독");
        Enemy.OnEnemyDeath += HandleEnemyDeath;
        CodeEditor.OnCodeRefreshed += HandleCodeRefreshed;
        if (started) { BindPassives(); RefreshStatsFromCode(); }
    }

    private void OnDisable()
    {
        Enemy.OnEnemyDeath -= HandleEnemyDeath;
        CodeEditor.OnCodeRefreshed -= HandleCodeRefreshed;
        if (passiveEffects != null) passiveEffects.OnPassivesChanged -= RefreshStatsFromCode;
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0f || currentHealth <= 0f || float.IsNaN(damage))
            return;

        float appliedDamage = Mathf.Min(damage, currentHealth);
        currentHealth -= appliedDamage;
        currentHealth = Mathf.Max(0f, currentHealth);

        Debug.Log($"[Player] 피해: {damage} / HP: {currentHealth}/{maxHealth}");

        OnDamaged?.Invoke(appliedDamage);

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
        Entity target = other.GetComponent<Entity>();

        if (target == null)
            return;

        if (!target.Is(EntityType.Enemy))
            return;

        Debug.Log($"[Player] Enemy 충돌 감지: {target.name}");

        codeController.ExecuteTouch(target);
    }

    private void HandleEnemyDeath(Enemy enemy, DeathType deathType)
    {
        if (enemy == null)
            return;

        Debug.Log($"[Player] Enemy 사망 감지: {enemy.name} / {deathType}");

        if (deathType == DeathType.Normal)
        {
            codeController.ExecuteDeath(enemy.GetComponent<Entity>());

            if (codeDropManager == null)
                Debug.LogError("[Player] CodeDropManager가 설정되지 않아 코드 드롭을 처리하지 못했습니다.", this);
            else
                codeDropManager.TryDrop(enemy.CodeDropTable);
        }
    }
}

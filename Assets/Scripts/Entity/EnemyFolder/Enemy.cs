using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;

enum EnmeyTrigger
{
    Alive,
    Dead,
}

public enum DeathType
{
    Normal,
    SelfDestruct,
}

public abstract class Enemy : MonoBehaviour, IDamageable, IPointDrop
{
    private const float MaxMoveSpeedMultiplier = 2.5f;

    [Header("Data Reference")]
    [SerializeField]
    private EnemyData enemyData;

    [Header("Visual")]
    [SerializeField]
    private SpriteRenderer spriteRenderer;
    private Color defaultSpriteColor = Color.white;
    private Vector3 defaultSpriteScale = Vector3.one;
    private Coroutine damageColorCoroutine;
    private Tween damageScaleTween;
    private Tween deathTween;

    [Header("Death Animation")]
    [SerializeField]
    private Animator deathAnimator;
    [SerializeField]
    private string deathTriggerName = "Die";
    [SerializeField, Min(0f)]
    private float deathAnimationDuration = 0.5f;
    private Coroutine deathAnimationCoroutine;
    private Collider2D[] enemyColliders;
    private bool[] colliderEnabledOnAwake;

    [Header("Movement")]
    [SerializeField]
    private string playerTag = "Player";

    [Header("Knockback Settings")]
    [SerializeField]
    private float knockbackForce = 5f;

    [SerializeField]
    private float knockbackDuration = 0.2f;

    [SerializeField]
    private int pointReward = 10;

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
    public float MoveSpeed => moveSpeed;
    public float StatMultiplier { get; private set; } = 1f;

    protected float ScaleAttackDuration(float duration) => duration / StatMultiplier;

    // Always rebuild from shared base data, including when an instance is pooled.
    public void InitializeForSpawn(EnemyData data, float statMultiplier)
    {
        InitializeFromData(data);
        StatMultiplier = Mathf.Max(1f, statMultiplier);
        maxHealth = data.maxHealth * StatMultiplier;
        currentHealth = maxHealth;
        baseDamage = data.baseDamage * StatMultiplier;
        float moveSpeedMultiplier = Mathf.Min(Mathf.Sqrt(StatMultiplier), MaxMoveSpeedMultiplier);
        moveSpeed = data.moveSpeed * moveSpeedMultiplier;
        knockbackForce = data.knockbackForce / StatMultiplier;
        knockbackDuration = data.knockbackDuration / StatMultiplier;
    }

    [SerializeField]
    private CodeDropTable codeDropTable;
    public CodeDropTable CodeDropTable => codeDropTable;

    protected virtual void Awake()
    {
        DamageNumberPool.WarmUp();
        rb = GetComponent<Rigidbody2D>();
        enemyColliders = GetComponentsInChildren<Collider2D>();
        colliderEnabledOnAwake = new bool[enemyColliders.Length];
        for (int i = 0; i < enemyColliders.Length; i++)
            colliderEnabledOnAwake[i] = enemyColliders[i].enabled;

        if (deathAnimator == null)
            deathAnimator = GetComponentInChildren<Animator>();

        if (spriteRenderer != null)
        {
            defaultSpriteColor = spriteRenderer.color;
            defaultSpriteScale = spriteRenderer.transform.localScale;
        }

        if (enemyData != null)
            InitializeFromData(enemyData);
    }

    protected virtual void OnEnable()
    {
        currentHealth = maxHealth;

        isKnockedBack = false;
        isDead = false;
        if (deathAnimator != null)
        {
            deathAnimator.speed = 1f;
            deathAnimator.Rebind();
            deathAnimator.Update(0f);
        }
        SetCollidersEnabled(true);
        deathTween?.Kill();
        deathTween = null;
        if (deathAnimationCoroutine != null)
        {
            StopCoroutine(deathAnimationCoroutine);
            deathAnimationCoroutine = null;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = defaultSpriteColor;
            spriteRenderer.transform.localScale = defaultSpriteScale;
        }

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);

            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }
    }

    public virtual void InitializeFromData(EnemyData data)
    {
        enemyData = data;
        StatMultiplier = 1f;

        maxHealth = data.maxHealth;
        baseDamage = data.baseDamage;
        moveSpeed = data.moveSpeed;

        currentHealth = maxHealth;

        knockbackForce = data.knockbackForce;
        knockbackDuration = data.knockbackDuration;

        pointReward = data.PointDropNum;
        defaultSpriteColor = Color.white;
        if (spriteRenderer != null)
            spriteRenderer.color = defaultSpriteColor;
    }

    // -------------------------
    // Damage
    // -------------------------

    public void TakeDamage(float damage)
    {
        Vector2 knockbackDir = Vector2.zero;

        if (playerTransform != null)
        {
            knockbackDir = (transform.position - playerTransform.position).normalized;
        }

        TakeDamage(damage, knockbackDir);
    }

    public virtual void TakeDamage(float damage, Vector2 hitDirection)
    {
        if (isDead || !(damage > 0f)) return;
        float appliedDamage = Mathf.Min(damage, currentHealth);
        currentHealth -= appliedDamage;
        if (appliedDamage > 0f)
            ShowDamageNumber(appliedDamage);

        Debug.Log($"[Enemy] 피해: {damage} " + $"HP: {currentHealth}/{maxHealth}");

        if (hitDirection != Vector2.zero)
        {
            ApplyKnockback(hitDirection);
        }
        DamgeColor();
        DmgTween();
        if (currentHealth <= 0f)
        {
            Debug.Log("[Enemy] currentHealth <= 0, Die() 호출 준비");

            Die();
        }
    }
    private void ShowDamageNumber(float damage)
    {
        Vector3 position = transform.position;
        if (spriteRenderer != null)
        {
            Bounds bounds = spriteRenderer.bounds;
            position = bounds.center;
            position.y = bounds.max.y;
        }
        DamageNumberPool.Show(damage, position, spriteRenderer);
    }

    public void DamgeColor()
    {
        if (spriteRenderer != null)
        {
            if (damageColorCoroutine != null)
                StopCoroutine(damageColorCoroutine);

            spriteRenderer.color = Color.red;
            damageColorCoroutine = StartCoroutine(DamageColorRoutine());
        }
    }
    public IEnumerator DamageColorRoutine()
    {
        if (spriteRenderer == null)
            yield break;

        yield return new WaitForSeconds(0.2f);

        spriteRenderer.color = defaultSpriteColor;
        damageColorCoroutine = null;
    }

    protected virtual void OnDisable()
    {
        CancelKnockback();
        if (damageColorCoroutine != null)
        {
            StopCoroutine(damageColorCoroutine);
            damageColorCoroutine = null;
        }

        if (deathAnimationCoroutine != null)
        {
            StopCoroutine(deathAnimationCoroutine);
            deathAnimationCoroutine = null;
        }

        damageScaleTween?.Kill();
        damageScaleTween = null;
        deathTween?.Kill();
        deathTween = null;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = defaultSpriteColor;
            spriteRenderer.transform.localScale = defaultSpriteScale;
        }
    }
    public void DmgTween()//피격시 크기가 커졌다가 원래대로 돌아오는 트윈
    {
        if (spriteRenderer == null)
            return;

        damageScaleTween?.Kill();
        Transform spriteTransform = spriteRenderer.transform;
        spriteTransform.localScale = defaultSpriteScale;

        Sequence tween = DOTween.Sequence();
        tween.Append(spriteTransform.DOScale(defaultSpriteScale * 1.2f, 0.1f));
        tween.Append(spriteTransform.DOScale(defaultSpriteScale, 0.1f));
        tween.OnComplete(() => damageScaleTween = null);
        damageScaleTween = tween;
    }

    protected void UpdateFacing(Vector2 movementDirection)
    {
        if (spriteRenderer == null)
            return;

        if (movementDirection.x > 0.01f)
        {
            // 오른쪽 이동
            spriteRenderer.flipX = false;
        }
        else if (movementDirection.x < -0.01f)
        {
            // 왼쪽 이동
            spriteRenderer.flipX = true;
        }
    }

    protected void FacePlayer()
    {
        if (playerTransform == null || spriteRenderer == null)
            return;

        float directionX = playerTransform.position.x - transform.position.x;

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
    private Coroutine coroutineKnockback;

    private void ApplyKnockback(Vector2 direction)
    {
        if (coroutineKnockback != null)
        {
            StopCoroutine(coroutineKnockback);
        }

        coroutineKnockback = StartCoroutine(KnockbackRoutine(direction));
    }

    private IEnumerator KnockbackRoutine(Vector2 direction)
    {
        isKnockedBack = true;

        Vector2 dir = direction.normalized;
        float t = 0f;

        while (t < knockbackDuration)
        {
            float ratio = 1f - (t / knockbackDuration);

            if (rb != null)
            {
                rb.linearVelocity = dir * knockbackForce * ratio;
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

        Vector2 direction = (playerTransform.position - transform.position).normalized;
        UpdateFacing(direction);
        rb.linearVelocity = direction * moveSpeed;
    }

    protected void MoveAwayFromPlayer()
    {
        if (playerTransform == null || rb == null)
            return;

        Vector2 direction = (transform.position - playerTransform.position).normalized;
        UpdateFacing(direction);
        rb.linearVelocity = direction * moveSpeed;
    }

    protected void CancelKnockback()
    {
        if (coroutineKnockback != null) StopCoroutine(coroutineKnockback);
        coroutineKnockback = null;
        isKnockedBack = false;
        StopMovement();
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

        yield return new WaitForSeconds(recoveryTime);
    }

    // -------------------------
    // Death
    // -------------------------

    protected virtual void Die(DeathType deathType = DeathType.Normal)
    {
        if (isDead)
            return;

        isDead = true;
        CancelKnockback();
        if (deathAnimator != null) deathAnimator.speed = 1f;

        Debug.Log($"[Enemy] {name} 사망 / DeathType: {deathType}");

        StopMovement();
        SetCollidersEnabled(false);

        try
        {
            OnEnemyDeath?.Invoke(this, deathType);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Enemy] OnEnemyDeath 예외: {ex.Message}");

            Debug.LogError(ex.StackTrace);
        }

        if (HasDeathAnimationTrigger())
        {
            deathAnimator.SetTrigger(deathTriggerName);
            deathAnimationCoroutine = StartCoroutine(WaitForDeathAnimation());
        }
        else
        {
            PlayFallbackDeathAnimation();
        }
    }

    private bool HasDeathAnimationTrigger()
    {
        if (deathAnimator == null || string.IsNullOrEmpty(deathTriggerName))
            return false;

        foreach (AnimatorControllerParameter parameter in deathAnimator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger
                && parameter.name == deathTriggerName)
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerator WaitForDeathAnimation()
    {
        if (deathAnimationDuration > 0f)
            yield return new WaitForSeconds(deathAnimationDuration);

        CompleteDeathAnimation();
    }

    protected virtual void PlayFallbackDeathAnimation()
    {
        if (spriteRenderer == null || deathAnimationDuration <= 0f)
        {
            CompleteDeathAnimation();
            return;
        }

        Transform visualTransform = spriteRenderer.transform;
        Sequence sequence = DOTween.Sequence();
        sequence.Join(spriteRenderer.DOFade(0f, deathAnimationDuration));
        sequence.Join(visualTransform.DOScale(defaultSpriteScale * 0.2f, deathAnimationDuration));
        sequence.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        sequence.OnComplete(CompleteDeathAnimation);
        deathTween = sequence;
    }

    public void AnimationEvent_DeathComplete()
    {
        if (isDead)
            CompleteDeathAnimation();
    }

    private void CompleteDeathAnimation()
    {
        if (!isDead)
            return;

        if (deathAnimationCoroutine != null)
        {
            StopCoroutine(deathAnimationCoroutine);
            deathAnimationCoroutine = null;
        }

        deathTween?.Kill();
        deathTween = null;
        gameObject.SetActive(false);
    }

    private void SetCollidersEnabled(bool isEnabled)
    {
        if (enemyColliders == null)
            return;

        for (int i = 0; i < enemyColliders.Length; i++)
        {
            Collider2D enemyCollider = enemyColliders[i];
            if (enemyCollider != null)
                enemyCollider.enabled = isEnabled && colliderEnabledOnAwake[i];
        }
    }

    public int GetPointAmount()
    {
        return pointReward;
    }
}

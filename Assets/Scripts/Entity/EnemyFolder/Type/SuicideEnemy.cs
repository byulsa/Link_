using System.Collections;
using UnityEngine;

public class SuicideEnemy : Enemy
{
    [SerializeField] private SuicideEnemyData suicideData;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer visual;
    [SerializeField] private ParticleSystem explosionEffect;
    [SerializeField] private Color warningColor = new Color(1f, 0.1f, 0.05f, 1f);
    private SelfDestructCondition? conditionOverride;
    private SelfDestructCondition Condition => conditionOverride ?? suicideData.condition;
    public void SetConditionOverride(SelfDestructCondition? condition) => conditionOverride = condition;

    private bool charging, exploded;
    private bool chasingAfterDeath;
    private float readyTime;
    private Coroutine routine;
    private Rigidbody2D body;
    private RigidbodyConstraints2D originalConstraints;

    protected override void Awake()
    {
        base.Awake();
        body = GetComponent<Rigidbody2D>();
        if (body != null) originalConstraints = body.constraints;
        if (suicideData != null) InitializeFromData(suicideData);
    }

    public override void InitializeFromData(EnemyData data)
    {
        base.InitializeFromData(data);
        suicideData = data as SuicideEnemyData;
        conditionOverride = null;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        charging = exploded = false;
        chasingAfterDeath = false;
        if (body != null) body.constraints = originalConstraints;
        if (visual != null) visual.enabled = true;
        if (explosionEffect != null) explosionEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (animator != null) { animator.speed = 1f; animator.Rebind(); animator.Update(0f); }
    }

    protected override void Update()
    {
        if (IsDead || suicideData == null) return;
        if (charging)
        {
            if (chasingAfterDeath)
                ChaseBeforeExplosion();
            return;
        }
        if (Condition != SelfDestructCondition.OnDeath && IsPlayerBodyInRange(suicideData.triggerRange))
        { BeginSelfDestruct(); return; }
        base.Update();
    }

    private void LateUpdate()
    {
        if (charging && !exploded && visual != null)
            visual.color = Color.Lerp(Color.white, warningColor, Mathf.Clamp01(1f - (readyTime - Time.time)));
    }

    protected override void ApplyIncomingDamage(float damage, Vector2 direction, bool roundDamage)
    {
        if (!IsDead && !charging) base.ApplyIncomingDamage(damage, direction, roundDamage);
    }

    protected override void Die(DeathType deathType = DeathType.Normal)
    {
        if (IsDead || charging) return;
        if (suicideData != null) BeginSelfDestruct(chasePlayer: true);
        else
        {
            CancelKnockback();
            if (body != null) body.constraints = RigidbodyConstraints2D.FreezeAll;
            base.Die(deathType);
        }
    }

    private void BeginSelfDestruct(bool chasePlayer = false)
    {
        if (charging || IsDead) return;
        charging = true;
        chasingAfterDeath = chasePlayer;
        GameplaySfx.Play(GameplaySound.SelfDestructWarning);
        CancelKnockback();
        if (body != null && !chasePlayer) body.constraints = RigidbodyConstraints2D.FreezeAll;
        readyTime = Time.time + 1f;
        routine = StartCoroutine(Charge());
    }

    private void ChaseBeforeExplosion()
    {
        if (body == null || PlayerTarget == null || PlayerTarget.CurrentHealth <= 0f ||
            !PlayerTarget.isActiveAndEnabled || IsPlayerBodyInRange(0.25f))
        {
            StopMovement();
            return;
        }

        Vector2 direction = ((Vector2)PlayerTarget.transform.position - body.position).normalized;
        UpdateFacing(direction);
        body.linearVelocity = direction * (MoveSpeed * suicideData.deathChaseSpeedMultiplier);
    }

    private IEnumerator Charge()
    {
        if (animator != null && !chasingAfterDeath) animator.speed = 0f;
        yield return new WaitForSeconds(1f);
        if (IsDead) yield break;
        chasingAfterDeath = false;
        StopMovement();
        if (body != null) body.constraints = RigidbodyConstraints2D.FreezeAll;
        if (animator != null) { animator.speed = 1f; animator.SetTrigger("SelfDestruct"); }
        // Fallback when an animation event cannot run.
        yield return new WaitForSeconds(0.25f);
        AnimationEvent_Explode();
        routine = null;
    }

    public void AnimationEvent_Explode()
    {
        if (IsDead || !charging || exploded || Time.time < readyTime) return;
        exploded = true;
        if (suicideData != null && IsPlayerBodyInRange(suicideData.explosionRange))
            PlayerTarget.TakeDamage(suicideData.explosionDamage * StatMultiplier);
        if (explosionEffect != null) explosionEffect.Play(true);
        if (visual != null) visual.enabled = false;
        base.Die(DeathType.SelfDestruct);
    }

    private bool IsPlayerBodyInRange(float range)
    {
        if (PlayerTarget == null || !PlayerTarget.isActiveAndEnabled || PlayerTarget.CurrentHealth <= 0f)
            return false;

        Collider2D playerBody = PlayerTarget.BodyCollider;
        return playerBody != null && playerBody.enabled
            && Vector2.Distance(transform.position, playerBody.ClosestPoint(transform.position)) <= range;
    }

    protected override void PlayFallbackDeathAnimation()
    {
        if (exploded) StartCoroutine(WaitForExplosionEffect());
        else base.PlayFallbackDeathAnimation();
    }

    private IEnumerator WaitForExplosionEffect()
    {
        // Keep the root scale unchanged while its child particles finish.
        do { yield return null; }
        while (explosionEffect != null && explosionEffect.IsAlive(true));
        AnimationEvent_DeathComplete();
    }

    protected override void OnDisable()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        chasingAfterDeath = false;
        CancelKnockback();
        if (body != null) body.constraints = originalConstraints;
        if (animator != null) animator.speed = 1f;
        base.OnDisable();
    }
}

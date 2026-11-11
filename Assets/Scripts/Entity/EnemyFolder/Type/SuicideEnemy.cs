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
        if (body != null) body.constraints = originalConstraints;
        if (visual != null) visual.enabled = true;
        if (explosionEffect != null) explosionEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (animator != null) { animator.speed = 1f; animator.Rebind(); animator.Update(0f); }
    }

    protected override void Update()
    {
        if (IsDead || charging || suicideData == null) return;
        if (Condition != SelfDestructCondition.OnDeath && PlayerTransform != null &&
            Vector2.Distance(transform.position, PlayerTransform.position) <= suicideData.triggerRange)
        { BeginSelfDestruct(); return; }
        base.Update();
    }

    private void LateUpdate()
    {
        if (charging && !exploded && visual != null)
            visual.color = Color.Lerp(Color.white, warningColor, Mathf.Clamp01(1f - (readyTime - Time.time)));
    }

    public override void TakeDamage(float damage, Vector2 direction)
    {
        if (!IsDead && !charging) base.TakeDamage(damage, direction);
    }

    protected override void Die(DeathType deathType = DeathType.Normal)
    {
        if (IsDead || charging) return;
        if (suicideData != null && Condition != SelfDestructCondition.PlayerNearby) BeginSelfDestruct();
        else
        {
            CancelKnockback();
            if (body != null) body.constraints = RigidbodyConstraints2D.FreezeAll;
            base.Die(deathType);
        }
    }

    private void BeginSelfDestruct()
    {
        if (charging || IsDead) return;
        charging = true;
        CancelKnockback();
        if (body != null) body.constraints = RigidbodyConstraints2D.FreezeAll;
        readyTime = Time.time + 1f;
        routine = StartCoroutine(Charge());
    }

    private IEnumerator Charge()
    {
        if (animator != null) animator.speed = 0f;
        yield return new WaitForSeconds(1f);
        if (animator != null) { animator.speed = 1f; animator.SetTrigger("SelfDestruct"); }
        // Fallback when an animation event cannot run.
        yield return new WaitForSeconds(0.25f);
        AnimationEvent_Explode();
        routine = null;
    }

    public void AnimationEvent_Explode()
    {
        if (!charging || exploded || Time.time < readyTime) return;
        exploded = true;
        if (PlayerTransform != null && suicideData != null)
        {
            bool inRange = Vector2.Distance(transform.position, PlayerTransform.position) <= suicideData.explosionRange;
            foreach (Collider2D col in PlayerTransform.GetComponentsInChildren<Collider2D>())
                if (col.enabled && col.gameObject.activeInHierarchy &&
                    Vector2.Distance(transform.position, col.ClosestPoint(transform.position)) <= suicideData.explosionRange)
                    inRange = true;
            if (inRange && PlayerTransform.TryGetComponent<IDamageable>(out var target)) target.TakeDamage(suicideData.explosionDamage * StatMultiplier);
        }
        if (explosionEffect != null) explosionEffect.Play(true);
        if (visual != null) visual.enabled = false;
        base.Die(DeathType.SelfDestruct);
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
        CancelKnockback();
        if (body != null) body.constraints = originalConstraints;
        if (animator != null) animator.speed = 1f;
        base.OnDisable();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class BossEnemy : MeleeEnemy
{
    [SerializeField] private SpriteRenderer visual;
    [Header("Attack Effect Prefabs")]
    [SerializeField] private BossAttackEffect laserEffectPrefab;
    [SerializeField] private BossAttackEffect slashEffectPrefab;
    [SerializeField, Min(0f)] private float timeBetweenPatterns = 5f;
    [SerializeField, Min(0f)] private float laserDamage = 24f;
    [SerializeField, Min(0f)] private float slashDamage = 38f;
    [Header("Bonus Damage (% of Player Max HP)")]
    [SerializeField, Range(2f, 5f)] private float meleeHealthDamagePercent = 2f;
    [SerializeField, Range(2f, 5f)] private float laserHealthDamagePercent = 3f;
    [SerializeField, Range(2f, 5f)] private float slashHealthDamagePercent = 5f;
    [SerializeField, Range(0.5f, 0.75f)] private float patternDamageReduction = 0.65f;
    [SerializeField, Range(0f, 1f)] private float lifeSteal = 0.1f;

    [Header("Phase Transition Lasers")]
    [SerializeField, Range(1, 16)] private int phaseLaserCount = 6;

    private const float LaserWidth = 0.55f;
    private const float LaserFireTime = 0.6f;
    private const float FollowUpWidth = 0.4f;
    private const float LaserFadeTime = 0.22f;
    private const float SlashWidth = 1.25f;
    private const float SlashFadeTime = 0.32f;
    private const float PhaseLaserWidth = 0.65f;
    private const float PhaseWarningTime = 0.75f;
    private static readonly Color LaserColor = new Color(1f, 0.17f, 0.11f, 1f);
    private static readonly Color PhaseLaserColor = new Color(1f, 0.37f, 0.17f, 1f);
    private static readonly int Moving = Animator.StringToHash("Moving");
    private static readonly int Idle = Animator.StringToHash("Base Layer.Idle");
    private static readonly int Laser = Animator.StringToHash("Base Layer.Laser");
    private static readonly int Slash = Animator.StringToHash("Base Layer.Slash");

    private readonly HashSet<Enemy> slashTargets = new();
    private sealed class BeamEffect
    {
        public LineRenderer Line;
        public Color Color;
        public float Width;
        public float Duration;
        public float Elapsed;
        public BossAttackEffect Style;
    }

    private readonly BeamEffect primaryBeam = new();
    private readonly BeamEffect followUpBeam = new();
    private readonly BeamEffect slashBeam = new();
    private BeamEffect[] phaseBeams;
    private Vector3[] phaseLaserCenters;
    private Rigidbody2D body;
    private Coroutine pattern;
    private LineRenderer attackLine;
    private LineRenderer slashWarning;
    private Material lineMaterial;
    private Vector3 lineStart;
    private Vector3 lineEnd;
    private float patternTimer;
    private bool hidden;
    private bool firingLasers;
    private bool isPhaseTwo;
    private PlayerHitFeedback phaseFeedback;
    [Header("Death Effects")]
    [SerializeField] private ParticleSystem deathExplosion;
    [SerializeField, Range(0.05f, 1f)] private float deathSlowScale = 0.25f;
    [SerializeField, Min(0.05f)] private float deathAnticipation = 0.4f;
    [SerializeField, Min(0.05f)] private float deathFlashDuration = 0.2f;
    [SerializeField, Min(0.1f)] private float deathExplosionTimeout = 3f;
    private Image deathFlash;
    private GameObject deathFlashOverlay;
    private ParticleSystem activeDeathExplosion;
    private float previousDeathTimeScale;
    private float appliedDeathTimeScale;
    private bool slowingDeath;
    private bool followUpArmed;
    private bool transitioningPhase;

    protected override bool UsesWaveStatMultiplier => false;
    private float LaserSpeed => isPhaseTwo ? 1.5f : 1f;
    private float PatternSpeed => isPhaseTwo ? 1.5f : 1f;
    private float CurrentPatternSpeed => transitioningPhase ? 1f : firingLasers ? LaserSpeed : PatternSpeed;

    protected override void Awake()
    {
        base.Awake();
        body = GetComponent<Rigidbody2D>();
        CreateAttackLine();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        patternTimer = timeBetweenPatterns;
        isPhaseTwo = false;
    }

    protected override void Update()
    {
        if (IsDead || PlayerTransform == null)
            return;

        if (pattern != null)
        {
            animator.speed = CurrentPatternSpeed;
            return;
        }

        patternTimer -= Time.deltaTime * PatternSpeed;
        if (patternTimer <= 0f && !IsAttacking && !IsKnockedBack)
        {
            pattern = StartCoroutine(RunPattern());
            return;
        }

        base.Update();
        animator.SetBool(Moving, !IsAttacking && body.linearVelocity.sqrMagnitude > 0.01f);
    }

    protected override void ApplyIncomingDamage(float damage, Vector2 hitDirection, bool roundDamage)
    {
        if (!hidden)
        {
            base.ApplyIncomingDamage(pattern != null ? damage * (1f - patternDamageReduction) : damage, Vector2.zero, roundDamage);
            if (!IsDead && !isPhaseTwo && CurrentHealth <= MaxHealth * 0.5f)
                BeginPhaseTwo();
        }
    }

    protected override void HitPlayer(Player player, float damage)
    {
        DamagePlayerAndHeal(player, damage, meleeHealthDamagePercent);
    }

    private float DamagePlayerAndHeal(Player player, float damage, float healthDamagePercent)
    {
        float appliedDamage = player.ApplyDamage(damage + player.MaxHealth * (healthDamagePercent / 100f));
        RestoreHealth(appliedDamage * lifeSteal);
        return appliedDamage;
    }

    private IEnumerator RunPattern()
    {
        CancelKnockback();
        animator.SetBool(Moving, false);
        firingLasers = true;
        animator.speed = LaserSpeed;
        // Phase two fires two lasers per swing.
        int swings = isPhaseTwo ? 3 : 4;
        for (int swing = 0; swing < swings; swing++)
        {
            followUpArmed = false;
            PrepareLaser(LaserFireTime / LaserSpeed);
            animator.Play(Laser, 0, 0f);
            yield return WaitForPatternTime(isPhaseTwo ? 1.06f : 0.84f);
            HideLine();
            if (PlayerTransform == null)
            {
                ResetPattern();
                yield break;
            }
        }

        firingLasers = false;
        animator.speed = PatternSpeed;
        hidden = true;
        visual.enabled = false;
        SetCollidersEnabled(false);
        yield return WaitForPatternTime(0.45f);

        if (PlayerTransform != null)
        {
            Vector3 target = PlayerTransform.position;
            float side = Random.value < 0.5f ? -1f : 1f;
            body.position = (Vector2)target + Vector2.right * (2.5f * side);
            transform.position = new Vector3(body.position.x, body.position.y, transform.position.z);
            SetCollidersEnabled(true);
            visual.enabled = true;
            hidden = false;
            UpdateFacing(Vector2.left * side);

            AimLine(transform.position, Vector2.right);
            ShowLine(0.12f, new Color(1f, 0.52f, 0.16f, 0.65f));
            animator.Play(Slash, 0, 0f);
            yield return WaitForPatternTime(0.96f);
        }

        ResetPattern();
    }

    private void BeginPhaseTwo()
    {
        isPhaseTwo = true;
        GameplaySfx.Play(GameplaySound.BossPhaseWarning);
        if (PlayerTarget != null && PlayerTarget.TryGetComponent(out phaseFeedback))
            phaseFeedback.SetBossPhaseVignette(true);
        CancelPattern();
        CancelMeleeAttack();
        CancelKnockback();
        animator.SetBool(Moving, false);
        animator.speed = 1f;
        animator.Play(Idle, 0, 0f);
        transitioningPhase = true;
        pattern = StartCoroutine(RunPhaseTransition());
    }

    private IEnumerator RunPhaseTransition()
    {
        PreparePhaseLasers(Vector2.up);
        yield return new WaitForSeconds(PhaseWarningTime);
        FirePhaseLasers(Vector2.up);

        yield return new WaitForSeconds(1f - PhaseWarningTime);
        PreparePhaseLasers(Vector2.right);
        yield return new WaitForSeconds(PhaseWarningTime);
        FirePhaseLasers(Vector2.right);

        yield return new WaitForSeconds(LaserFadeTime);
        ResetPattern();
    }

    private void PreparePhaseLasers(Vector2 direction)
    {
        Camera camera = Camera.main;
        bool vertical = direction == Vector2.up;
        Vector3 fallbackAxis = vertical ? Vector3.right : Vector3.up;
        for (int i = 0; i < phaseBeams.Length; i++)
        {
            float fraction = (i + 1f) / (phaseBeams.Length + 1f);
            Vector3 center = transform.position + fallbackAxis * ((fraction - 0.5f) * 18f);
            if (camera != null && camera.orthographic)
            {
                float depth = camera.WorldToViewportPoint(transform.position).z;
                center = camera.ViewportToWorldPoint(
                    new Vector3(vertical ? fraction : 0.5f, vertical ? 0.5f : fraction, depth));
            }

            phaseLaserCenters[i] = center;
            AimLine(center, direction);
            BeamEffect beam = phaseBeams[i];
            beam.Duration = 0f;
            if (beam.Line != null)
                DrawWarning(beam.Line, 0.1f, new Color(1f, 0.44f, 0.16f, 0.7f));
        }
    }

    private void FirePhaseLasers(Vector2 direction)
    {
        GameplaySfx.Play(GameplaySound.BossPhaseBurst);
        bool damagedPlayer = false;
        for (int i = 0; i < phaseBeams.Length; i++)
        {
            AimLine(phaseLaserCenters[i], direction);
            ShowBeam(phaseBeams[i], PhaseLaserWidth, PhaseLaserColor, LaserFadeTime);
            if (!damagedPlayer)
                damagedPlayer = DamageAlongLine(PhaseLaserWidth, laserDamage, laserHealthDamagePercent, false, 30f);
        }
    }

    private void PrepareLaser(float timeUntilFire)
    {
        Vector3 target = PredictLaserTarget(timeUntilFire);
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        UpdateFacing((Vector2)(target - transform.position));
        AimLine(target, direction);
        ShowLine(0.09f, new Color(1f, 0.2f, 0.12f, 0.55f));
    }

    private Vector3 PredictLaserTarget(float timeUntilFire)
    {
        Vector3 target = PlayerTransform.position;
        if (PlayerTarget.TryGetComponent<Rigidbody2D>(out var playerBody))
        {
            Vector2 lead = playerBody.linearVelocity * timeUntilFire;
            target += (Vector3)Vector2.ClampMagnitude(lead, 4f);
        }
        return target;
    }

    private IEnumerator WaitForPatternTime(float duration)
    {
        while (duration > 0f)
        {
            yield return null;
            duration -= Time.deltaTime * CurrentPatternSpeed;
        }
    }

    public void AnimationEvent_BossLaser()
    {
        if (pattern == null || IsDead || !firingLasers)
            return;

        FireLaser(primaryBeam, LaserWidth);
        followUpArmed = isPhaseTwo && PlayerTransform != null;
        if (followUpArmed)
            PrepareLaser(0f);
    }

    public void AnimationEvent_BossLaserFollowUp()
    {
        if (pattern == null || IsDead || !firingLasers || !followUpArmed)
            return;

        followUpArmed = false;
        FireLaser(followUpBeam, FollowUpWidth);
    }

    private void FireLaser(BeamEffect beam, float width)
    {
        GameplaySfx.Play(GameplaySound.BossLaser);
        ShowBeam(beam, width, LaserColor, LaserFadeTime);
        DamageAlongLine(width, laserDamage, laserHealthDamagePercent, false, 24f);
    }

    public void AnimationEvent_BossSlash()
    {
        if (pattern == null || IsDead || firingLasers || transitioningPhase)
            return;

        GameplaySfx.Play(GameplaySound.BossSlash);
        ShowBeam(slashBeam, SlashWidth, new Color(1f, 0.76f, 0.3f, 0.9f), SlashFadeTime);
        DamageAlongLine(SlashWidth, slashDamage, slashHealthDamagePercent, true, 52f);
    }

    private void PlayCameraImpact(float strength)
    {
        if (PlayerTarget != null && PlayerTarget.TryGetComponent<PlayerHitFeedback>(out var feedback))
            feedback.PlayImpact(strength);
    }

    private void AimLine(Vector3 center, Vector2 direction)
    {
        center.z = transform.position.z;
        Camera camera = Camera.main;
        float reach = 32f;
        if (camera != null && camera.orthographic)
        {
            float viewRadius = camera.orthographicSize * Mathf.Sqrt(1f + camera.aspect * camera.aspect);
            reach = Vector2.Distance(center, camera.transform.position) + viewRadius + 2f;
        }

        lineStart = center - (Vector3)direction * reach;
        lineEnd = center + (Vector3)direction * reach;
    }

    private bool DamageAlongLine(float width, float damage, float healthDamagePercent, bool killEnemies, float impactStrength)
    {
        Vector2 axis = lineEnd - lineStart;
        float angle = Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg;
        Collider2D[] hits = Physics2D.OverlapBoxAll((lineStart + lineEnd) * 0.5f, new Vector2(axis.magnitude, width), angle);
        Player hitPlayer = null;
        slashTargets.Clear();
        foreach (Collider2D hit in hits)
        {
            if (PlayerTarget != null && hit == PlayerTarget.BodyCollider)
                hitPlayer = PlayerTarget;

            if (killEnemies)
            {
                Enemy enemy = hit.GetComponentInParent<Enemy>();
                if (enemy != null && enemy != this)
                    slashTargets.Add(enemy);
            }
        }

        bool damagedPlayer = hitPlayer != null && DamagePlayerAndHeal(hitPlayer, damage, healthDamagePercent) > 0f;
        if (damagedPlayer)
            PlayCameraImpact(impactStrength);

        foreach (Enemy enemy in slashTargets)
        {
            if (enemy != null && enemy.isActiveAndEnabled)
                enemy.KillByBossSlash();
        }
        slashTargets.Clear();
        return damagedPlayer;
    }

    private void CreateAttackLine()
    {
        int count = Mathf.Clamp(phaseLaserCount, 1, 16);
        phaseBeams = new BeamEffect[count];
        phaseLaserCenters = new Vector3[count];
        for (int i = 0; i < phaseBeams.Length; i++)
            phaseBeams[i] = new BeamEffect();

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
            lineMaterial = new Material(shader);
        attackLine = CreateLine("Boss Attack Warning", laserEffectPrefab);
        slashWarning = CreateLine("Boss Slash Warning", slashEffectPrefab);
        primaryBeam.Line = CreateLine("Boss Attack Beam", laserEffectPrefab);
        followUpBeam.Line = CreateLine("Boss Follow-up Beam", laserEffectPrefab);
        slashBeam.Line = CreateLine("Boss Slash Beam", slashEffectPrefab);
        foreach (BeamEffect beam in phaseBeams)
            beam.Line = CreateLine("Boss Phase Beam", laserEffectPrefab);
    }

    private LineRenderer CreateLine(string name, BossAttackEffect prefab)
    {
        if (prefab != null)
        {
            BossAttackEffect effect = Instantiate(prefab, transform);
            effect.name = name;
            LineRenderer lineRenderer = effect.GetComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 2;
            lineRenderer.enabled = false;
            return lineRenderer;
        }

        GameObject line = new GameObject(name);
        line.transform.SetParent(transform, false);
        var renderer = line.AddComponent<LineRenderer>();
        renderer.sharedMaterial = lineMaterial;
        renderer.useWorldSpace = true;
        renderer.positionCount = 2;
        renderer.sortingOrder = 6;
        renderer.enabled = false;
        return renderer;
    }

    private void ShowLine(float width, Color color)
    {
        LineRenderer warning = firingLasers ? attackLine : slashWarning;
        if (warning == null)
            return;

        DrawWarning(warning, width, color);
    }

    private void DrawWarning(LineRenderer line, float width, Color color)
    {
        if (line.TryGetComponent<BossAttackEffect>(out var style))
        {
            width = style.warningWidth;
            color = style.warningColor;
        }
        DrawLine(line, width, color);
    }

    private void ShowBeam(BeamEffect beam, float width, Color color, float duration)
    {
        HideLine();
        if (beam.Line == null)
            return;

        beam.Style = beam.Line.GetComponent<BossAttackEffect>();
        if (beam.Style != null)
        {
            width *= beam.Style.widthMultiplier;
            color = beam.Style.attackColor;
            duration = beam.Style.fadeDuration;
        }
        beam.Width = width;
        beam.Color = color;
        beam.Duration = duration;
        beam.Elapsed = 0f;
        DrawLine(beam.Line, width, color);
    }

    private void DrawLine(LineRenderer line, float width, Color color)
    {
        line.SetPosition(0, lineStart);
        line.SetPosition(1, lineEnd);
        line.widthMultiplier = width;
        line.startColor = color;
        line.endColor = color;
        line.enabled = true;
    }

    private void LateUpdate()
    {
        FadeBeam(primaryBeam);
        FadeBeam(followUpBeam);
        FadeBeam(slashBeam);
        foreach (BeamEffect beam in phaseBeams)
            FadeBeam(beam);
    }

    private static void FadeBeam(BeamEffect beam)
    {
        if (beam.Line == null || !beam.Line.enabled || beam.Duration <= 0f)
            return;

        beam.Elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(beam.Elapsed / beam.Duration);
        float remaining = beam.Style != null ? Mathf.Max(0f, beam.Style.shrinkCurve.Evaluate(progress)) : 1f - progress;
        beam.Line.widthMultiplier = beam.Width * remaining;
        Color color = beam.Color;
        color.a *= remaining;
        beam.Line.startColor = color;
        beam.Line.endColor = color;
        if (progress >= 1f)
            beam.Line.enabled = false;
    }

    private void HideLine()
    {
        if (attackLine != null)
            attackLine.enabled = false;
        if (slashWarning != null)
            slashWarning.enabled = false;
    }

    private void ResetPattern()
    {
        if (hidden)
        {
            hidden = false;
            visual.enabled = true;
            SetCollidersEnabled(true);
        }
        HideLine();
        firingLasers = false;
        followUpArmed = false;
        transitioningPhase = false;
        animator.speed = 1f;
        if (animator.isActiveAndEnabled)
            animator.Play(Idle, 0, 0f);
        patternTimer = timeBetweenPatterns;
        pattern = null;
    }

    protected override void Die(DeathType deathType = DeathType.Normal)
    {
        ClearPhaseVignette();
        CancelPattern();
        base.Die(deathType);
    }

    protected override void OnDisable()
    {
        ReleaseDeathSlowMotion();
        ClearDeathEffects();
        ClearPhaseVignette();
        CancelPattern();
        base.OnDisable();
    }

    protected override void PlayFallbackDeathAnimation()
    {
        StartCoroutine(PlayDeathSequence());
    }

    private IEnumerator PlayDeathSequence()
    {
        previousDeathTimeScale = Time.timeScale;
        if (previousDeathTimeScale > 0f)
        {
            appliedDeathTimeScale = previousDeathTimeScale * deathSlowScale;
            Time.timeScale = appliedDeathTimeScale;
            slowingDeath = true;
        }

        animator.speed = 0f;
        deathFlash = CreateDeathFlash();
        float flashTime = Mathf.Max(0.05f, deathFlashDuration);
        float delay = Mathf.Max(flashTime, deathAnticipation);
        for (float elapsed = 0f; elapsed < delay; elapsed += Time.unscaledDeltaTime)
        {
            deathFlash.color = new Color(1f, 1f, 1f, 0.92f * (1f - Mathf.Clamp01(elapsed / flashTime)));
            yield return null;
        }

        ReleaseDeathSlowMotion();
        Destroy(deathFlashOverlay);
        deathFlashOverlay = null;
        deathFlash = null;
        GameplaySfx.Play(GameplaySound.Explosion);
        Vector3 explosionPosition = visual != null ? visual.bounds.center : transform.position;
        if (visual != null)
            visual.enabled = false;

        if (deathExplosion != null)
        {
            activeDeathExplosion = Instantiate(deathExplosion, explosionPosition, deathExplosion.transform.rotation);
            activeDeathExplosion.transform.SetParent(transform, true);
            foreach (ParticleSystem particles in activeDeathExplosion.GetComponentsInChildren<ParticleSystem>())
            {
                var main = particles.main;
                main.useUnscaledTime = true;
            }
            activeDeathExplosion.Play(true);
            float endTime = Time.unscaledTime + deathExplosionTimeout;
            while (activeDeathExplosion != null && activeDeathExplosion.IsAlive(true) && Time.unscaledTime < endTime)
                yield return null;
        }

        AnimationEvent_DeathComplete();
    }

    private Image CreateDeathFlash()
    {
        deathFlashOverlay = new GameObject("Boss Defeat Flash", typeof(RectTransform), typeof(Canvas));
        SceneManager.MoveGameObjectToScene(deathFlashOverlay, gameObject.scene);
        Canvas canvas = deathFlashOverlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue - 1;

        var panel = new GameObject("Flash", typeof(RectTransform), typeof(Image));
        var image = panel.GetComponent<Image>();
        RectTransform rect = image.rectTransform;
        rect.SetParent(deathFlashOverlay.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        image.raycastTarget = false;
        return image;
    }

    internal void ReleaseDeathSlowMotion()
    {
        if (!slowingDeath)
            return;

        if (Mathf.Approximately(Time.timeScale, appliedDeathTimeScale))
            Time.timeScale = previousDeathTimeScale;
        slowingDeath = false;
    }

    private void ClearDeathEffects()
    {
        if (deathFlashOverlay != null)
            Destroy(deathFlashOverlay);
        if (activeDeathExplosion != null)
            Destroy(activeDeathExplosion.gameObject);
        deathFlash = null;
        deathFlashOverlay = null;
        activeDeathExplosion = null;
    }

    private void ClearPhaseVignette()
    {
        if (phaseFeedback != null)
            phaseFeedback.SetBossPhaseVignette(false);
        phaseFeedback = null;
    }

    private void CancelPattern()
    {
        if (primaryBeam.Line != null)
            primaryBeam.Line.enabled = false;
        if (followUpBeam.Line != null)
            followUpBeam.Line.enabled = false;
        if (slashBeam.Line != null)
            slashBeam.Line.enabled = false;
        foreach (BeamEffect beam in phaseBeams)
        {
            if (beam.Line != null)
                beam.Line.enabled = false;
        }

        if (pattern == null)
            return;

        StopCoroutine(pattern);
        ResetPattern();
    }

    private void OnDestroy()
    {
        if (lineMaterial != null)
            Destroy(lineMaterial);
    }
}

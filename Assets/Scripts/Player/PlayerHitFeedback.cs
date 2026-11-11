using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerHitFeedback : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private Volume hitVolume;
    [SerializeField] private Transform shakePivot;
    [Header("Damage Scaling")]
    [SerializeField, Min(0.01f)] private float referenceDamage = 10f;
    [Tooltip("Normalized URP intensity. 10 damage: 0.13 -> 0.23 by default.")]
    [SerializeField, Min(0f)] private float chromaticIncrease = 0.1f;
    [SerializeField, Min(0f)] private float shakeStrength = 0.15f;
    [SerializeField, Min(0f)] private float maxShakeStrength = 1f;
    [SerializeField, Range(0f, 1f)] private float hitVignetteIntensity = 0.5f;
    [Header("Timing")]
    [SerializeField, Min(0.001f)] private float attackDuration = 0.04f;
    [SerializeField, Min(0.001f)] private float recoveryDuration = 0.3f;

    private VolumeProfile runtimeProfile;
    private bool ownsProfile;
    private ChromaticAberration chromatic;
    private Vignette vignette;
    private float baseChromatic, baseVignette;
    private bool chromaticOverride, vignetteOverride, chromaticActive, vignetteActive;
    private Vector3 shakeOrigin, shakeOffset;
    private float elapsed, chromaticStart, vignetteStart, chromaticPeak, shakePeak, envelope;
    private bool playing;

    public Vector3 WorldShakeOffset => shakePivot != null && shakePivot.parent != null
        ? shakePivot.parent.TransformVector(shakeOffset) : shakeOffset;

    private void Awake()
    {
        if (player == null) player = GetComponent<Player>();
        if (shakePivot != null) shakeOrigin = shakePivot.localPosition;
        InitializeVolume();
    }

    private void OnEnable()
    {
        if (player != null) player.OnDamaged += Play;
    }

    private bool InitializeVolume()
    {
        if (chromatic != null && vignette != null) return true;
        if (hitVolume == null) return false;
        ownsProfile = !hitVolume.HasInstantiatedProfile();
        runtimeProfile = hitVolume.profile; // Runtime component copies; never modify the shared asset.
        if (!runtimeProfile.TryGet(out chromatic)) chromatic = runtimeProfile.Add<ChromaticAberration>();
        if (!runtimeProfile.TryGet(out vignette)) vignette = runtimeProfile.Add<Vignette>();
        baseChromatic = chromatic.intensity.value;
        baseVignette = vignette.intensity.value;
        chromaticOverride = chromatic.intensity.overrideState;
        vignetteOverride = vignette.intensity.overrideState;
        chromaticActive = chromatic.active;
        vignetteActive = vignette.active;
        return true;
    }

    private void Play(float damage)
    {
        float ratio = Mathf.Max(0f, damage) / Mathf.Max(0.01f, referenceDamage);
        bool hasVolume = InitializeVolume();
        chromaticStart = hasVolume ? chromatic.intensity.value : 0f;
        vignetteStart = hasVolume ? vignette.intensity.value : 0f;
        chromaticPeak = Mathf.Clamp01(Mathf.Max(chromaticStart, baseChromatic + chromaticIncrease * ratio));
        shakePeak = Mathf.Min(maxShakeStrength, Mathf.Max(shakePeak * envelope, shakeStrength * ratio));
        elapsed = 0f;
        playing = true;
        if (hasVolume) { chromatic.active = true; vignette.active = true; }
    }

    private void LateUpdate()
    {
        if (!playing) return;
        elapsed += Time.unscaledDeltaTime;
        float attack = Mathf.Max(0.001f, attackDuration);
        float recovery = Mathf.Max(0.001f, recoveryDuration);
        if (elapsed >= attack + recovery) { Restore(); return; }
        float t = elapsed < attack ? Mathf.SmoothStep(0f, 1f, elapsed / attack) :
            1f - Mathf.SmoothStep(0f, 1f, (elapsed - attack) / recovery);
        envelope = t;
        if (chromatic != null && vignette != null)
        {
            float fromChromatic = elapsed < attack ? chromaticStart : baseChromatic;
            float fromVignette = elapsed < attack ? vignetteStart : baseVignette;
            chromatic.intensity.Override(Mathf.Lerp(fromChromatic, chromaticPeak, t));
            // Vignette always peaks at the configured fixed value, independent of damage.
            vignette.intensity.Override(Mathf.Lerp(fromVignette, hitVignetteIntensity, t));
        }
        if (shakePivot != null)
        {
            Vector2 offset = Random.insideUnitCircle * (shakePeak * t);
            shakeOffset = new Vector3(offset.x, offset.y, 0f);
            shakePivot.localPosition = shakeOrigin + shakeOffset;
        }
    }

    private void Restore()
    {
        playing = false;
        envelope = shakePeak = 0f;
        shakeOffset = Vector3.zero;
        if (shakePivot != null) shakePivot.localPosition = shakeOrigin;
        if (chromatic != null)
        {
            chromatic.intensity.value = baseChromatic;
            chromatic.intensity.overrideState = chromaticOverride;
            chromatic.active = chromaticActive;
        }
        if (vignette != null)
        {
            vignette.intensity.value = baseVignette;
            vignette.intensity.overrideState = vignetteOverride;
            vignette.active = vignetteActive;
        }
    }

    private void OnDisable()
    {
        if (player != null) player.OnDamaged -= Play;
        Restore();
    }

    private void OnDestroy()
    {
        if (!ownsProfile || runtimeProfile == null) return;
        if (hitVolume != null && hitVolume.HasInstantiatedProfile() && hitVolume.profile == runtimeProfile)
            hitVolume.profile = null;
        foreach (VolumeComponent component in runtimeProfile.components) Destroy(component);
        Destroy(runtimeProfile);
    }
}

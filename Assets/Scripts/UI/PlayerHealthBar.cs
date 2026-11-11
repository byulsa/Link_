using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerHealthBar : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private Image healthFill;
    [SerializeField] private Image damageTrail;
    [SerializeField, Min(0.01f)] private float healthDuration = 0.16f;
    [SerializeField, Min(0f)] private float trailDelay = 0.22f;
    [SerializeField, Min(0.01f)] private float trailDuration = 0.36f;

    private float target;
    private float healthStart;
    private float trailStart;
    private float elapsed;
    private bool initialized;
    private bool animating;

    private void OnEnable()
    {
        initialized = false;
        animating = false;
    }

    private void LateUpdate()
    {
        if (player == null || healthFill == null || damageTrail == null)
            return;

        float ratio = player.MaxHealth > 0f
            ? Mathf.Clamp01(player.CurrentHealth / player.MaxHealth)
            : 0f;
        if (!initialized || ratio > target)
        {
            initialized = true;
            animating = false;
            target = ratio;
            healthFill.fillAmount = ratio;
            damageTrail.fillAmount = ratio;
            return;
        }

        if (ratio < target)
        {
            target = ratio;
            healthStart = healthFill.fillAmount;
            trailStart = damageTrail.fillAmount;
            elapsed = 0f;
            animating = true;
        }

        if (!animating)
            return;

        elapsed += Time.unscaledDeltaTime;
        float healthProgress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, healthDuration));
        float trailProgress = Mathf.Clamp01((elapsed - trailDelay) / Mathf.Max(0.01f, trailDuration));
        healthFill.fillAmount = Mathf.SmoothStep(healthStart, target, healthProgress);
        damageTrail.fillAmount = Mathf.Max(
            healthFill.fillAmount,
            Mathf.SmoothStep(trailStart, target, trailProgress));
        animating = healthProgress < 1f || trailProgress < 1f;
    }
}

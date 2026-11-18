using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerHealthBar : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private Image healthFill;
    [SerializeField] private Image damageTrail;
    [SerializeField] private TMP_Text healthLabel;
    [SerializeField, Min(0.01f)] private float healthDuration = 0.16f;
    [SerializeField, Min(0f)] private float trailDelay = 0.22f;
    [SerializeField, Min(0.01f)] private float trailDuration = 0.36f;

    private float target;
    private float healthStart;
    private float trailStart;
    private float elapsed;
    private bool initialized;
    private bool animating;
    private int displayedHealth = -1;
    private int displayedMaxHealth = -1;

    private void Awake()
    {
        if (healthLabel != null)
            return;

        GameObject label = new GameObject("HP Value", typeof(RectTransform));
        label.layer = gameObject.layer;
        var rect = (RectTransform)label.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        healthLabel = label.AddComponent<TextMeshProUGUI>();
        healthLabel.alignment = TextAlignmentOptions.Center;
        healthLabel.fontSize = 15f;
        healthLabel.fontStyle = FontStyles.Bold;
        healthLabel.raycastTarget = false;
    }

    private void OnEnable()
    {
        initialized = false;
        animating = false;
    }

    private void LateUpdate()
    {
        if (player == null || healthFill == null || damageTrail == null)
            return;

        int health = Mathf.CeilToInt(player.CurrentHealth);
        int maxHealth = Mathf.CeilToInt(player.MaxHealth);
        if (health != displayedHealth || maxHealth != displayedMaxHealth)
        {
            healthLabel.SetText("HP {0} / {1}", health, maxHealth);
            displayedHealth = health;
            displayedMaxHealth = maxHealth;
        }

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

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BossHealthBar : MonoBehaviour
{
    [SerializeField] private Image healthFill;
    [SerializeField] private Image damageTrail;
    [SerializeField] private Image healingTrail;
    [SerializeField] private TMP_Text label;
    [SerializeField] private GameObject waveDisplay;
    [SerializeField] private string bossName = "유랑자";
    [SerializeField, Min(0.01f)] private float trailDuration = 0.55f;
    [SerializeField, Min(0f)] private float damageDelay = 0.16f;
    [SerializeField, Min(0f)] private float minimumHealWidth = 3f;

    private Enemy boss;
    private Color healColor;
    private float maxHealth;
    private float lastHealth;
    private float damageStart;
    private float damageTime;
    private float healingTime;
    private float deathTime;
    private bool restoreWaveDisplay;

    public void Bind(Enemy target)
    {
        if (healthFill == null || damageTrail == null || healingTrail == null || label == null)
        {
            Debug.LogWarning("Boss HP Bar is missing a UI reference.", this);
            return;
        }

        boss = target;
        maxHealth = Mathf.Max(1f, target.MaxHealth);
        lastHealth = target.CurrentHealth;
        damageTime = healingTime = deathTime = 0f;
        healColor = healingTrail.color;
        healingTrail.enabled = false;
        SetHealth(lastHealth);
        damageStart = damageTrail.fillAmount = healthFill.fillAmount;
        gameObject.SetActive(true);
        if (waveDisplay != null && waveDisplay.activeSelf)
        {
            restoreWaveDisplay = true;
            waveDisplay.SetActive(false);
        }
    }

    public void Hide()
    {
        boss = null;
        if (maxHealth > 0f && healingTrail != null)
            healingTrail.color = healColor;
        maxHealth = 0f;
        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (maxHealth <= 0f)
            return;

        float health = boss != null ? boss.CurrentHealth : 0f;
        float ratio = Mathf.Clamp01(health / maxHealth);
        if (health != lastHealth)
        {
            float previous = Mathf.Clamp01(lastHealth / maxHealth);
            if (health < lastHealth)
            {
                damageStart = Mathf.Max(previous, damageTrail.fillAmount);
                damageTime = trailDuration;
                healingTime = 0f;
            }
            else
            {
                var segment = healingTrail.rectTransform;
                float barWidth = ((RectTransform)segment.parent).rect.width;
                float flashWidth = minimumHealWidth / Mathf.Max(1f, barWidth);
                segment.anchorMin = new Vector2(Mathf.Max(0f, Mathf.Min(previous, ratio - flashWidth)), segment.anchorMin.y);
                segment.anchorMax = new Vector2(ratio, segment.anchorMax.y);
                healingTime = trailDuration;
            }
            SetHealth(health);
            lastHealth = health;
        }

        float delta = Time.unscaledDeltaTime;
        damageTime = Mathf.Max(0f, damageTime - delta);
        healingTime = Mathf.Max(0f, healingTime - delta);
        float delay = Mathf.Min(damageDelay, trailDuration);
        float damageProgress = Mathf.Clamp01((trailDuration - damageTime - delay) / Mathf.Max(0.001f, trailDuration - delay));
        damageTrail.fillAmount = damageTime > 0f
            ? Mathf.Max(ratio, Mathf.SmoothStep(damageStart, ratio, damageProgress))
            : ratio;
        healingTrail.enabled = healingTime > 0f;
        Color tint = healColor;
        tint.a *= healingTrail.enabled ? healingTime / trailDuration : 1f;
        healingTrail.color = tint;

        if (health <= 0f)
        {
            deathTime += delta;
            if (deathTime >= trailDuration)
                Hide();
        }
    }

    private void OnDisable()
    {
        if (restoreWaveDisplay && waveDisplay != null)
            waveDisplay.SetActive(true);
        restoreWaveDisplay = false;
    }

    private void SetHealth(float health)
    {
        healthFill.fillAmount = Mathf.Clamp01(health / maxHealth);
        string name = boss.Data != null ? boss.Data.enemyName : bossName;
        label.text = $"{name} HP  {Mathf.CeilToInt(health)} / {Mathf.CeilToInt(maxHealth)}";
    }
}

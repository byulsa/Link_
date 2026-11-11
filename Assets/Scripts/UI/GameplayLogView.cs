using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayLogView : MonoBehaviour
{
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private TMP_Text rewardLogText;
    [SerializeField] private ScrollRect codingLogScroll;
    [SerializeField] private TMP_Text codingLogText;
    [SerializeField] private Font logFontSource;
    [SerializeField, Min(0f)] private float rewardHoldSeconds = 3f;
    [SerializeField, Min(0.01f)] private float rewardFadeSeconds = 0.75f;

    private TMP_FontAsset logFont;
    private string shownRewardLog;
    private float rewardFadeStart;

    private void OnEnable()
    {
        if (waveManager == null)
            return;

        waveManager.LogChanged += Render;
        Render();
    }

    private void OnDisable()
    {
        if (waveManager != null)
            waveManager.LogChanged -= Render;
    }

    private void Render()
    {
        if (logFont == null && logFontSource != null &&
            (waveManager.IsCodingLogUnlocked || !string.IsNullOrEmpty(waveManager.LastRewardLog)))
        {
            logFont = TMP_FontAsset.CreateFontAsset(logFontSource);
            if (logFont != null && rewardLogText != null)
                rewardLogText.font = logFont;
            if (logFont != null && codingLogText != null)
                codingLogText.font = logFont;
        }

        if (rewardLogText != null && shownRewardLog != waveManager.LastRewardLog)
        {
            shownRewardLog = waveManager.LastRewardLog;
            rewardLogText.text = waveManager.LastRewardLog;
            rewardFadeStart = Time.unscaledTime + rewardHoldSeconds;
            UpdateRewardFade();
        }

        bool unlocked = waveManager.IsCodingLogUnlocked;
        if (codingLogScroll != null)
            codingLogScroll.gameObject.SetActive(unlocked);

        if (!unlocked || codingLogText == null)
            return;

        codingLogText.text = waveManager.CodingLog.Count == 0
            ? "현재 문법 오류가 없습니다."
            : string.Join("\n\n", waveManager.CodingLog);
        if (codingLogScroll == null || !codingLogScroll.isActiveAndEnabled)
            return;

        Canvas.ForceUpdateCanvases();
        codingLogScroll.StopMovement();
        codingLogScroll.verticalNormalizedPosition = 0f;
    }

    private void Update()
    {
        if (rewardLogText != null && rewardLogText.alpha > 0f && !string.IsNullOrEmpty(shownRewardLog))
            UpdateRewardFade();
    }

    private void UpdateRewardFade()
    {
        rewardLogText.alpha = string.IsNullOrEmpty(shownRewardLog)
            ? 0f
            : 1f - Mathf.Clamp01((Time.unscaledTime - rewardFadeStart) / Mathf.Max(0.01f, rewardFadeSeconds));
    }

    private void OnDestroy()
    {
        if (logFont != null)
            Destroy(logFont);
    }
}

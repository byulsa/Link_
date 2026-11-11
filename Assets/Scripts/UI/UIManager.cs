using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI pointText;
    [SerializeField] private TextMeshProUGUI waveText;

    private void OnEnable()
    {
        WaveManager.ProgressChanged += UpdateWaveUI;
        UpdateWaveUI();
    }

    private void OnDisable()
    {
        WaveManager.ProgressChanged -= UpdateWaveUI;
    }

    private void OnDestroy()
    {
        if (PointManager.Instance != null) PointManager.Instance.OnPointChanged -= UpdateUI;
    }

    private void UpdateWaveUI()
    {
        WaveManager manager = WaveManager.Instance;
        if (waveText == null || manager == null) return;
        string state = manager.IsChapterComplete ? "  CLEAR" :
            (!WaveManager.IsBattleActive && manager.CurrentWaveNumber > 0 ? "  COMPLETE" : "");
        waveText.text = $"CHAPTER {manager.CurrentChapterName}  |  WAVE {manager.CurrentWaveNumber:00} / {manager.TotalWaves:00}{state}";
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        if (PointManager.Instance != null)
        {
            PointManager.Instance.OnPointChanged += UpdateUI;
            UpdateUI(PointManager.Instance.CurrentPoint);
        }
        UpdateWaveUI();
    }

    private void UpdateUI(int point)
    {
        if (pointText != null) pointText.text = $"{point}";
    }
}

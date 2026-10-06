using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [SerializeField] private ChapterData chapterData;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private DataCollectionUI dataCollectionUI;

    private int currentWaveIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        StartWave(0);
    }

    public void StartWave(int index)
    {
        if (chapterData == null || chapterData.waves == null)
            return;

        if (index < 0 || index >= chapterData.waves.Count)
        {
            CompleteChapter();
            return;
        }

        currentWaveIndex = index;

        WaveData wave = chapterData.waves[index];

        enemySpawner.StartWave(wave);
    }

    public void OnWaveEnded()
    {
        if (dataCollectionUI != null)
            dataCollectionUI.Open();
    }

    public void NextWave()
    {
        StartWave(currentWaveIndex + 1);
    }

    private void CompleteChapter()
    {
        Debug.Log("[WaveManager] 챕터 종료");
    }
}
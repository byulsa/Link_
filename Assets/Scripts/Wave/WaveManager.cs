using System;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    private enum IntermissionStep { None, CodeCollection, Shop, CodeEditor }
    private const DialogueReward UnlockableFeatures =
        DialogueReward.UnlockCodingLog | DialogueReward.UnlockShop | DialogueReward.UnlockCodeCollection;

    public static WaveManager Instance { get; private set; }

    [SerializeField] private ChapterData chapterData;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private DataCollectionUI dataCollectionUI;
    [SerializeField] private CodePanel codePanel;
    [SerializeField] private Font dialogueDefaultFont;
    [SerializeField] private WaveDialogueView dialogueView;

    [Header("Feature Unlocks")]
    [SerializeField] private DialogueReward initiallyUnlockedFeatures =
        DialogueReward.UnlockShop | DialogueReward.UnlockCodeCollection;

    [Header("Enemy Stat Growth")]
    [SerializeField, Range(1f, 1.3f)] private float waveStatGrowth = 1.15f;
    [SerializeField, Min(1f)] private float chapterStatGrowth = 1.5f;
    [SerializeField] private List<ChapterData> followingChapters = new List<ChapterData>();
    private int nextChapterIndex;
    private float chapterStartMultiplier = 1f;
    public int CurrentChapterNumber { get; private set; } = 1;
    public float CurrentStatMultiplier { get; private set; } = 1f;

    private int currentWaveIndex = -1;
    private IntermissionStep intermissionStep;
    public bool CanEditCode => intermissionStep == IntermissionStep.CodeEditor && !IsBattleActive && !IsDialoguePlaying;
    private const int CodingLogLimit = 9;
    private readonly Queue<string> codingLog = new Queue<string>();
    private readonly List<string> activeCodingErrors = new List<string>();
    public event Action LogChanged;
    private DialogueReward unlockedFeatures;
    public bool IsCodingLogUnlocked => (unlockedFeatures & DialogueReward.UnlockCodingLog) != 0;
    public bool IsShopUnlocked => (unlockedFeatures & DialogueReward.UnlockShop) != 0;
    public bool IsCodeCollectionUnlocked => (unlockedFeatures & DialogueReward.UnlockCodeCollection) != 0;
    public string LastRewardLog { get; private set; } = string.Empty;
    public IReadOnlyCollection<string> CodingLog => codingLog;
    public static event Action ProgressChanged;
    public static event Action WaveStarted;
    public int CurrentWaveNumber => currentWaveIndex + 1;
    public int TotalWaves => chapterData != null && chapterData.waves != null ? chapterData.waves.Count : 0;
    public string CurrentChapterName => chapterData != null ? chapterData.chapterName : string.Empty;
    public bool IsChapterComplete { get; private set; }
    public static bool IsBattleActive { get; private set; }
    private bool IsDialoguePlaying => dialogueView != null && dialogueView.IsPlaying;
    private WaveData CurrentWave => currentWaveIndex >= 0 && currentWaveIndex < TotalWaves
        ? chapterData.waves[currentWaveIndex] : null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        IsBattleActive = false;
        unlockedFeatures = initiallyUnlockedFeatures;
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        Instance = null;
        IsBattleActive = false;
    }

    private void Start()
    {
        StartWave(0);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.F8) || Time.timeScale == 0f || enemySpawner == null || TotalWaves == 0)
            return;

        int bossWaveIndex = chapterData.waves.FindIndex(wave => wave != null && wave.boss != null);
        if (bossWaveIndex < 0 || (IsBattleActive && currentWaveIndex == bossWaveIndex))
            return;

        dialogueView?.Cancel();
        dataCollectionUI?.CloseForBossTest();
        codePanel?.SetOpen(false);
        intermissionStep = IntermissionStep.None;
        IsBattleActive = false;
        enemySpawner.ResetForBossTest();
        FindAnyObjectByType<Player>()?.ResetHealthForBossTest();
        StartWave(bossWaveIndex);
    }
#endif

    public void StartWave(int index)
    {
        if (chapterData == null || chapterData.waves == null)
        {
            IsBattleActive = false;
            return;
        }

        if (index < 0 || IsBattleActive || IsDialoguePlaying || intermissionStep != IntermissionStep.None) return;

        if (index >= chapterData.waves.Count)
        {
            CompleteChapter();
            return;
        }

        WaveData wave = chapterData.waves[index];
        if (wave == null || enemySpawner == null)
        {
            Debug.LogError("[WaveManager] Wave data or EnemySpawner is missing.", this);
            return;
        }
        IsChapterComplete = false;
        currentWaveIndex = index;
        IsBattleActive = true;

        CurrentStatMultiplier = chapterStartMultiplier * Mathf.Pow(Mathf.Clamp(waveStatGrowth, 1f, 1.3f), index);
        WaveStarted?.Invoke();
        enemySpawner.StartWave(wave, CurrentStatMultiplier);
        GameplaySfx.Play(GameplaySound.WaveStart);
        ProgressChanged?.Invoke();
    }

    public void OnWaveEnded()
    {
        if (!IsBattleActive) return;
        IsBattleActive = false;
        ProgressChanged?.Invoke();
        PlayDialogueThen(CurrentWave?.dialogueAfterWave, OpenCodeCollection);
    }

    private void OpenCodeCollection()
    {
        if (dataCollectionUI == null || !IsCodeCollectionUnlocked)
        {
            if (dataCollectionUI != null)
                dataCollectionUI.ConvertPendingRewardsToPoints();

            OpenShop();
            return;
        }

        intermissionStep = IntermissionStep.CodeCollection;
        dataCollectionUI.Open();
    }

    public bool TryFinishCodeCollection()
    {
        if (intermissionStep != IntermissionStep.CodeCollection)
            return false;

        intermissionStep = IntermissionStep.None;
        PlayDialogueThen(CurrentWave?.dialogueAfterCodeCollection, OpenShop);
        return true;
    }

    private void OpenShop()
    {
        if (IsShopUnlocked && dataCollectionUI != null && dataCollectionUI.TryOpenShop())
            intermissionStep = IntermissionStep.Shop;
        else
            OpenCodeEditor();
    }

    public bool TryFinishShop()
    {
        if (intermissionStep != IntermissionStep.Shop)
            return false;

        intermissionStep = IntermissionStep.None;
        PlayDialogueThen(CurrentWave?.dialogueAfterShop, OpenCodeEditor);
        return true;
    }

    private void OpenCodeEditor()
    {
        if (codePanel == null)
        {
            AdvanceWave();
            return;
        }

        intermissionStep = IntermissionStep.CodeEditor;
        codePanel.SetOpen(true);
    }

    public void NextWave()
    {
        if (IsBattleActive || IsChapterComplete || IsDialoguePlaying)
            return;

        if (intermissionStep == IntermissionStep.Shop)
            codePanel?.CloseShop();
        else if (intermissionStep == IntermissionStep.CodeEditor)
            codePanel?.CloseCodeEditor();
    }

    public void OnCodeEditorClosed()
    {
        if (intermissionStep != IntermissionStep.CodeEditor)
            return;

        intermissionStep = IntermissionStep.None;
        PlayDialogueThen(CurrentWave?.dialogueAfterCodeEditor, AdvanceWave);
    }

    private void PlayDialogueThen(WaveDialogue dialogue, Action continuation)
    {
        if (dialogue != null && dialogue.lines != null &&
            Array.Exists(dialogue.lines, line => !string.IsNullOrWhiteSpace(line?.text)))
        {
            if (dialogueView != null && dialogueView.TryPlay(dialogue, dialogueDefaultFont, () =>
            {
                GrantDialogueReward(dialogue.completionReward);
                continuation();
            }))
                return;

            Debug.LogWarning("[WaveManager] Connect a configured Dialogue View to play this wave's dialogue.", this);
        }

        continuation();
    }

    private void GrantDialogueReward(DialogueReward reward)
    {
        DialogueReward newlyUnlocked = reward & UnlockableFeatures & ~unlockedFeatures;
        if (newlyUnlocked == DialogueReward.None)
            return;

        unlockedFeatures |= newlyUnlocked;
        var names = new List<string>();
        if ((newlyUnlocked & DialogueReward.UnlockCodingLog) != 0)
            names.Add("코딩 로그");
        if ((newlyUnlocked & DialogueReward.UnlockShop) != 0)
            names.Add("상점");
        if ((newlyUnlocked & DialogueReward.UnlockCodeCollection) != 0)
            names.Add("코드 수집창");

        LastRewardLog = $"[보상] 챕터 {CurrentChapterNumber} · 웨이브 {CurrentWaveNumber} 대사 완료 — {string.Join(", ", names)} 해금";
        Debug.Log(LastRewardLog, this);
        LogChanged?.Invoke();
    }

    public void UpdateCodingErrors(IReadOnlyList<string> errors)
    {
        if (!IsCodingLogUnlocked)
            return;

        var newErrors = new HashSet<string>(errors);
        int removedCount = activeCodingErrors.RemoveAll(error => !newErrors.Remove(error));
        if (removedCount == 0 && newErrors.Count == 0)
            return;

        foreach (string error in errors)
        {
            if (newErrors.Remove(error))
                activeCodingErrors.Add(error);
        }

        codingLog.Clear();
        for (int i = Mathf.Max(0, activeCodingErrors.Count - CodingLogLimit); i < activeCodingErrors.Count; i++)
            codingLog.Enqueue(activeCodingErrors[i]);

        LogChanged?.Invoke();
    }

    private void AdvanceWave()
    {
        if (currentWaveIndex + 1 < TotalWaves)
        {
            StartWave(currentWaveIndex + 1);
            return;
        }
        CompleteChapter();
        if (followingChapters != null && nextChapterIndex < followingChapters.Count)
        {
            ChapterData nextChapter = followingChapters[nextChapterIndex];
            if (StartNextChapter(nextChapter)) nextChapterIndex++;
        }
    }

    public bool StartNextChapter(ChapterData nextChapter)
    {
        if (IsBattleActive || IsDialoguePlaying || !IsChapterComplete || nextChapter == null ||
            nextChapter.waves == null || nextChapter.waves.Count == 0 ||
            nextChapter.waves[0] == null || enemySpawner == null) return false;
        // Carry the previous chapter's final stats forward; do not reset to base stats.
        chapterStartMultiplier = CurrentStatMultiplier * Mathf.Max(1f, chapterStatGrowth);
        chapterData = nextChapter;
        CurrentChapterNumber++;
        unlockedFeatures |= DialogueReward.UnlockShop;
        currentWaveIndex = -1;
        IsChapterComplete = false;
        StartWave(0);
        return true;
    }

    private void CompleteChapter()
    {
        IsChapterComplete = true;
        ProgressChanged?.Invoke();
        IsBattleActive = false;
        if (codePanel != null)
            codePanel.SetOpen(false);

        Debug.Log("[WaveManager] 챕터 종료");
    }
}

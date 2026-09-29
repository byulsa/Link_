using System;
using System.Collections.Generic;
using UnityEngine;

// Wave 동안 획득한 Code를 임시로 보관하고,
// DATA COLLECTION 화면에서의 선택 결과를 처리하는 게임 로직 담당 클래스.
// UI(DataCollectionUI, RewardSlotUI)는 이 클래스의 데이터/메서드만 참조한다.
public class WaveRewardManager : MonoBehaviour
{
    public static WaveRewardManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private CodeGrid grid;
    [SerializeField] private CodeEditor editor;

    [Header("Settings")]
    [SerializeField, Min(1)] private int maxRewardsPerWave = 8;
    [SerializeField, Min(0)] private int pointPerUnselectedCode = 10;

    public class RewardEntry
    {
        public BlockDefinition Block;
        public int Value;
        public bool IsSelected;
    }

    private readonly List<RewardEntry> currentRewards = new();

    public IReadOnlyList<RewardEntry> CurrentRewards => currentRewards;

    // acquiredCount / 2, 최소 1 (보상이 하나도 없으면 0)
    public int MaxSelectableCount =>
        currentRewards.Count > 0 ? Mathf.Max(1, currentRewards.Count / 2) : 0;

    public int SelectedCount
    {
        get
        {
            int count = 0;

            foreach (RewardEntry entry in currentRewards)
            {
                if (entry.IsSelected)
                    count++;
            }

            return count;
        }
    }

    public event Action OnRewardAdded;
    public event Action OnSelectionChanged;
    public event Action OnRewardsConfirmed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // CodeDropManager 등 Code를 실제로 획득하는 지점에서 호출한다.
    public void AddReward(BlockDefinition block, int value)
    {
        if (block == null)
            return;

        if (currentRewards.Count >= maxRewardsPerWave)
        {
            Debug.Log(
                $"[WaveRewardManager] 최대 보상 개수({maxRewardsPerWave}) 초과, 추가 무시: {block.displayText}"
            );

            return;
        }

        currentRewards.Add(new RewardEntry
        {
            Block = block,
            Value = value,
            IsSelected = false
        });

        OnRewardAdded?.Invoke();
    }

    // 슬롯 선택/해제. 최대 선택 개수를 넘으면 false를 반환한다.
    public bool ToggleSelect(int index)
    {
        if (index < 0 || index >= currentRewards.Count)
            return false;

        RewardEntry entry = currentRewards[index];

        if (!entry.IsSelected && SelectedCount >= MaxSelectableCount)
        {
            // 최대 선택 개수 초과 - 선택 불가
            return false;
        }

        entry.IsSelected = !entry.IsSelected;

        OnSelectionChanged?.Invoke();

        return true;
    }

    // Confirm 처리:
    // 1) 선택된 Code -> CodeEditor를 통해 실제 배치
    // 2) 선택되지 않은 Code -> Point로 환산하여 PointManager에 지급
    public void ConfirmSelection()
    {
        int unselectedCount = 0;

        foreach (RewardEntry entry in currentRewards)
        {
            if (entry.IsSelected)
            {
                if (!TryPlaceBlock(entry))
                {
                    // 배치할 공간이 없는 경우, 중복 지급 없이 Point로 대체 환산한다.
                    unselectedCount++;
                }
            }
            else
            {
                unselectedCount++;
            }
        }

        if (unselectedCount > 0 && PointManager.Instance != null)
        {
            int points = unselectedCount * pointPerUnselectedCode;
            PointManager.Instance.AddPoint(points);
        }

        currentRewards.Clear();

        OnRewardsConfirmed?.Invoke();
    }

    private bool TryPlaceBlock(RewardEntry entry)
    {
        if (grid == null || editor == null)
            return false;

        int blockWidth = GetBlockWidth(entry.Block, entry.Value);

        if (!grid.TryFindEmptyPosition(blockWidth, out Vector2Int position))
        {
            Debug.Log(
                $"[WaveRewardManager] 공간 부족으로 배치 실패: {entry.Block.displayText}"
            );

            return false;
        }

        editor.CreateDropBlock(entry.Block, position, entry.Value);

        return true;
    }

    private int GetBlockWidth(BlockDefinition definition, int value)
    {
        if (!definition.hasValue)
            return definition.displayText.Length;

        return definition.displayText.Length + value.ToString().Length;
    }
}

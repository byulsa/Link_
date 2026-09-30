using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// DATA COLLECTION 화면의 표시/입력만 담당한다.
// 실제 선택 규칙, Point 환산 등의 로직은 WaveRewardManager가 담당한다.
public class DataCollectionUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private WaveRewardManager rewardManager;

    [SerializeField]
    private RewardSlotUI slotPrefab;

    [SerializeField]
    private Transform slotContainer;

    [Header("UI")]
    [SerializeField]
    private GameObject panelRoot;

    [SerializeField]
    private TMP_Text selectionCountText;

    [SerializeField]
    private Button confirmButton;

    [Header("Shop 연결 (선택)")]
    [SerializeField]
    private GameObject shopPanel;

    private readonly List<RewardSlotUI> spawnedSlots = new();

    private void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnClickConfirm);
    }

    // WaveManager가 아직 완성되지 않은 프로토타입 단계에서
    // 전투 시스템과 별개로 DATA COLLECTION 화면을 테스트하기 위한 메서드.
    public void TestOpenDataCollection()
    {
        Open();
    }

    public void Open()
    {
        if (rewardManager == null)
            return;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        RenderSlots();
        UpdateSelectionText();
    }

    public void Close()
    {
        ClearSlots();

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void RenderSlots()
    {
        ClearSlots();

        IReadOnlyList<WaveRewardManager.RewardEntry> rewards = rewardManager.CurrentRewards;

        for (int i = 0; i < rewards.Count; i++)
        {
            RewardSlotUI slotUI = Instantiate(slotPrefab, slotContainer);
            slotUI.Setup(i, rewards[i], this);
            spawnedSlots.Add(slotUI);
        }
    }

    private void ClearSlots()
    {
        foreach (RewardSlotUI slot in spawnedSlots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }

        spawnedSlots.Clear();
    }

    // RewardSlotUI에서 클릭 시 호출된다.
    public void OnSlotClicked(int index)
    {
        if (!rewardManager.ToggleSelect(index))
            return;

        if (index >= 0 && index < spawnedSlots.Count)
        {
            bool isSelected = rewardManager.CurrentRewards[index].IsSelected;
            spawnedSlots[index].SetSelected(isSelected);
        }

        UpdateSelectionText();
    }

    private void UpdateSelectionText()
    {
        if (selectionCountText == null)
            return;

        selectionCountText.text =
            $"{rewardManager.SelectedCount} / {rewardManager.MaxSelectableCount}";
    }

    private void OnClickConfirm()
    {
        rewardManager.ConfirmSelection();

        Close();

        if (shopPanel != null)
            shopPanel.SetActive(true);
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 개별 Code 보상 슬롯의 표시/선택 상태를 담당한다.
// Shop 슬롯처럼 균일한 사각형이 아니라, 실제 CodeBlock과 같은 방식(글자 수 x cellSize)으로
// 너비가 정해지고, 카테고리별 색상도 CodeBlock과 동일하게 적용해서 "진짜 코드"처럼 보이게 한다.
public class RewardSlotUI : MonoBehaviour
{
    [SerializeField]
    private TMP_Text blockText;

    [SerializeField]
    private Button button;

    // 선택 표시는 CodeBlock의 에러 표시(Outline)와 같은 방식을 사용한다.
    // 배경 오버레이를 덮는 대신, 코드 블록 자체의 테두리를 강조하는 형태.
    [SerializeField]
    private Outline selectedOutline;

    [Header("Sizing")]
    [Tooltip(
        "CodeGrid의 Cell Size와 동일하게 맞추면, 실제로 CodeGrid에 배치됐을 때와 같은 크기로 보인다."
    )]
    [SerializeField]
    private float cellSize = 75f;

    [SerializeField]
    private RectTransform rectTransform;
    private int slotIndex;
    private DataCollectionUI dataCollectionUI;

    private void Awake()
    {
        //rectTransform = GetComponent<RectTransform>();

        if (selectedOutline == null)
            selectedOutline = GetComponent<Outline>();

        SetSelected(false);
    }

    public void Setup(int index, WaveRewardManager.RewardEntry entry, DataCollectionUI ui)
    {
        slotIndex = index;
        dataCollectionUI = ui;

        string displayText = GetDisplayText(entry);

        if (blockText != null)
        {
            blockText.text = displayText;

            // CodeBlock과 동일한 규칙: 카테고리(구조/동작/역할)에 따라 글자 색을 다르게 표시.
            blockText.color = BlockCategoryColor.GetColor(entry.Block.category);
        }

        UpdateSize(displayText.Length);

        SetSelected(entry.IsSelected);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
    }

    private string GetDisplayText(WaveRewardManager.RewardEntry entry)
    {
        return entry.Block.hasValue
            ? $"{entry.Block.displayText}{entry.Value}"
            : entry.Block.displayText;
    }

    private void UpdateSize(int textLength)
    {
        if (rectTransform == null)
            return;

        rectTransform.sizeDelta = new Vector2(textLength * cellSize, cellSize);
        blockText.rectTransform.sizeDelta = new Vector2(textLength * cellSize, cellSize);
    }

    private void OnClick()
    {
        dataCollectionUI.OnSlotClicked(slotIndex);
    }

    public void SetSelected(bool selected)
    {
        if (selectedOutline != null)
            selectedOutline.enabled = selected;
    }
}

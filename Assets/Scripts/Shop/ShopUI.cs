using System.Collections.Generic;
using System.Collections;
using TMPro;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    [SerializeField] private ShopManager shopManager;

    [Header("고정 슬롯 (씬에 미리 배치된 ShopSlotUI)")]
    [SerializeField] private List<ShopSlotUI> topSlotUIs;    // 상단 5개 (코드 블록 + 패시브)
    [SerializeField] private List<ShopSlotUI> weaponSlotUIs; // 하단 2개 (무기 고정)

    [Header("대사 (DATA EXCHANGE 캐릭터)")]
    [SerializeField] private Image characterImage;
    [SerializeField] Sprite[] characterSprite;//0 = 구매할때, 1 = 구매 실패
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private string defaultLine = "실험이 종료되었어요. 'P'는 많이 얻으셨나요?";
    [SerializeField] private string[] purchaseLines;
    [SerializeField] private string[] FailedSoldLines;
    [SerializeField, Min(0f)] private float characterDelay = 0.01f;
    [SerializeField] private RectTransform dialogueShakeTarget;

    private Tween dialogueTween;
    private Tween dialogueShakeTween;
    private Vector2 dialogueOriginPosition;
    private bool hasDialogueOriginPosition;

    private void OnEnable()
    {
        shopManager.OnShopRefreshed += RenderSlots;
        shopManager.OnPurchaseSucceeded += OnPurchaseSucceeded;
        shopManager.OnPurchaseFailed += OnPurchaseFailed;

        if (dialogueText != null)
            ShowLine(defaultLine);

        shopManager.RefreshShop();
    }

    private void OnDisable()
    {
        dialogueTween?.Kill();
        dialogueTween = null;

        dialogueShakeTween?.Kill();
        dialogueShakeTween = null;

        if (dialogueShakeTarget != null && hasDialogueOriginPosition)
            dialogueShakeTarget.anchoredPosition = dialogueOriginPosition;

        hasDialogueOriginPosition = false;

        shopManager.OnShopRefreshed -= RenderSlots;
        shopManager.OnPurchaseSucceeded -= OnPurchaseSucceeded;
        shopManager.OnPurchaseFailed -= OnPurchaseFailed;
    }

    private void RenderSlots()
    {
        IReadOnlyList<ShopSlot> slots = shopManager.CurrentSlots;

        for (int i = 0; i < topSlotUIs.Count; i++)
        {
            ApplySlot(topSlotUIs[i], i, slots);
        }

        int weaponStart = topSlotUIs.Count;

        for (int i = 0; i < weaponSlotUIs.Count; i++)
        {
            ApplySlot(weaponSlotUIs[i], weaponStart + i, slots);
        }
    }

    private void ApplySlot(ShopSlotUI slotUI, int slotIndex, IReadOnlyList<ShopSlot> slots)
    {
        if (slotUI == null) return;

        if (slotIndex < slots.Count)
        {
            slotUI.gameObject.SetActive(true);
            slotUI.Setup(slotIndex, slots[slotIndex], shopManager);
        }
        else
        {
            // 드랍테이블에 항목이 부족해 못 채운 칸은 비워둠
            slotUI.gameObject.SetActive(false);
        }
        ShowLine(defaultLine);
    }

    private ShopSlotUI GetSlotUI(int slotIndex)
    {
        if (slotIndex < topSlotUIs.Count)
            return topSlotUIs[slotIndex];

        int weaponIndex = slotIndex - topSlotUIs.Count;

        if (weaponIndex >= 0 && weaponIndex < weaponSlotUIs.Count)
            return weaponSlotUIs[weaponIndex];

        return null;
    }

    private void OnPurchaseSucceeded(int slotIndex)
    {
        ShopSlotUI slotUI = GetSlotUI(slotIndex);

        if (slotUI != null)
            slotUI.SetSold(true);

        ShowRandomLine();
        StartCoroutine(characterEmotion(0)); // 구매 성공 시 캐릭터 감정 변화
        // TODO: 구매 애니메이션 트리거는 나중에 여기 연결
    }

    private void OnPurchaseFailed(int slotIndex)
    {
        Debug.Log($"[ShopUI] 구매 실패: slot {slotIndex}");
        ShowFailedRandomLine();
        StartCoroutine(characterEmotion(1)); // 구매 실패 시 캐릭터 감정 변화
    }

    private void ShowRandomLine()
    {
        if (dialogueText == null || purchaseLines == null || purchaseLines.Length == 0)
            return;

        ShowLine(purchaseLines[Random.Range(0, purchaseLines.Length)]);
    }

    private void ShowFailedRandomLine()
    {
        if (dialogueText == null || FailedSoldLines == null || FailedSoldLines.Length == 0)
            return;

        ShowLine(FailedSoldLines[Random.Range(0, FailedSoldLines.Length)]);
        FailedSoldShake();
    }
    public void FailedSoldShake()
    {
        if (dialogueShakeTarget == null)
            return;

        if (dialogueShakeTween != null && dialogueShakeTween.IsActive())
        {
            dialogueShakeTween.Kill();
            dialogueShakeTarget.anchoredPosition = Vector2.zero;
        }

        dialogueShakeTween = dialogueShakeTarget
            .DOShakeAnchorPos(0.2f, new Vector2(20f, 0f), 40, 90f)
            .OnComplete(() =>
            {
                dialogueShakeTarget.anchoredPosition = Vector2.zero;
                dialogueShakeTween = null;
            });
    }

    private void ShowLine(string line)
    {
        dialogueTween?.Kill();
        dialogueText.text = line;
        dialogueText.ForceMeshUpdate(true, true);

        int characterCount = dialogueText.textInfo.characterCount;
        if (characterCount == 0 || characterDelay <= 0f)
        {
            dialogueText.maxVisibleCharacters = characterCount;
            return;
        }

        const int initialVisibleCharacters = 1;
        dialogueText.maxVisibleCharacters = initialVisibleCharacters;

        if (characterCount <= initialVisibleCharacters)
            return;

        dialogueTween = DOTween.To(
                () => initialVisibleCharacters,
                visibleCharacters => dialogueText.maxVisibleCharacters = visibleCharacters,
                characterCount,
                (characterCount - initialVisibleCharacters) * characterDelay
            )
            .SetEase(Ease.Linear)
            .SetTarget(dialogueText);
    }
    private IEnumerator characterEmotion(int characterCount)//0 = 구매할때, 1 = 구매 실패
    {
        characterImage.sprite = characterSprite[characterCount];
        yield return new WaitForSeconds(0.5f);
        //:후에 돌아오는 로직
    }
}
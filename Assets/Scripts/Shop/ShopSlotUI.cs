using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler
{
    [Header("Common")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button buyButton;
    [SerializeField] private GameObject soldOverlay;

    [Header("Preview")]
    [SerializeField] private GameObject itemPreview;
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject codePreview;
    [SerializeField] private CodeBlock codeBlock;

    [Header("Price Color")]
    [SerializeField] private Color affordableColor = Color.black;
    [SerializeField] private Color unaffordableColor = Color.red;

    private ShopSlot slot;
    private int slotIndex;
    private ShopManager shopManager;
    private RectTransform nameTextRect;
    private Vector2 originalNameAnchorMin;
    private Vector2 originalNameAnchorMax;
    private Vector2 originalNameOffsetMin;
    private Vector2 originalNameOffsetMax;
    private TextAlignmentOptions originalNameAlignment;

    private void Awake()
    {
        if (nameText == null)
            return;

        nameTextRect = nameText.rectTransform;
        originalNameAnchorMin = nameTextRect.anchorMin;
        originalNameAnchorMax = nameTextRect.anchorMax;
        originalNameOffsetMin = nameTextRect.offsetMin;
        originalNameOffsetMax = nameTextRect.offsetMax;
        originalNameAlignment = nameText.alignment;
    }

    private void OnEnable()
    {
        if (PointManager.Instance != null)
            PointManager.Instance.OnPointChanged += UpdatePriceColor;
    }

    private void OnDisable()
    {
        if (PointManager.Instance != null)
            PointManager.Instance.OnPointChanged -= UpdatePriceColor;
    }

    public void Setup(int index, ShopSlot slot, ShopManager manager)
    {
        this.slot = slot;
        slotIndex = index;
        shopManager = manager;

        if (nameText != null)
            nameText.text = slot.GetDisplayName();

        switch (slot.Type)
        {
            case ShopItemType.Block:
                SetupCode();
                break;
            case ShopItemType.Weapon:
            case ShopItemType.Passive:
                SetupImageItem();
                break;
        }

        if (priceText != null)
            priceText.text = $"{slot.Price} P";
        SetSold(slot.IsSold);

        if (PointManager.Instance != null)
            UpdatePriceColor(PointManager.Instance.CurrentPoint);

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnClickBuy);
        }
    }

    private void SetupCode()
    {
        SetNameTextArea(false);

        if (itemPreview != null)
            itemPreview.SetActive(false);

        if (codePreview != null)
            codePreview.SetActive(true);

        if (codeBlock == null)
        {
            Debug.LogError("[ShopSlotUI] CodeBlock이 설정되지 않았습니다.");
            return;
        }

        codeBlock.gameObject.SetActive(true);
        codeBlock.InitializePreview(slot.Block, slot.Value, 75f);
    }

    private void SetupImageItem()
    {
        SetNameTextArea(true);

        if (itemPreview != null)
            itemPreview.SetActive(true);

        if (codePreview != null)
            codePreview.SetActive(false);

        if (iconImage != null)
        {
            iconImage.sprite = slot.GetIcon();
            iconImage.preserveAspect = true;
        }
    }

    private void SetNameTextArea(bool reserveIconSpace)
    {
        if (nameTextRect == null)
            return;

        nameTextRect.anchorMin = reserveIconSpace
            ? new Vector2(0.3f, originalNameAnchorMin.y)
            : originalNameAnchorMin;
        nameTextRect.anchorMax = originalNameAnchorMax;
        nameTextRect.offsetMin = reserveIconSpace
            ? new Vector2(0f, originalNameOffsetMin.y)
            : originalNameOffsetMin;
        nameTextRect.offsetMax = originalNameOffsetMax;
        nameText.alignment = reserveIconSpace
            ? TextAlignmentOptions.MidlineLeft
            : originalNameAlignment;
    }

    private void UpdatePriceColor(int currentPoint)
    {
        if (slot == null)
            return;

        if (priceText != null)
            priceText.color = currentPoint >= slot.Price ? affordableColor : unaffordableColor;
    }

    private void OnClickBuy()
    {
        if (shopManager.TryPurchase(slotIndex))
            SetSold(true);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (slot == null || slot.IsSold)
            return;

        if (slot.Type == ShopItemType.Weapon)
            CodeBlockTooltip.Show(slot.Block.targetWeapon, eventData.position);
        else if (slot.Type == ShopItemType.Passive)
            CodeBlockTooltip.Show(slot.Passive, eventData.position);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        CodeBlockTooltip.Move(eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        CodeBlockTooltip.Hide();
    }

    public void SetSold(bool sold)
    {
        if (buyButton != null)
            buyButton.interactable = !sold;

        if (nameText != null)
            nameText.text = sold ? "NULL" : slot.GetDisplayName();
        if (priceText != null)
            priceText.text = sold ? $"<s>{slot.Price} P</s>" : $"{slot.Price} P";
        if (iconImage != null)
            iconImage.gameObject.SetActive(!sold);
        if (codeBlock != null)
            codeBlock.gameObject.SetActive(!sold);


        if (soldOverlay != null)
            soldOverlay.SetActive(sold);
    }
}
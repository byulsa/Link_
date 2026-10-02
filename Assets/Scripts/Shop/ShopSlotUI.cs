using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopSlotUI : MonoBehaviour
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

        priceText.text = $"{slot.Price} P";
        SetSold(slot.IsSold);

        if (PointManager.Instance != null)
            UpdatePriceColor(PointManager.Instance.CurrentPoint);

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnClickBuy);
    }

    private void SetupCode()
    {
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
        if (itemPreview != null)
            itemPreview.SetActive(true);

        if (codePreview != null)
            codePreview.SetActive(false);

        if (iconImage != null)
            iconImage.sprite = slot.GetIcon();
    }

    private void UpdatePriceColor(int currentPoint)
    {
        if (slot == null)
            return;

        priceText.color = currentPoint >= slot.Price ? affordableColor : unaffordableColor;
    }

    private void OnClickBuy()
    {
        if (shopManager.TryPurchase(slotIndex))
            SetSold(true);
    }

    public void SetSold(bool sold)
    {
        if (buyButton != null)
            buyButton.interactable = !sold;

        if (soldOverlay != null)
            soldOverlay.SetActive(sold);
    }
}
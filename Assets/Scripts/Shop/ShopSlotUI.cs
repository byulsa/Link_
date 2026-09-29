using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopSlotUI : MonoBehaviour
{
    [SerializeField] private TMP_Text blockText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button buyButton;
    [SerializeField] private GameObject soldOverlay;

    [SerializeField] private Color affordableColor = Color.black;
    [SerializeField] private Color unaffordableColor = Color.red;

    private ShopSlot slot;
    private int slotIndex;
    private ShopManager shopManager;

    void Update()
    {
        if (slot == null || PointManager.Instance == null)
            return;

        UpdatePriceColor(PointManager.Instance.CurrentPoint);
    }

    public void Setup(int index, ShopSlot slot, ShopManager manager)
    {
        this.slot = slot;
        slotIndex = index;
        shopManager = manager;

        blockText.text = slot.Block.hasValue
            ? $"{slot.Block.displayText}{slot.Value}"
            : slot.Block.displayText;

        priceText.text = $"{slot.Price}P";

        SetSold(slot.IsSold);

        if (PointManager.Instance != null)
        {
            UpdatePriceColor(PointManager.Instance.CurrentPoint);
        }

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnClickBuy);
    }

    private void UpdatePriceColor(int currentPoint)
    {
        if (slot == null) return;

        priceText.color = currentPoint >= slot.Price
            ? affordableColor
            : unaffordableColor;
    }

    private void OnClickBuy()
    {
        if (shopManager.TryPurchase(slotIndex))
        {
            SetSold(true);
        }
    }

    public void SetSold(bool sold)
    {
        if (buyButton != null)
            buyButton.interactable = !sold;

        if (soldOverlay != null)
            soldOverlay.SetActive(sold);
    }

    [ContextMenu("Toggle Sold")]
    public void ToggleSold()
    {
        if (slot == null) return;

        slot.SetSold(!slot.IsSold);
        SetSold(slot.IsSold);
    }

    private void OnDestroy()
    {
        if (PointManager.Instance != null)
            PointManager.Instance.OnPointChanged -= UpdatePriceColor;
    }
}
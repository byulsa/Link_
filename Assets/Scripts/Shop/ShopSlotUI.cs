using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopSlotUI : MonoBehaviour
{
    [SerializeField] private TMP_Text blockText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button buyButton;
    [SerializeField] private GameObject soldOverlay;

    private int slotIndex;
    private ShopManager shopManager;

    public void Setup(int index, ShopSlot slot, ShopManager manager)
    {
        slotIndex = index;
        shopManager = manager;

        blockText.text = slot.Block.hasValue
            ? $"{slot.Block.displayText}{slot.Value}"
            : slot.Block.displayText;

        priceText.text = $"{slot.Price}P";

        SetSold(slot.IsSold);

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnClickBuy);
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
}
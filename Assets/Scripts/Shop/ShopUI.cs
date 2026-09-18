using System.Collections.Generic;
using UnityEngine;

public class ShopUI : MonoBehaviour
{
    [SerializeField] private ShopManager shopManager;
    [SerializeField] private ShopSlotUI slotPrefab;
    [SerializeField] private Transform slotContainer;

    private readonly List<ShopSlotUI> spawnedSlots = new();

    private void OnEnable()
    {
        shopManager.OnShopRefreshed += RenderSlots;
        shopManager.OnPurchaseSucceeded += OnPurchaseSucceeded;
        shopManager.OnPurchaseFailed += OnPurchaseFailed;

        shopManager.RefreshShop();
    }

    private void OnDisable()
    {
        shopManager.OnShopRefreshed -= RenderSlots;
        shopManager.OnPurchaseSucceeded -= OnPurchaseSucceeded;
        shopManager.OnPurchaseFailed -= OnPurchaseFailed;
    }

    private void RenderSlots()
    {
        ClearSlots();

        for (int i = 0; i < shopManager.CurrentSlots.Count; i++)
        {
            ShopSlotUI slotUI = Instantiate(slotPrefab, slotContainer);
            slotUI.Setup(i, shopManager.CurrentSlots[i], shopManager);
            spawnedSlots.Add(slotUI);
        }
    }

    private void ClearSlots()
    {
        foreach (var slot in spawnedSlots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }

        spawnedSlots.Clear();
    }

    private void OnPurchaseSucceeded(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < spawnedSlots.Count)
            spawnedSlots[slotIndex].SetSold(true);
    }

    private void OnPurchaseFailed(int slotIndex)
    {
        Debug.Log($"[ShopUI] 구매 실패: slot {slotIndex}");
    }
}
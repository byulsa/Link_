using System;
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [SerializeField] private ShopDropTable dropTable;
    [SerializeField] private CodeGrid grid;
    [SerializeField] private CodeEditor editor;

    [Header("Shop Settings")]
    [SerializeField, Min(1)] private int minSlotCount = 3;
    [SerializeField, Min(1)] private int maxSlotCount = 5;

    private readonly List<ShopSlot> currentSlots = new();

    public IReadOnlyList<ShopSlot> CurrentSlots => currentSlots;

    public event Action OnShopRefreshed;
    public event Action<int> OnPurchaseSucceeded;
    public event Action<int> OnPurchaseFailed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void RefreshShop()
    {
        currentSlots.Clear();

        if (dropTable == null ||
            (dropTable.simpleEntries.Count == 0 && dropTable.valueEntries.Count == 0))
        {
            OnShopRefreshed?.Invoke();
            return;
        }

        int slotCount = UnityEngine.Random.Range(minSlotCount, maxSlotCount + 1);

        for (int i = 0; i < slotCount; i++)
        {
            if (!dropTable.TrySelectRandomItem(
                    out BlockDefinition block,
                    out int value,
                    out int price))
            {
                continue;
            }

            currentSlots.Add(new ShopSlot(block, price, value));
        }

        OnShopRefreshed?.Invoke();
    }

    public bool TryPurchase(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= currentSlots.Count)
            return false;

        ShopSlot slot = currentSlots[slotIndex];

        if (slot.IsSold)
            return false;

        if (!PointManager.Instance.TrySpendPoint(slot.Price))
        {
            OnPurchaseFailed?.Invoke(slotIndex);
            return false;
        }

        int blockWidth = GetBlockWidth(slot.Block, slot.Value);

        if (!grid.TryFindEmptyPosition(blockWidth, out Vector2Int position))
        {
            // 배치 실패 -> Point 환불
            PointManager.Instance.AddPoint(slot.Price);
            OnPurchaseFailed?.Invoke(slotIndex);
            return false;
        }

        editor.CreateDropBlock(slot.Block, position, slot.Value);

        slot.MarkSold();

        OnPurchaseSucceeded?.Invoke(slotIndex);

        return true;
    }

    private int GetBlockWidth(BlockDefinition definition, int value)
    {
        if (!definition.hasValue)
            return definition.displayText.Length;

        return definition.displayText.Length + value.ToString().Length;
    }
}
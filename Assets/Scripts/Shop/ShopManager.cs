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

        if (dropTable == null || dropTable.entries.Count == 0)
        {
            Debug.LogWarning("[ShopManager] ShopDropTable이 비어 있습니다.");
            OnShopRefreshed?.Invoke();
            return;
        }

        int slotCount = UnityEngine.Random.Range(minSlotCount, maxSlotCount + 1);

        for (int i = 0; i < slotCount; i++)
        {
            ShopDropTable.ShopEntry entry = SelectEntry();

            if (entry == null)
                continue;

            int value = 0;

            if (entry.block.hasValue)
            {
                value = UnityEngine.Random.Range(
                    entry.block.minValue,
                    entry.block.maxValue + 1
                );
            }

            currentSlots.Add(new ShopSlot(entry.block, entry.price, value));
        }

        Debug.Log($"[ShopManager] 상점 갱신: {currentSlots.Count}개 슬롯");

        OnShopRefreshed?.Invoke();
    }

    private ShopDropTable.ShopEntry SelectEntry()
    {
        float totalWeight = 0f;

        foreach (var entry in dropTable.entries)
        {
            if (entry == null || entry.block == null || entry.weight <= 0f)
                continue;

            totalWeight += entry.weight;
        }

        if (totalWeight <= 0f)
            return null;

        float random = UnityEngine.Random.Range(0f, totalWeight);

        foreach (var entry in dropTable.entries)
        {
            if (entry == null || entry.block == null || entry.weight <= 0f)
                continue;

            random -= entry.weight;

            if (random <= 0f)
                return entry;
        }

        return null;
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
            Debug.Log($"[ShopManager] Point 부족: {slot.Block.displayText}");
            OnPurchaseFailed?.Invoke(slotIndex);
            return false;
        }

        int blockWidth = GetBlockWidth(slot.Block, slot.Value);

        if (!grid.TryFindEmptyPosition(blockWidth, out Vector2Int position))
        {
            Debug.Log($"[ShopManager] 공간 부족: {slot.Block.displayText}");

            // 배치 실패 -> Point 환불
            PointManager.Instance.AddPoint(slot.Price);
            OnPurchaseFailed?.Invoke(slotIndex);
            return false;
        }

        editor.CreateDropBlock(slot.Block, position, slot.Value);

        slot.MarkSold();

        Debug.Log($"[ShopManager] 구매 완료: {slot.Block.displayText} (-{slot.Price}P)");

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
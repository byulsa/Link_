using System;
using System.Collections.Generic;
using UnityEngine;

public enum ShopItemType
{
    Block,
    Weapon,
    Passive,
}

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

        if (dropTable == null)
        {
            OnShopRefreshed?.Invoke();
            return;
        }

        int slotCount = UnityEngine.Random.Range(minSlotCount, maxSlotCount + 1);

        for (int i = 0; i < slotCount; i++)
        {
            if (!dropTable.TrySelectRandomItem(out ShopDropTable.ShopDropResult result))
                continue;

            ShopSlot slot = result.type switch
            {
                ShopItemType.Block => new ShopSlot(result.block, result.price, result.value),
                ShopItemType.Weapon => new ShopSlot(result.block, result.price), // Weapon도 block 기반
                ShopItemType.Passive => new ShopSlot(result.passive, result.price),
                _ => null,
            };

            if (slot != null)
                currentSlots.Add(slot);
        }

        OnShopRefreshed?.Invoke();
    }

    public bool TryPurchase(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= currentSlots.Count) return false;

        ShopSlot slot = currentSlots[slotIndex];
        if (slot.IsSold) return false;

        return slot.Type switch
        {
            ShopItemType.Block => TryPurchaseBlock(slot, slotIndex),
            ShopItemType.Weapon => TryPurchaseBlock(slot, slotIndex), // 무기도 결국 블록 배치 + 부수 효과
            ShopItemType.Passive => TryPurchasePassive(slot, slotIndex),
            _ => false
        };
    }

    private bool TryPurchaseBlock(ShopSlot slot, int slotIndex)
    {
        int blockWidth = GetBlockWidth(slot.Block, slot.Value);

        if (!grid.TryFindEmptyPosition(blockWidth, out Vector2Int position) ||
            !grid.CanPlace(position, blockWidth, null))
        {
            OnPurchaseFailed?.Invoke(slotIndex);
            return false;
        }

        if (!PointManager.Instance.TrySpendPoint(slot.Price))
        {
            OnPurchaseFailed?.Invoke(slotIndex);
            return false;
        }

        editor.CreateDropBlock(slot.Block, position, slot.Value);

        // 무기 전용 블록이면 실제 무기 오브젝트도 추가 장착 (여러 개 동시 보유 가능)
        if (slot.Block.targetWeapon != null)
            WeaponManager.Instance.AddWeapon(slot.Block.targetWeapon);

        slot.MarkSold();
        OnPurchaseSucceeded?.Invoke(slotIndex);
        return true;
    }

    private bool TryPurchasePassive(ShopSlot slot, int slotIndex)
    {
        if (!PointManager.Instance.TrySpendPoint(slot.Price))
        {
            OnPurchaseFailed?.Invoke(slotIndex);
            return false;
        }

        PassiveEffectManager.Instance.Acquire(slot.Passive);

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
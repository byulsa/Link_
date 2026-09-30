using System;
using System.Collections.Generic;
using UnityEngine;
public enum ShopItemType
{
    Block,
    Weapon,
    Passive
}
public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [SerializeField] private ShopDropTable dropTable;
    [SerializeField] private CodeGrid grid;
    [SerializeField] private CodeEditor editor;
    [SerializeField] private WeaponBase playerWeapon; // 장착 중인 무기 참조
    [SerializeField] private BlockDefinition weaponBlockDefinition;

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
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void RefreshShop()
    {
        currentSlots.Clear();

        if (dropTable == null) { OnShopRefreshed?.Invoke(); return; }

        int slotCount = UnityEngine.Random.Range(minSlotCount, maxSlotCount + 1);

        for (int i = 0; i < slotCount; i++)
        {
            if (!dropTable.TrySelectRandomItem(out ShopDropTable.ShopDropResult result))
                continue;

            ShopSlot slot = result.type switch
            {
                ShopItemType.Block => new ShopSlot(result.block, result.price, result.value),
                ShopItemType.Weapon => new ShopSlot(result.weapon, result.price),
                ShopItemType.Passive => new ShopSlot(result.passive, result.price),
                _ => null
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
            ShopItemType.Weapon => TryPurchaseWeapon(slot, slotIndex),
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
        slot.MarkSold();
        OnPurchaseSucceeded?.Invoke(slotIndex);
        return true;
    }

    private bool TryPurchaseWeapon(ShopSlot slot, int slotIndex)
    {
        int blockWidth = weaponBlockDefinition.displayText.Length;

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

        // 1) 코드 그리드에 WEAP 블록 배치 → 코드 공간을 차지
        CodeBlock block = editor.CreateDropBlock(weaponBlockDefinition, position, 0);
        block.SetLinkedWeapon(slot.Weapon);

        // 2) 실제 무기 오브젝트 스폰 (플레이어 주변에서 회전)
        WeaponManager.Instance.AddWeapon(slot.Weapon);

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
        if (!definition.hasValue) return definition.displayText.Length;
        return definition.displayText.Length + value.ToString().Length;
    }
}
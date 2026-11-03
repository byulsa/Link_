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
    [SerializeField] private ShopDropTable dropTable;
    [SerializeField] private CodeGrid grid;
    [SerializeField] private CodeEditor editor;
    [SerializeField] private WeaponManager weaponManager;

    [Header("Shop Layout (DATA EXCHANGE)")]
    [SerializeField] private int topSlotCount = 5;     // 상단: 코드 블록 + 패시브
    [SerializeField] private int weaponSlotCount = 2;  // 하단: 무기 고정

    private readonly List<ShopSlot> currentSlots = new();
    public IReadOnlyList<ShopSlot> CurrentSlots => currentSlots;

    public event Action OnShopRefreshed;
    public event Action<int> OnPurchaseSucceeded;
    public event Action<int> OnPurchaseFailed;

    public void RefreshShop()
    {
        currentSlots.Clear();

        if (dropTable == null)
        {
            OnShopRefreshed?.Invoke();
            return;
        }

        // Each top slot independently rolls Value / Code / Passive at 70 / 20 / 10.
        HashSet<PassiveItemDefinition> excludedPassives = new();
        if (PassiveEffectManager.Instance != null)
        {
            foreach (PassiveItemDefinition passive in PassiveEffectManager.Instance.AcquiredPassives)
                excludedPassives.Add(passive);
        }

        for (int i = 0; i < topSlotCount; i++)
        {
            if (!dropTable.TrySelectTopItem(excludedPassives, out ShopDropTable.ShopDropResult result))
            {
                // Reserve the index so weapon slots never move into the top row.
                currentSlots.Add(null);
                continue;
            }
            if (result.type == ShopItemType.Passive)
            {
                currentSlots.Add(new ShopSlot(result.passive, result.price));
                excludedPassives.Add(result.passive);
            }
            else
                currentSlots.Add(new ShopSlot(result.block, result.price, result.value));
        }

        // ── 하단: 무기 고정 ──
        for (int i = 0; i < weaponSlotCount; i++)
        {
            if (dropTable.TrySelectWeaponOnly(out ShopDropTable.ShopDropResult result))
                currentSlots.Add(new ShopSlot(result.block, result.price, result.value));
            else
                currentSlots.Add(null);
        }

        OnShopRefreshed?.Invoke();
    }

    public bool TryPurchase(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= currentSlots.Count) return false;

        ShopSlot slot = currentSlots[slotIndex];
        if (slot == null || slot.IsSold) return false;

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
        if (slot.Block.targetWeapon != null && weaponManager == null)
        {
            Debug.LogError("[ShopManager] 무기를 추가할 WeaponManager가 설정되지 않았습니다.", this);
            OnPurchaseFailed?.Invoke(slotIndex);
            return false;
        }

        bool shouldPlaceBlock = slot.Type != ShopItemType.Weapon
            || !HasWeaponCode(slot.Block.targetWeapon);
        int blockWidth = shouldPlaceBlock ? GetBlockWidth(slot.Block, slot.Value) : 0;

        Vector2Int position = default;
        if (shouldPlaceBlock &&
            (!grid.TryFindEmptyPosition(blockWidth, out position) ||
             !grid.CanPlace(position, blockWidth, null)))
        {
            OnPurchaseFailed?.Invoke(slotIndex);
            return false;
        }

        if (!PointManager.Instance.TrySpendPoint(slot.Price))
        {
            OnPurchaseFailed?.Invoke(slotIndex);
            return false;
        }

        if (shouldPlaceBlock)
            editor.CreateDropBlock(slot.Block, position, slot.Value);

        // 무기 전용 블록(targetWeapon 지정)이면 실제 무기 오브젝트도 추가 장착
        if (slot.Block.targetWeapon != null)
            weaponManager.AddWeapon(slot.Block.targetWeapon);

        slot.MarkSold();
        OnPurchaseSucceeded?.Invoke(slotIndex);
        return true;
    }

    private bool HasWeaponCode(WeaponDefinition weapon)
    {
        if (weapon == null)
            return false;

        foreach (CodeBlock block in grid.GetBlocks())
        {
            if (block.Definition != null && block.Definition.targetWeapon == weapon)
                return true;
        }

        return false;
    }

    private bool TryPurchasePassive(ShopSlot slot, int slotIndex)
    {
        PassiveEffectManager passiveEffectManager = PassiveEffectManager.Instance;
        if (passiveEffectManager == null)
        {
            Debug.LogError("[ShopManager] 패시브 구매를 처리할 PassiveEffectManager가 없습니다.", this);
            OnPurchaseFailed?.Invoke(slotIndex);
            return false;
        }

        if (passiveEffectManager.HasAcquired(slot.Passive))
        {
            OnPurchaseFailed?.Invoke(slotIndex);
            return false;
        }

        if (!PointManager.Instance.TrySpendPoint(slot.Price))
        {
            OnPurchaseFailed?.Invoke(slotIndex);
            return false;
        }

        passiveEffectManager.Acquire(slot.Passive);

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

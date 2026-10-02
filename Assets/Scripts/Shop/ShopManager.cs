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

    [Header("Shop Layout (DATA EXCHANGE)")]
    [SerializeField] private int topSlotCount = 5;     // 상단: 코드 블록 + 패시브
    [SerializeField] private int weaponSlotCount = 2;  // 하단: 무기 고정
    [SerializeField, Range(0, 2)] private int minPassiveCount = 0;
    [SerializeField, Range(0, 2)] private int maxPassiveCount = 2;

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

        // ── 상단: 패시브 0~2개 + 나머지는 블록 ──
        int passiveCount = UnityEngine.Random.Range(minPassiveCount, maxPassiveCount + 1);
        passiveCount = Mathf.Min(passiveCount, topSlotCount);
        int blockCount = topSlotCount - passiveCount;

        List<ShopSlot> topSlots = new List<ShopSlot>();

        for (int i = 0; i < passiveCount; i++)
        {
            if (dropTable.TrySelectPassiveOnly(out ShopDropTable.ShopDropResult result))
                topSlots.Add(new ShopSlot(result.passive, result.price));
        }

        for (int i = 0; i < blockCount; i++)
        {
            if (dropTable.TrySelectBlockOnly(out ShopDropTable.ShopDropResult result))
                topSlots.Add(new ShopSlot(result.block, result.price, result.value));
        }

        Shuffle(topSlots); // 패시브 슬롯이 항상 뒤쪽에 몰리지 않도록 순서 섞기
        currentSlots.AddRange(topSlots);

        // ── 하단: 무기 고정 ──
        for (int i = 0; i < weaponSlotCount; i++)
        {
            if (dropTable.TrySelectWeaponOnly(out ShopDropTable.ShopDropResult result))
                currentSlots.Add(new ShopSlot(result.block, result.price));
        }

        OnShopRefreshed?.Invoke();
    }

    private void Shuffle(List<ShopSlot> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
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

        // 무기 전용 블록(targetWeapon 지정)이면 실제 무기 오브젝트도 추가 장착
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
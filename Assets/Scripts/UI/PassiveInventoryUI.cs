using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PassiveInventoryUI : MonoBehaviour
{
    [SerializeField] private Text title;
    [SerializeField] private Text emptyText;
    [SerializeField] private RectTransform content;
    [SerializeField] private PassiveInventorySlot slotPrefab;

    private readonly List<PassiveInventorySlot> slots = new();
    private PassiveEffectManager manager;
    private bool started;

    private void Start()
    {
        started = true;
        Bind();
    }

    private void OnEnable()
    {
        if (started) Bind();
    }

    private void OnDisable()
    {
        if (manager != null) manager.OnPassivesChanged -= Refresh;
        manager = null;
    }

    private void Bind()
    {
        manager = PassiveEffectManager.Instance;
        if (manager != null) manager.OnPassivesChanged += Refresh;
        Refresh();
    }

    private void Refresh()
    {
        int count = manager != null ? manager.AcquiredPassives.Count : 0;
        title.text = $"보유 아이템  {count}";
        emptyText.gameObject.SetActive(count == 0);
        for (int i = 0; i < count; i++)
        {
            if (i == slots.Count)
                slots.Add(Instantiate(slotPrefab, content));
            slots[i].Setup(manager.AcquiredPassives[i]);
            slots[i].gameObject.SetActive(true);
        }

        for (int i = count; i < slots.Count; i++)
            slots[i].gameObject.SetActive(false);
    }
}

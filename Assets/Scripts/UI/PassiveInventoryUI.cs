using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PassiveInventoryUI : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset font;
    private PassiveEffectManager manager;
    private RectTransform content;
    private TMP_Text emptyText;
    private TMP_Text title;
    private bool started;

    private void Awake()
    {
        Image background = gameObject.GetComponent<Image>();
        if (background == null) background = gameObject.AddComponent<Image>();
        background.color = new Color(0.035f, 0.05f, 0.08f, 0.9f);
        title = CreateText("Title", transform, "보유 아이템  0", 22f);
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(-24f, 40f));
        title.alignment = TextAlignmentOptions.MidlineLeft;

        RectTransform scrollRoot = CreateRect("Item Scroll", transform);
        Stretch(scrollRoot, new Vector2(12f, 12f), new Vector2(-12f, -44f));
        ScrollRect scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 28f;

        RectTransform viewport = CreateRect("Viewport", scrollRoot);
        Stretch(viewport, Vector2.zero, Vector2.zero);
        Image viewportImage = viewport.gameObject.AddComponent<Image>();
        viewportImage.color = Color.clear;
        viewport.gameObject.AddComponent<RectMask2D>();
        content = CreateRect("Content", viewport);
        SetRect(content, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
        GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(52f, 52f);
        grid.spacing = new Vector2(8f, 8f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        grid.childAlignment = TextAnchor.UpperLeft;
        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport;
        scroll.content = content;

        emptyText = CreateText("Empty", scrollRoot, "아직 보유한 아이템이 없습니다.", 17f);
        Stretch(emptyText.rectTransform, Vector2.zero, Vector2.zero);
        emptyText.color = new Color(0.65f, 0.7f, 0.78f);
        emptyText.alignment = TextAlignmentOptions.TopLeft;
    }

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
        if (manager != null) manager.OnPassivesChanged -= Refresh;
        manager = PassiveEffectManager.Instance;
        if (manager != null) manager.OnPassivesChanged += Refresh;
        Refresh();
    }

    private void Refresh()
    {
        foreach (Transform child in content)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
        int count = manager != null ? manager.AcquiredPassives.Count : 0;
        title.text = $"보유 아이템  {count}";
        emptyText.gameObject.SetActive(count == 0);
        if (manager == null) return;
        foreach (PassiveItemDefinition passive in manager.AcquiredPassives)
        {
            if (passive == null) continue;
            RectTransform slot = CreateRect(passive.itemName, content);
            Image background = slot.gameObject.AddComponent<Image>();
            background.color = new Color(0.18f, 0.24f, 0.32f, 1f);
            slot.gameObject.AddComponent<PassiveInventorySlot>().Setup(passive);
            if (passive.icon != null)
            {
                RectTransform iconRect = CreateRect("Icon", slot);
                Stretch(iconRect, Vector2.one * 5f, Vector2.one * -5f);
                Image icon = iconRect.gameObject.AddComponent<Image>();
                icon.sprite = passive.icon;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
            }
            else
            {
                TMP_Text label = CreateText("Name", slot, passive.itemName, 13f);
                Stretch(label.rectTransform, Vector2.one * 3f, Vector2.one * -3f);
                label.alignment = TextAlignmentOptions.Center;
            }
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    private TMP_Text CreateText(string name, Transform parent, string text, float size)
    {
        RectTransform rect = CreateRect(name, parent);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        label.text = text;
        label.fontSize = size;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    private RectTransform CreateRect(string name, Transform parent)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        child.layer = gameObject.layer;
        child.transform.SetParent(parent, false);
        return (RectTransform)child.transform;
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = min;
        rect.offsetMax = max;
    }

    private static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}

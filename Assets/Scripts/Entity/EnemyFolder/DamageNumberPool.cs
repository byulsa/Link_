using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class DamageNumberPool : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_FontAsset font;
    [SerializeField, Min(0.1f)] private float fontSize = 5f;
    [SerializeField] private FontStyles fontStyle = FontStyles.Bold;
    [SerializeField] private Color textColor = new Color(1f, 0.85f, 0.45f, 1f);
    [SerializeField] private int sortingOrderOffset = 10;

    [Header("Motion")]
    [SerializeField] private Vector2 spawnOffset = new Vector2(0f, 0.25f);
    [SerializeField, Min(0f)] private float horizontalSpread = 0.2f;
    [SerializeField, Min(0f)] private float riseDistance = 0.8f;
    [SerializeField, Min(0.01f)] private float duration = 0.65f;
    [SerializeField, Min(0f)] private float fadeDelay = 0.15f;

    [Header("Pool")]
    [SerializeField, Min(0)] private int initialCapacity = 24;
    private static DamageNumberPool instance;

    private sealed class Entry
    {
        public GameObject Object;
        public Transform Transform;
        public TextMeshPro Label;
        public Renderer Renderer;
        public Vector3 Origin;
        public float Elapsed;
    }

    private readonly Stack<Entry> available = new();
    private readonly List<Entry> active = new();

    public static void WarmUp()
    {
        if (instance != null)
            return;

        instance = FindAnyObjectByType<DamageNumberPool>();
        if (instance == null)
            instance = new GameObject("Damage Number Pool").AddComponent<DamageNumberPool>();

        for (int i = 0; i < instance.initialCapacity; i++)
            instance.available.Push(instance.CreateEntry());
        instance.enabled = false;
    }

    public static void Show(float damage, Vector3 position, SpriteRenderer enemyRenderer)
    {
        WarmUp();
        Entry entry = instance.available.Count > 0 ? instance.available.Pop() : instance.CreateEntry();
        position += (Vector3)instance.spawnOffset;
        position.x += Random.Range(-instance.horizontalSpread, instance.horizontalSpread);
        entry.Origin = position;
        entry.Elapsed = 0f;
        entry.Transform.position = position;
        TMP_FontAsset selectedFont = instance.font != null ? instance.font : TMP_Settings.defaultFontAsset;
        if (selectedFont != null && entry.Label.font != selectedFont)
            entry.Label.font = selectedFont;
        entry.Label.fontSize = instance.fontSize;
        entry.Label.fontStyle = instance.fontStyle;
        entry.Label.color = instance.textColor;
        entry.Label.text = damage < 0.01f ? "<0.01" : damage.ToString("0.##");
        entry.Renderer.sortingLayerID = enemyRenderer != null ? enemyRenderer.sortingLayerID : 0;
        entry.Renderer.sortingOrder = (enemyRenderer != null ? enemyRenderer.sortingOrder : 0) + instance.sortingOrderOffset;
        entry.Object.SetActive(true);
        instance.active.Add(entry);
        instance.enabled = true;
    }

    private Entry CreateEntry()
    {
        GameObject popup = new GameObject("Damage Number", typeof(RectTransform));
        popup.transform.SetParent(transform, false);
        TextMeshPro label = popup.AddComponent<TextMeshPro>();
        label.alignment = TextAlignmentOptions.Center;
        label.richText = false;
        label.rectTransform.sizeDelta = new Vector2(4f, 1f);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        popup.SetActive(false);
        return new Entry
        {
            Object = popup,
            Transform = popup.transform,
            Label = label,
            Renderer = popup.GetComponent<Renderer>(),
        };
    }

    private void Update()
    {
        float delta = Time.deltaTime;
        float lifetime = Mathf.Max(0.01f, duration);
        float fadeStart = Mathf.Clamp(fadeDelay, 0f, lifetime);
        for (int i = active.Count - 1; i >= 0; i--)
        {
            Entry entry = active[i];
            entry.Elapsed += delta;
            if (entry.Elapsed >= lifetime)
            {
                entry.Object.SetActive(false);
                available.Push(entry);
                int last = active.Count - 1;
                active[i] = active[last];
                active.RemoveAt(last);
                continue;
            }

            float progress = entry.Elapsed / lifetime;
            float rise = 1f - (1f - progress) * (1f - progress);
            entry.Transform.position = entry.Origin + Vector3.up * (riseDistance * rise);
            entry.Label.alpha = textColor.a * (1f - Mathf.InverseLerp(fadeStart, lifetime, entry.Elapsed));
        }

        if (active.Count == 0)
            enabled = false;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}

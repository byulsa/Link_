using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CodeBlockTooltip : MonoBehaviour
{
    private static CodeBlockTooltip instance;
    private static bool missingTooltipWarningShown;

    [SerializeField]
    private TMP_Text codeNameText;

    [SerializeField]
    private TMP_Text categoryText;

    [SerializeField]
    private TMP_Text descriptionText;

    [SerializeField, Min(0f)]
    private float cursorOffset = 18f;

    [SerializeField, Min(0f)]
    private float screenMargin = 12f;

    private RectTransform rectTransform;
    private RectTransform canvasRectTransform;
    private Canvas canvas;
    private Vector2 pointerPosition;
    private readonly Vector3[] worldCorners = new Vector3[4];
    private bool showRequested;
    private Image weaponSilhouette;
    private Vector2 descriptionPosition;
    private Vector2 descriptionSize;
    private bool hasDescriptionLayout;
    private TooltipKeyword[] keywords;
    private Regex keywordPattern;
    private Dictionary<string, TooltipKeyword> keywordsByName;
    private KeywordHoverPanel keywordPrefab;
    private readonly List<KeywordHoverPanel> keywordPanels = new();
    private float keywordHeight;
    private float keywordWidth;
    private const float KeywordGap = 6f;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogError("[CodeBlockTooltip] 툴팁 컴포넌트가 씬에 두 개 이상 있습니다.", this);
            return;
        }

        instance = this;
        rectTransform = transform as RectTransform;
        canvas = GetComponentInParent<Canvas>();
        canvasRectTransform = canvas != null ? canvas.transform as RectTransform : null;

        foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;

        if (rectTransform == null || canvas == null || canvasRectTransform == null)
            Debug.LogError("[CodeBlockTooltip] 툴팁은 RectTransform을 가진 Canvas 자식이어야 합니다.", this);

        if (!showRequested)
            gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public static void Show(CodeBlock block, Vector2 pointerPosition)
    {
        if (block == null || block.Definition == null)
            return;

        WeaponDefinition weapon = block.LinkedWeapon != null ? block.LinkedWeapon : block.Definition.targetWeapon;
        if (weapon != null)
            Show(weapon, pointerPosition);
        else
            Show(block.Definition, pointerPosition);
    }

    public static void Show(BlockDefinition definition, Vector2 pointerPosition)
    {
        if (definition == null)
            return;

        Show(
            definition.fullName,
            definition.displayText,
            definition.category.ToString(),
            definition.description,
            pointerPosition
        );
    }

    public static void Show(WeaponDefinition weapon, Vector2 pointerPosition)
    {
        if (weapon == null)
            return;

        string displayName = string.IsNullOrWhiteSpace(weapon.displayText)
            ? weapon.weaponName
            : weapon.displayText;

        Show(
            displayName,
            "Weapon",
            "Weapon",
            weapon.description,
            pointerPosition,
            weapon
        );
    }

    public static void Show(PassiveItemDefinition passive, Vector2 pointerPosition)
    {
        if (passive == null)
            return;

        Show(
            passive.itemName,
            passive.itemName,
            "Passive",
            passive.GetFormattedDescription(),
            pointerPosition
        );
    }

    private static void Show(string fullName, string fallbackName, string category, string description,
        Vector2 pointerPosition, WeaponDefinition weapon = null)
    {
        if (instance == null)
            instance = FindFirstObjectByType<CodeBlockTooltip>(FindObjectsInactive.Include);

        if (instance == null)
        {
            if (!missingTooltipWarningShown)
            {
                Debug.LogWarning("[CodeBlockTooltip] 씬에서 툴팁 컴포넌트를 찾지 못했습니다.");
                missingTooltipWarningShown = true;
            }

            return;
        }

        if (
            instance.codeNameText == null
            || instance.categoryText == null
            || instance.descriptionText == null
        )
        {
            Debug.LogError("[CodeBlockTooltip] 이름, 카테고리, 설명 TMP 텍스트를 모두 연결해야 합니다.", instance);
            return;
        }

        instance.codeNameText.text = string.IsNullOrWhiteSpace(fullName)
            ? fallbackName
            : fullName;
        instance.categoryText.text = category;
        instance.descriptionText.text = description;
        instance.pointerPosition = pointerPosition;
        instance.showRequested = true;
        instance.transform.SetAsLastSibling();
        instance.gameObject.SetActive(true);
        instance.showRequested = false;
        instance.SetWeaponSilhouette(weapon);
        instance.SetKeywordPanels(description);
        Canvas.ForceUpdateCanvases();
        instance.UpdatePosition();
        missingTooltipWarningShown = false;
    }

    private void SetKeywordPanels(string description)
    {
        if (keywords == null)
        {
            keywords = Resources.LoadAll<TooltipKeyword>("TooltipKeywords");
            keywordsByName = new Dictionary<string, TooltipKeyword>();
            foreach (TooltipKeyword keyword in keywords)
                if (!string.IsNullOrWhiteSpace(keyword.keyword))
                    keywordsByName.TryAdd(keyword.keyword, keyword);

            if (keywordsByName.Count > 0)
            {
                string terms = string.Join("|", keywordsByName.Keys.OrderByDescending(term => term.Length).Select(Regex.Escape));
                keywordPattern = new Regex($"<[^>]*>|{terms}");
            }
            keywordPrefab = Resources.Load<KeywordHoverPanel>("Keyword Hover Panel");
        }

        var found = new List<TooltipKeyword>();
        descriptionText.text = keywordPattern == null ? description : keywordPattern.Replace(description ?? "", match =>
        {
            if (!keywordsByName.TryGetValue(match.Value, out TooltipKeyword data))
                return match.Value;

            if (!found.Contains(data))
                found.Add(data);
            return $"<color=#{ColorUtility.ToHtmlStringRGB(data.highlightColor)}>{match.Value}</color>";
        });
        keywordHeight = keywordWidth = 0f;
        int count = keywordPrefab != null ? found.Count : 0;
        for (int i = 0; i < count; i++)
        {
            if (i == keywordPanels.Count)
                keywordPanels.Add(Instantiate(keywordPrefab, rectTransform));
            KeywordHoverPanel panel = keywordPanels[i];
            panel.gameObject.SetActive(true);
            panel.Bind(found[i]);
            panel.Rect.anchorMin = panel.Rect.anchorMax = new Vector2(0f, 0f);
            panel.Rect.pivot = new Vector2(0f, 1f);
            panel.Rect.anchoredPosition = new Vector2(0f, -keywordHeight - KeywordGap);
            keywordHeight += KeywordGap + panel.Rect.rect.height;
            keywordWidth = Mathf.Max(keywordWidth, panel.Rect.rect.width);
        }
        for (int i = count; i < keywordPanels.Count; i++)
            keywordPanels[i].gameObject.SetActive(false);
    }

    private void SetWeaponSilhouette(WeaponDefinition weapon)
    {
        RectTransform description = descriptionText.rectTransform;
        if (!hasDescriptionLayout)
        {
            descriptionPosition = description.anchoredPosition;
            descriptionSize = description.sizeDelta;
            hasDescriptionLayout = true;
        }

        Sprite sprite = null;
        if (weapon != null)
        {
            SpriteRenderer renderer = weapon.prefab != null
                ? weapon.prefab.GetComponentInChildren<SpriteRenderer>(true)
                : null;
            sprite = renderer != null && renderer.sprite != null ? renderer.sprite : weapon.icon;
        }

        if (sprite != null && weaponSilhouette == null)
        {
            GameObject preview = new GameObject("Weapon Silhouette", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            preview.layer = gameObject.layer;
            preview.transform.SetParent(rectTransform, false);
            weaponSilhouette = preview.GetComponent<Image>();
            weaponSilhouette.preserveAspect = true;
            weaponSilhouette.raycastTarget = false;
            weaponSilhouette.color = new Color32(201, 231, 244, 255);
            weaponSilhouette.rectTransform.sizeDelta = new Vector2(122f, 122f);
            weaponSilhouette.rectTransform.anchoredPosition =
                descriptionPosition + Vector2.right * (descriptionSize.x * 0.5f - 61f);
        }

        if (weaponSilhouette != null)
        {
            weaponSilhouette.sprite = sprite;
            weaponSilhouette.gameObject.SetActive(sprite != null);
        }

        float previewWidth = sprite != null ? 140f : 0f;
        description.sizeDelta = descriptionSize - Vector2.right * previewWidth;
        description.anchoredPosition = descriptionPosition - Vector2.right * (previewWidth * 0.5f);
    }

    public static void Move(Vector2 pointerPosition)
    {
        if (instance == null || !instance.gameObject.activeSelf)
            return;

        instance.pointerPosition = pointerPosition;
        instance.UpdatePosition();
    }

    public static void Hide()
    {
        if (instance != null)
            instance.gameObject.SetActive(false);
    }

    private void UpdatePosition()
    {
        if (rectTransform == null || canvasRectTransform == null)
            return;

        rectTransform.GetWorldCorners(worldCorners);
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(uiCamera, worldCorners[0]);
        Vector2 topRight = RectTransformUtility.WorldToScreenPoint(uiCamera, worldCorners[2]);
        float rootWidth = Mathf.Abs(topRight.x - bottomLeft.x);
        float rootHeight = Mathf.Abs(topRight.y - bottomLeft.y);
        float panelWidth = rootWidth * Mathf.Max(1f, keywordWidth / Mathf.Max(1f, rectTransform.rect.width));
        float extraHeight = rootHeight * keywordHeight / Mathf.Max(1f, rectTransform.rect.height);
        float panelHeight = rootHeight + extraHeight;

        bool placeRight = Screen.width - pointerPosition.x >= panelWidth + cursorOffset + screenMargin;
        bool placeAbove = Screen.height - pointerPosition.y >= panelHeight + cursorOffset + screenMargin;
        rectTransform.pivot = new Vector2(placeRight ? 0f : 1f, placeAbove ? 0f : 1f);
        foreach (KeywordHoverPanel panel in keywordPanels)
        {
            if (!panel.gameObject.activeSelf)
                continue;
            Vector2 position = panel.Rect.anchoredPosition;
            position.x = placeRight ? 0f : rectTransform.rect.width - panel.Rect.rect.width;
            panel.Rect.anchoredPosition = position;
        }

        Vector2 targetScreenPosition = new Vector2(
            pointerPosition.x + (placeRight ? cursorOffset : -cursorOffset),
            pointerPosition.y + (placeAbove ? cursorOffset : -cursorOffset)
        );
        targetScreenPosition.x = Mathf.Clamp(
            targetScreenPosition.x,
            screenMargin + (placeRight ? 0f : panelWidth),
            Screen.width - screenMargin - (placeRight ? panelWidth : 0f)
        );
        targetScreenPosition.y = Mathf.Clamp(
            targetScreenPosition.y,
            screenMargin + (placeAbove ? 0f : panelHeight),
            Screen.height - screenMargin - (placeAbove ? panelHeight : 0f)
        );
        if (placeAbove)
            targetScreenPosition.y += extraHeight;

        if (
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                canvasRectTransform,
                targetScreenPosition,
                uiCamera,
                out Vector3 worldPosition
            )
        )
        {
            rectTransform.position = worldPosition;
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

public sealed class KeywordHoverPanel : MonoBehaviour
{
    [SerializeField] private Text title;
    [SerializeField] private Text description;
    [SerializeField] private Image icon;
    [SerializeField, Min(0f)] private float padding = 12f;
    [SerializeField, Min(0f)] private float rowGap = 5f;
    [SerializeField, Min(80f)] private float maxWidth = 380f;

    public RectTransform Rect => (RectTransform)transform;

    public void Bind(TooltipKeyword data)
    {
        title.text = data.keyword;
        title.color = data.highlightColor;
        description.text = data.description;
        icon.sprite = data.icon;
        icon.gameObject.SetActive(data.icon != null);

        float iconWidth = data.icon != null ? icon.rectTransform.sizeDelta.x + rowGap : 0f;
        float headerWidth = title.preferredWidth + iconWidth;
        float width = Mathf.Min(maxWidth, Mathf.Max(headerWidth, description.preferredWidth) + padding * 2f);
        float contentWidth = Mathf.Max(1f, width - padding * 2f);
        float titleWidth = Mathf.Min(title.preferredWidth, Mathf.Max(1f, contentWidth - iconWidth));
        float headerHeight = Mathf.Max(title.preferredHeight, data.icon != null ? icon.rectTransform.sizeDelta.y : 0f);
        title.rectTransform.sizeDelta = new Vector2(titleWidth, headerHeight);
        title.rectTransform.anchoredPosition = new Vector2(padding, -padding);
        icon.rectTransform.anchoredPosition = new Vector2(padding + titleWidth + rowGap, -padding);

        RectTransform body = description.rectTransform;
        body.anchoredPosition = new Vector2(padding, -padding - headerHeight - rowGap);
        body.sizeDelta = new Vector2(contentWidth, 0f);
        float bodyHeight = description.preferredHeight;
        body.sizeDelta = new Vector2(contentWidth, bodyHeight);
        Rect.sizeDelta = new Vector2(width, padding * 2f + headerHeight + rowGap + bodyHeight);
    }
}

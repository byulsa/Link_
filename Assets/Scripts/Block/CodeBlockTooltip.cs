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

        instance.codeNameText.text = string.IsNullOrWhiteSpace(block.Definition.fullName)
            ? block.Definition.displayText
            : block.Definition.fullName;
        instance.categoryText.text = block.Definition.category.ToString();
        instance.descriptionText.text = block.Definition.description;
        instance.pointerPosition = pointerPosition;
        instance.showRequested = true;
        instance.gameObject.SetActive(true);
        instance.showRequested = false;
        Canvas.ForceUpdateCanvases();
        instance.UpdatePosition();
        missingTooltipWarningShown = false;
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
        float panelWidth = Mathf.Abs(topRight.x - bottomLeft.x);
        float panelHeight = Mathf.Abs(topRight.y - bottomLeft.y);

        bool placeRight = Screen.width - pointerPosition.x >= panelWidth + cursorOffset + screenMargin;
        bool placeAbove = Screen.height - pointerPosition.y >= panelHeight + cursorOffset + screenMargin;
        rectTransform.pivot = new Vector2(placeRight ? 0f : 1f, placeAbove ? 0f : 1f);

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

using TMPro;
using UnityEngine.Events;
using UnityEngine;
using UnityEngine.UI;

public sealed class CodeWorkspace : MonoBehaviour
{
    [SerializeField] private RectTransform viewport;
    [SerializeField] private CodeGrid grid;
    [SerializeField] private CodeGridBackdrop backdrop;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text status;
    [SerializeField] private TMP_Text fallbackLog;
    [SerializeField] private Button centerButton;
    [SerializeField] private Button checkCodeButton;
    [SerializeField] private Button doneButton;
    [SerializeField] private GameObject readOnlyOverlay;
    [SerializeField] private TMP_Text readOnlyMessage;
    [SerializeField] private Font readOnlyFontSource;

    private TMP_FontAsset readOnlyFont;
    [SerializeField, Min(0f)] private float boardPadding = 18f;

    private RectTransform board;
    private Canvas canvas;
    private CodeEditorRenderOutput renderOutput;
    private Vector2 homePosition;
    private Vector2 previousPointer;
    private float zoom = 1f;
    private bool panning;
    private bool isOpen;
    private Vector2 lastViewportSize;
    private const float TutorialCanvasScale = 0.0175f;
    private CanvasGroup tutorialOverlay;
    private CodeTutorialSpotlight spotlight;
    private Text tutorialHint;
    private bool showingTutorial;
    private CodeBlock highlightedBlock;
    private Rect highlightedSource;
    private Rect highlightedDestination;
    private readonly Vector3[] tutorialCorners = new Vector3[4];

    public void Initialize(UnityAction close)
    {
        board = (RectTransform)grid.transform;
        homePosition = board.anchoredPosition;
        canvas = GetComponentInParent<Canvas>();
        renderOutput = canvas.GetComponent<CodeEditorRenderOutput>();
        grid.SetEditingViewport(viewport);
        centerButton.onClick.AddListener(ResetView);
        checkCodeButton.onClick.AddListener(CheckCode);
        doneButton.onClick.AddListener(close);
    }

    public void SetVisible(bool visible)
    {
        canvasGroup.alpha = visible ? 1f : 0f;
        renderOutput?.SetVisible(visible);
    }

    public void SetOpen(bool open, bool readOnly, bool preserveView = false)
    {
        if (!open || readOnly)
        {
            showingTutorial = false;
            if (tutorialOverlay != null)
                tutorialOverlay.gameObject.SetActive(false);
        }
        bool wasOpen = isOpen;
        isOpen = open;
        bool canEdit = open && !readOnly;
        canvasGroup.blocksRaycasts = canEdit;
        canvasGroup.interactable = canEdit;
        grid.SetEditingEnabled(canEdit);
        if (readOnlyOverlay != null)
        {
            if (open && readOnly && readOnlyFont == null && readOnlyMessage != null && readOnlyFontSource != null)
            {
                readOnlyFont = TMP_FontAsset.CreateFontAsset(readOnlyFontSource);
                if (readOnlyFont != null)
                    readOnlyMessage.font = readOnlyFont;
            }
            readOnlyOverlay.SetActive(open && readOnly);
        }
        if (!canEdit)
            CodeBlockTooltip.Hide();
        if (!open)
        {
            panning = false;
            return;
        }

        if (!preserveView)
        {
            status.text = "";
            if (!wasOpen)
                ResetView();
        }
    }

    private void OnEnable() => CodeEditor.OnCodeRefreshed += ClearTestResult;

    private void OnDisable() => CodeEditor.OnCodeRefreshed -= ClearTestResult;

    private void OnDestroy()
    {
        if (tutorialOverlay != null)
            Destroy(tutorialOverlay.gameObject);
        if (readOnlyFont != null)
            Destroy(readOnlyFont);
    }

    private void ClearTestResult(System.Collections.Generic.IReadOnlyList<CodeChain> chains)
    {
        if (status != null)
            status.text = "";
    }

    private void Update()
    {
        if (!isOpen || viewport == null)
            return;

        if (lastViewportSize != viewport.rect.size)
            ApplyScale();
        if (grid.TutorialPending || showingTutorial)
            return;
        if (fallbackLog != null && WaveManager.Instance != null && WaveManager.Instance.IsCodingLogUnlocked)
            fallbackLog.text = string.Join("\n\n", WaveManager.Instance.CodingLog);

        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 screenPoint = Input.mousePosition;
        bool overGrid = RectTransformUtility.RectangleContainsScreenPoint(viewport, screenPoint, uiCamera);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, screenPoint, uiCamera, out Vector2 pointer))
            return;

        if (Input.GetMouseButtonDown(2) && overGrid && !Input.GetMouseButton(0))
        {
            panning = true;
            previousPointer = pointer;
            CodeBlockTooltip.Hide();
        }

        if (!Input.GetMouseButton(2) || Input.GetMouseButton(0))
            panning = false;
        if (panning)
        {
            board.anchoredPosition += pointer - previousPointer;
            previousPointer = pointer;
            backdrop.SetVerticesDirty();
        }

        float scroll = Input.mouseScrollDelta.y;
        if (!overGrid || scroll == 0f || Input.GetMouseButton(0))
            return;

        float previousZoom = zoom;
        zoom = Mathf.Clamp(zoom * Mathf.Pow(1.15f, scroll), 0.4f, 3f);
        board.anchoredPosition = pointer + (board.anchoredPosition - pointer) * (zoom / previousZoom);
        ApplyScale();
    }

    private void ResetView()
    {
        zoom = 1f;
        board.anchoredPosition = homePosition;
        ApplyScale();
    }

    private void ApplyScale()
    {
        lastViewportSize = viewport.rect.size;
        float width = grid.Width * grid.CellSize;
        float height = grid.Height * grid.CellSize;
        board.sizeDelta = new Vector2(width, height);
        float fit = Mathf.Max(0.01f, Mathf.Min((lastViewportSize.x - boardPadding * 2f) / width,
            (lastViewportSize.y - boardPadding * 2f) / height));
        board.localScale = Vector3.one * (fit * zoom);
        backdrop.SetVerticesDirty();
    }

    private void CheckCode()
    {
        if (!grid.CanEdit)
            return;

        status.text = grid.CheckInstalledCode();
    }

    public void ShowTutorial()
    {
        if (!grid.TutorialPending)
            return;
        if (tutorialOverlay == null)
            CreateTutorialOverlay();

        highlightedBlock = null;
        highlightedSource = highlightedDestination = default;
        tutorialOverlay.alpha = 0f;
        tutorialOverlay.gameObject.SetActive(true);
        showingTutorial = true;
        centerButton.interactable = checkCodeButton.interactable = doneButton.interactable = false;
    }

    private void CreateTutorialOverlay()
    {
        var overlay = new GameObject("Code Tutorial", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(overlay, gameObject.scene);
        overlay.layer = canvas.gameObject.layer;
        Canvas overlayCanvas = overlay.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.WorldSpace;
        overlayCanvas.worldCamera = canvas.worldCamera;
        overlayCanvas.sortingLayerID = canvas.sortingLayerID;
        overlayCanvas.sortingOrder = canvas.sortingOrder + 1;
        overlay.transform.localScale = Vector3.one * TutorialCanvasScale;
        tutorialOverlay = overlay.GetComponent<CanvasGroup>();
        tutorialOverlay.blocksRaycasts = false;

        var mask = new GameObject("Spotlight", typeof(RectTransform), typeof(CanvasRenderer), typeof(CodeTutorialSpotlight));
        mask.layer = overlay.layer;
        spotlight = mask.GetComponent<CodeTutorialSpotlight>();
        spotlight.raycastTarget = false;
        RectTransform maskRect = spotlight.rectTransform;
        maskRect.SetParent(overlay.transform, false);
        maskRect.anchorMin = Vector2.zero;
        maskRect.anchorMax = Vector2.one;
        maskRect.offsetMin = maskRect.offsetMax = Vector2.zero;

        var hint = new GameObject("Instructions", typeof(RectTransform), typeof(Text));
        hint.layer = overlay.layer;
        tutorialHint = hint.GetComponent<Text>();
        tutorialHint.font = readOnlyFontSource != null ? readOnlyFontSource : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tutorialHint.color = Color.white;
        tutorialHint.fontStyle = FontStyle.Bold;
        tutorialHint.alignment = TextAnchor.MiddleCenter;
        tutorialHint.raycastTarget = false;
        tutorialHint.horizontalOverflow = HorizontalWrapMode.Overflow;
        RectTransform hintRect = tutorialHint.rectTransform;
        hintRect.SetParent(overlay.transform, false);
        hintRect.anchorMin = hintRect.anchorMax = Vector2.one * 0.5f;
        hintRect.sizeDelta = new Vector2(800f, 44f);
    }

    private void LateUpdate()
    {
        if (!showingTutorial || !isOpen)
        {
            centerButton.interactable = checkCodeButton.interactable = doneButton.interactable = !grid.TutorialPending;
            return;
        }

        CodeBlock block = grid.TutorialBlock;
        if (block != highlightedBlock || highlightedSource.width == 0f)
        {
            highlightedBlock = block;
            tutorialHint.text = block != null
                ? $"{block.GetDisplayText()} 블록을 초록색 빈칸으로 옮기세요."
                : "CIR → DMG → +10 연결 완료. Done또는 ESC를 눌러 시작하세요.";
        }
        doneButton.interactable = block == null;

        FitTutorialCanvasToCamera();
        Rect source = TutorialRect((RectTransform)(block != null ? block.transform : doneButton.transform));
        Rect destination = default;
        if (block != null)
        {
            Vector3 center = grid.GridToLocalCenter(grid.TutorialDestination, block.GridWidth);
            Vector3 half = new Vector3(block.GridWidth * grid.CellSize, grid.CellSize) * 0.5f;
            destination = ProjectBounds(board.TransformPoint(center - half), board.TransformPoint(center + half));
        }

        float blend = 1f - Mathf.Exp(-22f * Time.unscaledDeltaTime);
        highlightedSource = highlightedSource.width > 0f && (block == null || !block.IsDragging)
            ? BlendRect(highlightedSource, source, blend) : source;
        highlightedDestination = destination.width > 0f && highlightedDestination.width > 0f
            ? BlendRect(highlightedDestination, destination, blend) : destination;
        tutorialOverlay.alpha = Mathf.MoveTowards(tutorialOverlay.alpha, 1f, Time.unscaledDeltaTime * 5f);
        spotlight.Show(highlightedSource, highlightedDestination, 1f);

        float screenHeight = spotlight.rectTransform.rect.height;
        float top = Mathf.Max(source.yMax, destination.yMax);
        float bottom = destination.width > 0f ? Mathf.Min(source.yMin, destination.yMin) : source.yMin;
        float hintHeight = Mathf.Clamp(screenHeight * 0.07f, 30f, 55f);
        float hintY = top + hintHeight <= screenHeight * 0.5f
            ? top + hintHeight * 0.5f : bottom - hintHeight * 0.5f;
        tutorialHint.rectTransform.anchoredPosition = new Vector2(0f,
            Mathf.Clamp(hintY, -screenHeight * 0.5f + hintHeight * 0.5f, screenHeight * 0.5f - hintHeight * 0.5f));
        tutorialHint.fontSize = Mathf.Clamp(Mathf.RoundToInt(screenHeight * 0.027f), 13, 26);
    }

    private void FitTutorialCanvasToCamera()
    {
        var overlayRect = (RectTransform)tutorialOverlay.transform;
        Camera camera = canvas.worldCamera;
        if (camera == null)
        {
            overlayRect.SetPositionAndRotation(canvas.transform.position, canvas.transform.rotation);
            Vector2 size = ((RectTransform)canvas.transform).rect.size;
            Vector3 scale = canvas.transform.lossyScale;
            overlayRect.sizeDelta = Vector2.Scale(size, new Vector2(scale.x, scale.y)) / TutorialCanvasScale;
            return;
        }

        float depth = camera.WorldToViewportPoint(canvas.transform.position).z;
        Vector3 bottomLeft = camera.ViewportToWorldPoint(new Vector3(0f, 0f, depth));
        Vector3 topRight = camera.ViewportToWorldPoint(new Vector3(1f, 1f, depth));
        overlayRect.SetPositionAndRotation((bottomLeft + topRight) * 0.5f, camera.transform.rotation);
        Vector3 span = camera.transform.InverseTransformVector(topRight - bottomLeft);
        overlayRect.sizeDelta = new Vector2(span.x, span.y) / TutorialCanvasScale;
    }

    private Rect TutorialRect(RectTransform target)
    {
        target.GetWorldCorners(tutorialCorners);
        return ProjectBounds(tutorialCorners[0], tutorialCorners[2]);
    }

    private Rect ProjectBounds(Vector3 first, Vector3 second)
    {
        RectTransform maskRect = spotlight.rectTransform;
        Vector2 a = maskRect.InverseTransformPoint(first);
        Vector2 b = maskRect.InverseTransformPoint(second);
        return Rect.MinMaxRect(Mathf.Min(a.x, b.x) - 3f, Mathf.Min(a.y, b.y) - 3f,
            Mathf.Max(a.x, b.x) + 3f, Mathf.Max(a.y, b.y) + 3f);
    }

    private static Rect BlendRect(Rect from, Rect to, float amount)
    {
        return new Rect(Vector2.Lerp(from.position, to.position, amount), Vector2.Lerp(from.size, to.size, amount));
    }
}

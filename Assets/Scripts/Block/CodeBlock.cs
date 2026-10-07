using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CodeBlock
    : MonoBehaviour,
        IPointerDownHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
{
    private bool isPreview;
    private WeaponDefinition linkedWeapon;
    public WeaponDefinition LinkedWeapon => linkedWeapon;

    [SerializeField]
    private TMP_Text text;

    [SerializeField]
    private Outline outline;

    private BlockDefinition definition;
    private CodeGrid grid;

    private RectTransform rectTransform;
    private Canvas canvas;
    private bool isDragging;
    private Vector3 dragWorldOffset;
    private Vector2Int dragOriginalPosition;
    private BlockType? runtimeBlockType;

    private int dragCellOffset;

    public BlockDefinition Definition => definition;
    public BlockType CodeType => runtimeBlockType ?? definition.blockType;

    public Vector2Int GridPosition { get; private set; }
    private int value;
    public int Value => value;

    public int GridWidth
    {
        get
        {
            if (definition == null)
                return 0;

            return GetDisplayText().Length;
        }
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        canvas = GetComponentInParent<Canvas>();
        if (outline == null)
        {
            outline = GetComponent<Outline>();
        }

        ClearError();
    }

    public void Initialize(
        BlockDefinition definition,
        CodeGrid grid,
        int value = 0,
        BlockType? runtimeBlockType = null
    )
    {
        this.definition = definition;
        this.grid = grid;
        this.value = value;
        this.runtimeBlockType = runtimeBlockType;

        if (text != null)
        {
            text.text = GetDisplayText();

            // [추가] Code의 구조/동작/역할(BlockCategory)에 따라 글자 색을 다르게 표시한다.
            text.color = BlockCategoryColor.GetColor(definition.category);
        }

        UpdateSize();

        ClearError();
    }
    public void InitializePreview(BlockDefinition definition, int value = 0, float cellSize = 75f)
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (rectTransform == null)
        {
            Debug.LogError("[CodeBlock] Preview에는 RectTransform이 필요합니다.", this);
            return;
        }

        isPreview = true;

        this.definition = definition;
        this.value = value;
        this.grid = null;
        runtimeBlockType = null;

        if (text != null)
        {
            text.text = GetDisplayText();
            text.color = BlockCategoryColor.GetColor(definition.category);
        }

        float width = GridWidth * cellSize;
        float height = cellSize;

        rectTransform.sizeDelta =
            new Vector2(width, height);

        UpdateTextSize();

        ClearError();
    }

    private string GetDisplayText()
    {
        if (definition == null)
            return string.Empty;

        if (CodeType == BlockType.PARENTHESIS_BUNDLE)
        {
            return "()";
        }

        if (CodeType == BlockType.PARENTHESIS_OPEN)
            return "(";
        if (CodeType == BlockType.PARENTHESIS_CLOSE)
            return ")";

        if (!definition.hasValue)
            return definition.displayText;

        return $"{definition.displayText}{value}";
    }

    public void SetLinkedWeapon(WeaponDefinition weapon)
    {
        linkedWeapon = weapon;
    }

    private void UpdateSize()
    {
        if (definition == null || grid == null)
            return;

        float width = GridWidth * grid.CellSize;

        float height = grid.CellSize;

        rectTransform.sizeDelta = new Vector2(width, height);

        UpdateTextSize();
    }

    private void UpdateTextSize()
    {
        if (text == null)
            return;

        RectTransform textRect = text.GetComponent<RectTransform>();

        if (textRect == null || rectTransform == null)
            return;

        textRect.sizeDelta = rectTransform.sizeDelta;
    }

    public void SetGridPosition(Vector2Int position)
    {
        GridPosition = position;

        transform.position = grid.GridToWorldCenter(position, GridWidth);
    }

    public void SetError(bool value)
    {
        if (outline == null)
            return;

        outline.enabled = value;
    }

    public void ClearError()
    {
        if (outline == null)
            return;

        outline.enabled = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"블록 선택: {definition.displayText}");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isPreview || grid == null || canvas == null || definition == null)
        {
            isDragging = false;
            return;
        }

        isDragging = true;
        dragOriginalPosition = GridPosition;
        Debug.Log($"드래그 시작: {definition.displayText}");

        transform.SetAsLastSibling();

        // 현재 마우스 위치를 그리드 좌표로 변환
        Vector3 pointerWorldPosition = GetWorldPosition(eventData);
        Vector2Int mouseGridPosition = grid.WorldToGrid(pointerWorldPosition);
        dragWorldOffset = transform.position - pointerWorldPosition;

        // 마우스가 블록의 몇 번째 셀을 잡았는지 계산
        dragCellOffset = mouseGridPosition.x - GridPosition.x;

        // 범위를 안전하게 제한
        dragCellOffset = Mathf.Clamp(dragCellOffset, 0, GridWidth - 1);

    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || canvas == null || grid == null)
            return;

        Vector3 worldPosition = GetWorldPosition(eventData);
        rectTransform.position = worldPosition + dragWorldOffset;

    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        isDragging = false;
        Debug.Log($"드래그 종료: {definition.displayText}");

        Vector2Int pointerGridPosition = grid.WorldToGrid(GetWorldPosition(eventData));

        if (IsParenthesisBundle())
        {
            Vector2Int bundlePosition = new Vector2Int(
                pointerGridPosition.x - dragCellOffset,
                pointerGridPosition.y
            );
            if (!grid.TryExpandParenthesisBundle(this, bundlePosition))
                SetGridPosition(GridPosition);

            return;
        }

        Vector2Int targetPosition = new Vector2Int(
            pointerGridPosition.x - dragCellOffset,
            pointerGridPosition.y
        );

        if (!grid.CanPlace(targetPosition, GridWidth, this))
        {
            SetGridPosition(dragOriginalPosition);
            return;
        }

        grid.UnregisterBlock(this);

        SetGridPosition(targetPosition);

        grid.RegisterBlock(this);

        grid.NotifyCodeChanged();
    }

    private bool IsParenthesisBundle()
    {
        return definition != null && CodeType == BlockType.PARENTHESIS_BUNDLE;
    }

    private Vector3 GetWorldPosition(PointerEventData eventData)
    {
        RectTransform canvasRect = canvas.transform as RectTransform;

        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            canvasRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector3 worldPosition
        );

        return worldPosition;
    }

    public bool IsRightNextTo(CodeBlock other)
    {
        if (GridPosition.y != other.GridPosition.y)
        {
            return false;
        }

        int rightEdge = GridPosition.x + GridWidth;

        return rightEdge == other.GridPosition.x;
    }
}

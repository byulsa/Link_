using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CodeBlock : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private Outline outline;

    private BlockDefinition definition;
    private CodeGrid grid;

    private RectTransform rectTransform;
    private Canvas canvas;

    public BlockDefinition Definition => definition;

    public Vector2Int GridPosition { get; private set; }

    public int GridWidth
    {
        get
        {
            if (definition == null)
                return 0;

            return definition.displayText.Length;
        }
    }

    private void Awake()
    {
        rectTransform =
            GetComponent<RectTransform>();

        canvas =
            GetComponentInParent<Canvas>();

        if (outline == null)
        {
            outline =
                GetComponent<Outline>();
        }

        ClearError();
    }

    public void Initialize(
        BlockDefinition definition,
        CodeGrid grid)
    {
        this.definition = definition;
        this.grid = grid;

        if (text != null)
            text.text = definition.displayText;

        UpdateSize();

        ClearError();
    }

    private void UpdateSize()
    {
        if (definition == null || grid == null)
            return;

        float width =
            GridWidth * grid.CellSize;

        float height =
            grid.CellSize;

        rectTransform.sizeDelta =
            new Vector2(width, height);

        UpdateTextSize();
    }

    private void UpdateTextSize()
    {
        if (text == null)
            return;

        RectTransform textRect =
            text.GetComponent<RectTransform>();

        textRect.sizeDelta =
            rectTransform.sizeDelta;
    }

    public void SetGridPosition(
        Vector2Int position)
    {
        GridPosition = position;

        transform.position =
            grid.GridToWorldCenter(
                position,
                GridWidth
            );
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

    public void OnPointerDown(
        PointerEventData eventData)
    {
        Debug.Log(
            $"블록 선택: {definition.displayText}"
        );
    }

    public void OnBeginDrag(
        PointerEventData eventData)
    {
        Debug.Log(
            $"드래그 시작: {definition.displayText}"
        );

        transform.SetAsLastSibling();
    }

    public void OnDrag(
        PointerEventData eventData)
    {
        if (canvas == null)
            return;

        RectTransform canvasRect =
            canvas.transform as RectTransform;

        if (RectTransformUtility
            .ScreenPointToWorldPointInRectangle(
                canvasRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector3 worldPosition))
        {
            rectTransform.position =
                worldPosition;
        }
    }

    public void OnEndDrag(
        PointerEventData eventData)
    {
        Debug.Log(
            $"드래그 종료: {definition.displayText}"
        );

        Vector2Int targetPosition =
            grid.WorldToGrid(
                transform.position
            );

        if (!grid.CanPlace(
                targetPosition,
                GridWidth,
                this))
        {
            SetGridPosition(GridPosition);
            return;
        }

        grid.UnregisterBlock(this);

        SetGridPosition(targetPosition);

        grid.RegisterBlock(this);

        grid.NotifyCodeChanged();
    }

    public bool IsRightNextTo(
        CodeBlock other)
    {
        if (GridPosition.y !=
            other.GridPosition.y)
        {
            return false;
        }

        int rightEdge =
            GridPosition.x + GridWidth;

        return rightEdge ==
               other.GridPosition.x;
    }
}
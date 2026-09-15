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

    // 드래그 시작 시 마우스가 잡은 셀의 위치
    private int dragCellOffset;

    public BlockDefinition Definition => definition;

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

    public void Initialize(BlockDefinition definition, CodeGrid grid, int value = 0)
    {
        this.definition = definition;
        this.grid = grid;
        this.value = value;

        if (text != null)
            text.text = GetDisplayText();

        UpdateSize();

        ClearError();
    }
    private string GetDisplayText()
    {
        if (definition == null)
            return string.Empty;

        if (!definition.hasValue)
            return definition.displayText;

        return $"{definition.displayText}{value}";
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

        // 현재 마우스 위치를 그리드 좌표로 변환
        Vector2Int mouseGridPosition =
            grid.WorldToGrid(
                GetWorldPosition(eventData)
            );

        // 마우스가 블록의 몇 번째 셀을 잡았는지 계산
        dragCellOffset =
            mouseGridPosition.x -
            GridPosition.x;

        // 범위를 안전하게 제한
        dragCellOffset =
            Mathf.Clamp(
                dragCellOffset,
                0,
                GridWidth - 1
            );
    }

    public void OnDrag(
        PointerEventData eventData)
    {
        if (canvas == null || grid == null)
            return;

        Vector3 worldPosition =
            GetWorldPosition(eventData);

        // 마우스가 위치한 그리드 셀
        Vector2Int mouseGridPosition =
            grid.WorldToGrid(worldPosition);

        // 마우스가 잡았던 셀이
        // 현재 마우스 셀에 오도록 시작 위치 계산
        Vector2Int targetPosition =
            new Vector2Int(
                mouseGridPosition.x - dragCellOffset,
                mouseGridPosition.y
            );

        // 화면에서 실제 블록도 해당 위치로 Snap
        if (grid.CanPlace(
                targetPosition,
                GridWidth,
                this))
        {
            rectTransform.position =
                grid.GridToWorldCenter(
                    targetPosition,
                    GridWidth
                );
        }
        else
        {
            // 배치할 수 없는 위치라면
            // 일단 마우스를 따라가도록 하지 않고
            // 마지막 정상 위치 유지
        }
    }

    public void OnEndDrag(
        PointerEventData eventData)
    {
        Debug.Log(
            $"드래그 종료: {definition.displayText}"
        );

        Vector2Int mouseGridPosition =
            grid.WorldToGrid(
                GetWorldPosition(eventData)
            );

        Vector2Int targetPosition =
            new Vector2Int(
                mouseGridPosition.x - dragCellOffset,
                mouseGridPosition.y
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

    private Vector3 GetWorldPosition(
        PointerEventData eventData)
    {
        RectTransform canvasRect =
            canvas.transform as RectTransform;

        RectTransformUtility
            .ScreenPointToWorldPointInRectangle(
                canvasRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector3 worldPosition
            );

        return worldPosition;
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
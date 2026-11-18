using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class CodeGridBackdrop : MaskableGraphic
{
    [SerializeField] private CodeGrid grid;
    [SerializeField] private Color32 boardColor = new Color32(19, 40, 52, 255);

    protected override void OnPopulateMesh(VertexHelper vertices)
    {
        vertices.Clear();
        if (grid == null)
            return;

        Rect area = rectTransform.rect;
        Transform board = grid.transform;
        Vector3 cellEnd = rectTransform.InverseTransformPoint(board.TransformPoint(Vector3.right * grid.CellSize));
        Vector2 center = rectTransform.InverseTransformPoint(board.position);
        float cell = Mathf.Abs(cellEnd.x - center.x);
        if (cell < 1f)
            return;

        Vector2 size = new Vector2(grid.Width, grid.Height) * cell;
        Vector2 origin = center - size * 0.5f;
        Color32 gridColor = color;

        Vector2 min = Vector2.Max(area.min, origin);
        Vector2 max = Vector2.Min(area.max, origin + size);
        if (min.x < max.x && min.y < max.y)
            AddQuad(vertices, new Rect(min, max - min), boardColor);

        float startX = area.xMin + Mathf.Repeat(origin.x - area.xMin, cell);
        for (float x = startX; x < area.xMax; x += cell)
            AddQuad(vertices, Rect.MinMaxRect(x, area.yMin, Mathf.Min(x + 1f, area.xMax), area.yMax), gridColor);

        float startY = area.yMin + Mathf.Repeat(origin.y - area.yMin, cell);
        for (float y = startY; y < area.yMax; y += cell)
            AddQuad(vertices, Rect.MinMaxRect(area.xMin, y, area.xMax, Mathf.Min(y + 1f, area.yMax)), gridColor);
    }

    private static void AddQuad(VertexHelper vertices, Rect rect, Color32 color)
    {
        int start = vertices.currentVertCount;
        vertices.AddVert(rect.min, color, Vector2.zero);
        vertices.AddVert(new Vector2(rect.xMin, rect.yMax), color, Vector2.zero);
        vertices.AddVert(rect.max, color, Vector2.zero);
        vertices.AddVert(new Vector2(rect.xMax, rect.yMin), color, Vector2.zero);
        vertices.AddTriangle(start, start + 1, start + 2);
        vertices.AddTriangle(start + 2, start + 3, start);
    }
}

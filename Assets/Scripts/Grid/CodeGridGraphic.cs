using UnityEngine;
using UnityEngine.UI;

public class CodeGridGraphic : Graphic
{
    [SerializeField] private int width = 10;
    [SerializeField] private int height = 10;
    [SerializeField] private float cellSize = 75f;

    [SerializeField] private float lineWidth = 2f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        float totalWidth = width * cellSize;
        float totalHeight = height * cellSize;

        float startX = -totalWidth * 0.5f;
        float startY = -totalHeight * 0.5f;

        // 세로선
        for (int x = 0; x <= width; x++)
        {
            float xPosition = startX + x * cellSize;

            AddLine(
                vh,
                new Vector2(xPosition, startY),
                new Vector2(xPosition, startY + totalHeight)
            );
        }

        // 가로선
        for (int y = 0; y <= height; y++)
        {
            float yPosition = startY + y * cellSize;

            AddLine(
                vh,
                new Vector2(startX, yPosition),
                new Vector2(startX + totalWidth, yPosition)
            );
        }
    }

    private void AddLine(
        VertexHelper vh,
        Vector2 start,
        Vector2 end)
    {
        Vector2 direction = (end - start).normalized;

        Vector2 perpendicular =
            new Vector2(-direction.y, direction.x);

        Vector2 offset =
            perpendicular * (lineWidth * 0.5f);

        int index = vh.currentVertCount;

        UIVertex vertex = UIVertex.simpleVert;

        vertex.color = color;

        vertex.position = start - offset;
        vh.AddVert(vertex);

        vertex.position = start + offset;
        vh.AddVert(vertex);

        vertex.position = end + offset;
        vh.AddVert(vertex);

        vertex.position = end - offset;
        vh.AddVert(vertex);

        vh.AddTriangle(
            index,
            index + 1,
            index + 2
        );

        vh.AddTriangle(
            index + 2,
            index + 3,
            index
        );
    }
}
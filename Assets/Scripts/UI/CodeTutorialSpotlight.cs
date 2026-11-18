using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class CodeTutorialSpotlight : MaskableGraphic
{
    private readonly float[] edgesX = new float[6];
    private readonly float[] edgesY = new float[6];
    private Rect source;
    private Rect destination;
    private Color32 dim;
    private readonly Color32 border = new Color32(105, 255, 158, 255);

    public void Show(Rect sourceRect, Rect destinationRect, float opacity)
    {
        source = sourceRect;
        destination = destinationRect;
        dim = new Color32(0, 0, 0, (byte)Mathf.RoundToInt(opacity * 230f));
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vertices)
    {
        vertices.Clear();
        Rect area = rectTransform.rect;
        SplitEdges(edgesX, area.xMin, area.xMax, source.xMin, source.xMax, destination.xMin, destination.xMax);
        SplitEdges(edgesY, area.yMin, area.yMax, source.yMin, source.yMax, destination.yMin, destination.yMax);
        for (int x = 0; x < edgesX.Length - 1; x++)
        for (int y = 0; y < edgesY.Length - 1; y++)
        {
            Rect tile = Rect.MinMaxRect(edgesX[x], edgesY[y], edgesX[x + 1], edgesY[y + 1]);
            if (!source.Contains(tile.center) && !destination.Contains(tile.center))
                AddQuad(vertices, tile, dim);
        }
        DrawBorder(vertices, source);
        DrawBorder(vertices, destination);
    }

    private static void SplitEdges(float[] edges, float min, float max, float a, float b, float c, float d)
    {
        edges[0] = min;
        edges[1] = max;
        edges[2] = Mathf.Clamp(a, min, max);
        edges[3] = Mathf.Clamp(b, min, max);
        edges[4] = Mathf.Clamp(c, min, max);
        edges[5] = Mathf.Clamp(d, min, max);
        Array.Sort(edges);
    }

    private void DrawBorder(VertexHelper vertices, Rect rect)
    {
        if (rect.width <= 0f || rect.height <= 0f)
            return;

        const float stroke = 2f;
        AddQuad(vertices, new Rect(rect.xMin, rect.yMin, rect.width, stroke), border);
        AddQuad(vertices, new Rect(rect.xMin, rect.yMax - stroke, rect.width, stroke), border);
        AddQuad(vertices, new Rect(rect.xMin, rect.yMin, stroke, rect.height), border);
        AddQuad(vertices, new Rect(rect.xMax - stroke, rect.yMin, stroke, rect.height), border);
    }

    private static void AddQuad(VertexHelper vertices, Rect rect, Color32 tint)
    {
        if (rect.width <= 0f || rect.height <= 0f)
            return;
        int first = vertices.currentVertCount;
        vertices.AddVert(rect.min, tint, Vector2.zero);
        vertices.AddVert(new Vector2(rect.xMin, rect.yMax), tint, Vector2.zero);
        vertices.AddVert(rect.max, tint, Vector2.zero);
        vertices.AddVert(new Vector2(rect.xMax, rect.yMin), tint, Vector2.zero);
        vertices.AddTriangle(first, first + 1, first + 2);
        vertices.AddTriangle(first + 2, first + 3, first);
    }
}

using System.Collections.Generic;
using UnityEngine;

public class CodeGrid : MonoBehaviour
{
    [SerializeField] private int width = 10;
    [SerializeField] private int height = 10;
    [SerializeField] private float cellSize = 75f;

    [SerializeField] private CodeEditor editor;

    private readonly Dictionary<Vector2Int, CodeBlock> occupiedCells = new();

    public float CellSize => cellSize;
    public int Width => width;
    public int Height => height;

    public bool IsInside(Vector2Int position, int blockWidth)
    {
        return position.x >= 0 &&
               position.y >= 0 &&
               position.x + blockWidth <= width &&
               position.y < height;
    }

    public bool CanPlace(
        Vector2Int position,
        int blockWidth,
        CodeBlock ignoreBlock = null)
    {
        if (!IsInside(position, blockWidth))
            return false;

        for (int x = 0; x < blockWidth; x++)
        {
            Vector2Int cell = new Vector2Int(
                position.x + x,
                position.y
            );

            if (occupiedCells.TryGetValue(cell, out CodeBlock block))
            {
                if (block != ignoreBlock)
                    return false;
            }
        }

        return true;
    }

    public void ClearBlocks()
    {
        occupiedCells.Clear();
    }

    public void RegisterBlock(CodeBlock block)
    {
        for (int x = 0; x < block.GridWidth; x++)
        {
            Vector2Int cell = new Vector2Int(
                block.GridPosition.x + x,
                block.GridPosition.y
            );

            occupiedCells[cell] = block;
        }
    }

    public void UnregisterBlock(CodeBlock block)
    {
        for (int x = 0; x < block.GridWidth; x++)
        {
            Vector2Int cell = new Vector2Int(
                block.GridPosition.x + x,
                block.GridPosition.y
            );

            if (occupiedCells.TryGetValue(cell, out CodeBlock current))
            {
                if (current == block)
                    occupiedCells.Remove(cell);
            }
        }
    }

    public Vector3 GridToWorldCenter(
        Vector2Int position,
        int blockWidth)
    {
        float totalWidth = width * cellSize;
        float totalHeight = height * cellSize;

        float localX =
            position.x * cellSize +
            blockWidth * cellSize * 0.5f -
            totalWidth * 0.5f;

        float localY =
            position.y * cellSize +
            cellSize * 0.5f -
            totalHeight * 0.5f;

        return transform.TransformPoint(
            new Vector3(localX, localY, 0f)
        );
    }

    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        Vector3 localPosition =
            transform.InverseTransformPoint(worldPosition);

        float totalWidth = width * cellSize;
        float totalHeight = height * cellSize;

        float gridX =
            localPosition.x + totalWidth * 0.5f;

        float gridY =
            localPosition.y + totalHeight * 0.5f;

        return new Vector2Int(
            Mathf.FloorToInt(gridX / cellSize),
            Mathf.FloorToInt(gridY / cellSize)
        );
    }

    public List<CodeBlock> GetBlocks()
    {
        List<CodeBlock> blocks = new List<CodeBlock>();

        foreach (CodeBlock block in GetComponentsInChildren<CodeBlock>())
        {
            blocks.Add(block);
        }

        return blocks;
    }

    public void NotifyCodeChanged()
    {
        if (editor == null)
        {
            Debug.LogWarning(
                "[CodeGrid] CodeEditor가 없습니다."
            );

            return;
        }

        editor.RefreshCode();
    }
}
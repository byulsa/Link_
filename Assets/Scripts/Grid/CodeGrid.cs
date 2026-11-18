using System.Collections.Generic;
using UnityEngine;

public class CodeGrid : MonoBehaviour
{
    [SerializeField]
    private int width = 10;

    [SerializeField]
    private int height = 10;

    [SerializeField]
    private float cellSize = 75f;

    [SerializeField]
    private CodeEditor editor;
    [SerializeField, Min(0)] private int codeSellPrice = 5;

    private readonly Dictionary<Vector2Int, CodeBlock> occupiedCells = new();
    private CodeBlock selectedBlock;
    private RectTransform editingViewport;
    private bool editingEnabled = true;
    private CodeBlock[] tutorialBlocks;
    private int tutorialStep;
    private Vector2Int tutorialDestination;
    private int tutorialEndColumn;

    public bool TutorialPending => tutorialBlocks != null && tutorialStep < tutorialBlocks.Length;
    public CodeBlock TutorialBlock => TutorialPending ? tutorialBlocks[tutorialStep] : null;
    public Vector2Int TutorialDestination => tutorialDestination;

    public void BeginTutorial(CodeBlock[] blocks, Vector2Int destination)
    {
        tutorialBlocks = blocks;
        tutorialStep = 0;
        tutorialDestination = destination;
        tutorialEndColumn = destination.x;
        foreach (CodeBlock block in blocks)
            tutorialEndColumn += block.GridWidth;
    }

    public bool CanEditBlock(CodeBlock block) => CanEdit && (!TutorialPending || block == TutorialBlock);

    public bool CanEdit => editingEnabled && !WaveManager.IsBattleActive;

    public void SetEditingEnabled(bool enabled)
    {
        editingEnabled = enabled;
        if (!enabled && selectedBlock != null)
            selectedBlock.CancelDrag();
    }

    public float CellSize => cellSize;
    public int Width => width;
    public int Height => height;

    public void ExpandTo(int size)
    {
        if (size <= width || size <= height)
            return;

        // Keep the expanded board within the original on-screen footprint.
        float scale = Mathf.Min(width, height) / (float)size;
        transform.localScale *= scale;
        width = size;
        height = size;

        foreach (CodeBlock block in new HashSet<CodeBlock>(occupiedCells.Values))
            block.SetGridPosition(block.GridPosition);

        if (TryGetComponent(out CodeGridGraphic graphic))
            graphic.SetVerticesDirty();
    }

    public void SetEditingViewport(RectTransform viewport) => editingViewport = viewport;

    public bool IsOverEditingViewport(Vector2 position, Camera camera) =>
        editingViewport == null || RectTransformUtility.RectangleContainsScreenPoint(editingViewport, position, camera);

    public string CheckInstalledCode() => editor != null ? editor.CheckInstalledCode(selectedBlock) : "Editor unavailable";

    public bool IsInside(Vector2Int position, int blockWidth)
    {
        return position.x >= 0
            && position.y >= 0
            && position.x + blockWidth <= width
            && position.y < height;
    }

    public bool CanPlace(Vector2Int position, int blockWidth, CodeBlock ignoreBlock = null, CodeBlock secondIgnoredBlock = null)
    {
        if (!IsInside(position, blockWidth))
            return false;

        if (TutorialPending && ignoreBlock != TutorialBlock && position.y == tutorialDestination.y
            && position.x < tutorialEndColumn && position.x + blockWidth > tutorialDestination.x)
            return false;

        for (int x = 0; x < blockWidth; x++)
        {
            Vector2Int cell = new Vector2Int(position.x + x, position.y);

            if (occupiedCells.TryGetValue(cell, out CodeBlock block))
            {
                if (block != ignoreBlock && block != secondIgnoredBlock)
                    return false;
            }
        }

        return true;
    }

    public void ClearBlocks()
    {
        SelectBlock(null);
        occupiedCells.Clear();
    }

    public void SelectBlock(CodeBlock block)
    {
        if (selectedBlock != null)
            selectedBlock.SetSelected(false);
        selectedBlock = block;
        if (selectedBlock != null)
            selectedBlock.SetSelected(true);
    }

    public void SellSelectedBlock()
    {
        if (TutorialPending)
            return;

        CodeBlock block = selectedBlock;
        if (!CanEdit || block == null || block.IsDragging || !block.isActiveAndEnabled ||
            PointManager.Instance == null)
            return;

        SelectBlock(null);
        if (block.PairedBracket != null && block.PairedBracket.isActiveAndEnabled)
            RemoveBlock(block.PairedBracket);
        RemoveBlock(block);
        CodeBlockTooltip.Hide();
        PointManager.Instance.AddPoint(codeSellPrice);
        NotifyCodeChanged();
    }

    private void RemoveBlock(CodeBlock block)
    {
        UnregisterBlock(block);
        block.gameObject.SetActive(false);
        Destroy(block.gameObject);
    }

    public bool TryMoveBlock(CodeBlock block, Vector2Int position, Vector2Int pointer)
    {
        if (TutorialPending && (block != TutorialBlock || position != tutorialDestination))
            return false;

        if (!CanEdit)
            return false;

        if (!TutorialPending && occupiedCells.TryGetValue(pointer, out CodeBlock other) && other != block)
            return TrySwapBlocks(block, other);

        if (!CanPlace(position, block.GridWidth, block) || position == block.GridPosition)
            return false;

        UnregisterBlock(block);
        block.SetGridPosition(position);
        RegisterBlock(block);
        if (TutorialPending)
        {
            tutorialStep++;
            tutorialDestination.x += block.GridWidth;
            SelectBlock(null);
        }
        NotifyCodeChanged();
        return true;
    }

    private bool TrySwapBlocks(CodeBlock block, CodeBlock other)
    {
        Vector2Int blockTarget = other.GridPosition;
        Vector2Int otherTarget = block.GridPosition;
        if (!CanSwapInto(block, blockTarget, other, otherTarget) && blockTarget.y == otherTarget.y)
        {
            // Keep the original gap when differently sized blocks trade places in one row.
            if (blockTarget.x > otherTarget.x)
                blockTarget.x += other.GridWidth - block.GridWidth;
            else
                otherTarget.x += block.GridWidth - other.GridWidth;
        }

        if (!CanSwapInto(block, blockTarget, other, otherTarget))
            return false;

        UnregisterBlock(block);
        UnregisterBlock(other);
        block.SetGridPosition(blockTarget);
        other.SetGridPosition(otherTarget);
        RegisterBlock(block);
        RegisterBlock(other);
        NotifyCodeChanged();
        return true;
    }

    private bool CanSwapInto(CodeBlock block, Vector2Int blockTarget, CodeBlock other, Vector2Int otherTarget)
    {
        bool overlaps = blockTarget.y == otherTarget.y
            && blockTarget.x < otherTarget.x + other.GridWidth
            && otherTarget.x < blockTarget.x + block.GridWidth;
        return !overlaps && CanPlace(blockTarget, block.GridWidth, block, other)
            && CanPlace(otherTarget, other.GridWidth, block, other);
    }

    public void RegisterBlock(CodeBlock block)
    {
        for (int x = 0; x < block.GridWidth; x++)
        {
            Vector2Int cell = new Vector2Int(block.GridPosition.x + x, block.GridPosition.y);

            occupiedCells[cell] = block;
        }
    }

    public void UnregisterBlock(CodeBlock block)
    {
        for (int x = 0; x < block.GridWidth; x++)
        {
            Vector2Int cell = new Vector2Int(block.GridPosition.x + x, block.GridPosition.y);

            if (occupiedCells.TryGetValue(cell, out CodeBlock current))
            {
                if (current == block)
                    occupiedCells.Remove(cell);
            }
        }
    }

    public Vector3 GridToLocalCenter(Vector2Int position, int blockWidth)
    {
        float totalWidth = width * cellSize;
        float totalHeight = height * cellSize;

        float localX = position.x * cellSize + blockWidth * cellSize * 0.5f - totalWidth * 0.5f;

        float localY = position.y * cellSize + cellSize * 0.5f - totalHeight * 0.5f;

        return new Vector3(localX, localY, 0f);
    }

    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);

        float totalWidth = width * cellSize;
        float totalHeight = height * cellSize;

        float gridX = localPosition.x + totalWidth * 0.5f;

        float gridY = localPosition.y + totalHeight * 0.5f;

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
            Debug.LogWarning("[CodeGrid] CodeEditor가 없습니다.");

            return;
        }

        editor.RefreshCode();
    }

    public bool TryExpandParenthesisBundle(CodeBlock bundle, Vector2Int position)
    {
        if (!CanEdit)
            return false;

        if (
            editor == null
            || bundle == null
            || bundle.Definition == null
            || bundle.CodeType != BlockType.PARENTHESIS_BUNDLE
        )
        {
            Debug.LogError("[CodeGrid] 유효한 괄호 번들이 아니거나 CodeEditor가 없습니다.", this);
            return false;
        }

        if (!CanPlace(position, 2, bundle))
        {
            return false;
        }

        UnregisterBlock(bundle);
        bundle.gameObject.SetActive(false);
        CodeBlock firstBlock = editor.CreateDropBlock(bundle.Definition, position, 0);
        if (firstBlock == null)
        {
            bundle.gameObject.SetActive(true);
            RegisterBlock(bundle);
            return false;
        }

        Destroy(bundle.gameObject);
        return true;
    }

    public bool TryFindEmptyPosition(int blockWidth, out Vector2Int position)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int candidate = new Vector2Int(x, y);

                if (CanPlace(candidate, blockWidth))
                {
                    position = candidate;
                    return true;
                }
            }
        }

        position = default;
        return false;
    }
}

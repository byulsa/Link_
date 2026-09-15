using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CodeEditor : MonoBehaviour
{
    public static event Action<IReadOnlyList<CodeChain>> OnCodeRefreshed;

    [Header("Grid")]
    [SerializeField] private CodeGrid grid;

    [Header("Block")]
    [SerializeField] private CodeBlock blockPrefab;

    [Header("Validation")]
    [SerializeField] private CodeValidator validator;

    [Header("Test Blocks")]
    [SerializeField] private BlockDefinition testWEAP;
    [SerializeField] private BlockDefinition testDMG;
    [SerializeField] private BlockDefinition testPLUS5;
    [SerializeField] private BlockDefinition testMULT;

    private readonly List<CodeChain> chains = new List<CodeChain>();

    public IReadOnlyList<CodeChain> Chains => chains;
    public bool IsReady { get; private set; }

    private void Awake()
    {
        IsReady = false;
    }

    private void Start()
    {
        CreateTestCode();
    }

    public void RefreshCode()
    {
        IsReady = false;
        chains.Clear();

        List<CodeBlock> blocks = GetBlocks();
        ClearAllErrors();
        BuildChains(blocks);
        ValidateChains();
        PrintChains();

        IsReady = true;

        OnCodeRefreshed?.Invoke(chains);
    }

    private void BuildChains(List<CodeBlock> blocks)
    {
        HashSet<CodeBlock> usedBlocks = new HashSet<CodeBlock>();

        foreach (CodeBlock startBlock in blocks)
        {
            if (usedBlocks.Contains(startBlock)) continue;

            CodeChain chain = new CodeChain();
            CodeBlock current = startBlock;

            while (current != null)
            {
                if (usedBlocks.Contains(current)) break;

                CodeNode node = CreateNode(current);
                chain.nodes.Add(node);
                usedBlocks.Add(current);

                current = FindNextBlock(current, blocks, usedBlocks);
            }

            if (chain.nodes.Count > 0)
            {
                chains.Add(chain);
            }
        }
    }

    private CodeBlock FindNextBlock(CodeBlock current, List<CodeBlock> blocks, HashSet<CodeBlock> usedBlocks)
    {
        CodeBlock nextBlock = null;

        foreach (CodeBlock block in blocks)
        {
            if (block == current || usedBlocks.Contains(block) || !IsConnected(current, block)) continue;

            if (nextBlock == null || block.GridPosition.x < nextBlock.GridPosition.x)
            {
                nextBlock = block;
            }
        }

        return nextBlock;
    }

    private bool IsConnected(CodeBlock current, CodeBlock next)
    {
        if (current == null || next == null || current.GridPosition.y != next.GridPosition.y) return false;

        int currentEndX = current.GridPosition.x + GetBlockWidth(current);
        return currentEndX == next.GridPosition.x;
    }

    private int GetBlockWidth(CodeBlock block)
    {
        if (block == null)
            return 1;

        return block.GridWidth;
    }

    private List<CodeBlock> GetBlocks()
    {
        return grid.GetBlocks()
            .OrderBy(block => block.GridPosition.y)
            .ThenBy(block => block.GridPosition.x)
            .ToList();
    }

    private CodeNode CreateNode(CodeBlock block)
    {
        return new CodeNode
        {
            blockType = block.Definition.blockType,
            value = block.Value,
            sourceBlock = block
        };
    }

    private void ValidateChains()
    {
        foreach (CodeChain chain in chains)
        {
            List<CodeBlock> blocks = new List<CodeBlock>();

            foreach (CodeNode node in chain.nodes)
            {
                if (node.sourceBlock != null) blocks.Add(node.sourceBlock);
            }

            ValidationResult result = validator.Validate(blocks);

            if (!result.IsValid)
            {
                foreach (CodeBlock block in result.InvalidBlocks)
                {
                    block.SetError(true);
                }
            }
        }
    }

    private void PrintChains()
    {
        for (int i = 0; i < chains.Count; i++)
        {
            string result = $"Chain {i}: ";
            foreach (CodeNode node in chains[i].nodes)
            {
                result += $"{node.blockType}({node.value}) → ";
            }
        }
    }

    private void ClearAllErrors()
    {
        foreach (CodeBlock block in GetBlocks())
        {
            block.ClearError();
        }
    }

    public void CreateTestCode()
    {
        ClearAllBlocks();

        CreateBlock(testWEAP, new Vector2Int(0, 0));
        CreateBlock(testDMG, new Vector2Int(4, 0));
        // CreateBlock(testPLUS5, new Vector2Int(7, 0));
        // CreateBlock(testMULT, new Vector2Int(9, 0));

        RefreshCode();
    }

    private CodeBlock CreateBlock(BlockDefinition definition, Vector2Int position, int value = 0)
    {
        if (definition == null)
            return null;

        CodeBlock block = Instantiate(blockPrefab, grid.transform);

        block.Initialize(definition, grid, value);

        block.SetGridPosition(position);
        grid.RegisterBlock(block);

        return block;
    }
    public CodeBlock CreateDropBlock(BlockDefinition definition, Vector2Int position, int value)
    {
        Debug.Log(
            $"[CodeEditor] CreateDropBlock 호출: {definition.displayText} / Value: {value} / Position: {position}"
        );
        CodeBlock block = CreateBlock(definition, position, value);

        RefreshCode();

        return block;
    }

    private void ClearAllBlocks()
    {
        foreach (CodeBlock block in grid.GetBlocks())
        {
            Destroy(block.gameObject);
        }
        grid.ClearBlocks();
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CodeEditor : MonoBehaviour
{
    public static event Action<IReadOnlyList<CodeChain>> OnCodeRefreshed;

    [Header("Grid")]
    [SerializeField]
    private CodeGrid grid;

    [Header("Block")]
    [SerializeField]
    private CodeBlock blockPrefab;

    [Header("Validation")]
    [SerializeField]
    private CodeValidator validator;

    [Header("Test Blocks")]
    [SerializeField]
    private BlockDefinition testWEAP;

    [SerializeField]
    private BlockDefinition testDMG;

    [SerializeField]
    private BlockDefinition testPLUS5;

    [SerializeField]
    private BlockDefinition testMULT;

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
        foreach (CodeBlock block in blocks)
            block.ClearError();

        BuildChains(blocks);
        ValidateChains();
        IsReady = true;
        OnCodeRefreshed?.Invoke(chains);
    }

    private void BuildChains(List<CodeBlock> blocks)
    {
        HashSet<CodeBlock> usedBlocks = new HashSet<CodeBlock>();

        foreach (CodeBlock startBlock in blocks)
        {
            if (usedBlocks.Contains(startBlock))
                continue;

            CodeChain chain = new CodeChain();
            CodeBlock current = startBlock;

            while (current != null && usedBlocks.Add(current))
            {
                chain.nodes.Add(CreateNode(current));
                chain.sourceBlocks.Add(current);
                current = FindNextBlock(current, blocks, usedBlocks);
            }

            if (chain.nodes.Count > 0)
            {
                FoldParenthesisGroups(chain);
                chains.Add(chain);
            }
        }
    }

    private static void FoldParenthesisGroups(CodeChain chain)
    {
        for (int i = 0; i < chain.nodes.Count; i++)
        {
            if (chain.nodes[i].blockType != BlockType.PARENTHESIS_OPEN)
                continue;

            int closeIndex = -1;
            for (int j = i + 1; j < chain.nodes.Count; j++)
            {
                if (chain.nodes[j].blockType == BlockType.PARENTHESIS_CLOSE)
                {
                    closeIndex = j;
                    break;
                }
            }

            if (closeIndex <= i + 1)
                continue;

            List<BlockType> targets = new List<BlockType>();
            for (int j = i + 1; j < closeIndex; j++)
                targets.Add(chain.nodes[j].blockType);

            chain.nodes[i].blockType = BlockType.PARENTHESIS_BUNDLE;
            chain.nodes[i].groupTargets = targets;
            chain.nodes.RemoveRange(i + 1, closeIndex - i);
        }
    }

    private CodeBlock FindNextBlock(
        CodeBlock current,
        List<CodeBlock> blocks,
        HashSet<CodeBlock> usedBlocks
    )
    {
        int endX = current.GridPosition.x + current.GridWidth;

        return blocks.FirstOrDefault(block =>
            !usedBlocks.Contains(block)
            && block.GridPosition.y == current.GridPosition.y
            && block.GridPosition.x == endX
        );
    }

    private List<CodeBlock> GetBlocks()
    {
        return grid.GetBlocks()
            .Where(block => block.Definition != null)
            .OrderBy(block => block.GridPosition.y)
            .ThenBy(block => block.GridPosition.x)
            .ToList();
    }

    private CodeNode CreateNode(CodeBlock block)
    {
        return new CodeNode
        {
            blockType = block.CodeType,
            value = block.Value,
            sourceBlock = block,
        };
    }

    private void ValidateChains()
    {
        foreach (CodeChain chain in chains)
        {
            if (chain.nodes[0].blockType == BlockType.WEAP)
            {
                bool valid = CodeValidator.IsValidWeaponChain(chain.nodes);
                if (!valid)
                {
                    foreach (CodeBlock block in chain.sourceBlocks)
                        block.SetError(true);
                }
                continue;
            }

            ValidationResult result = validator.Validate(chain.sourceBlocks);
            if (!result.IsValid)
            {
                foreach (CodeBlock block in result.InvalidBlocks)
                    block.SetError(true);
            }
        }
    }

    public void CreateTestCode()
    {
        ClearAllBlocks();
        CreateBlock(testWEAP, new Vector2Int(0, 0));
        CreateBlock(testDMG, new Vector2Int(4, 0));
        RefreshCode();
    }

    private CodeBlock CreateBlock(
        BlockDefinition definition,
        Vector2Int position,
        int value = 0,
        BlockType? runtimeBlockType = null
    )
    {
        if (definition == null)
            return null;

        CodeBlock block = Instantiate(blockPrefab, grid.transform);
        block.Initialize(definition, grid, value, runtimeBlockType);
        block.SetGridPosition(position);
        grid.RegisterBlock(block);
        return block;
    }

    public CodeBlock CreateDropBlock(BlockDefinition definition, Vector2Int position, int value)
    {
        if (definition == null)
        {
            Debug.LogError("[CodeEditor] 블록 정의가 없어 드롭 블록을 만들지 못했습니다.", this);
            return null;
        }

        if (definition.blockType == BlockType.PARENTHESIS_BUNDLE)
        {
            if (!grid.CanPlace(position, 2))
            {
                Debug.LogWarning("[CodeEditor] 괄호 번들을 배치할 공간이 부족합니다.", this);
                return null;
            }

            CodeBlock open = CreateBlock(
                definition,
                position,
                0,
                BlockType.PARENTHESIS_OPEN
            );
            CreateBlock(
                definition,
                new Vector2Int(position.x + 1, position.y),
                0,
                BlockType.PARENTHESIS_CLOSE
            );
            RefreshCode();
            return open;
        }

        CodeBlock block = CreateBlock(definition, position, value);
        if (block == null)
        {
            Debug.LogError("[CodeEditor] 드롭 블록 생성에 실패했습니다.", this);
            return null;
        }

        RefreshCode();
        return block;
    }

    private void ClearAllBlocks()
    {
        foreach (CodeBlock block in grid.GetBlocks())
            Destroy(block.gameObject);

        grid.ClearBlocks();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

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

    [Header("Starting Code")]
    [SerializeField, FormerlySerializedAs("testWEAP")]
    private BlockDefinition startingWeaponCode;

    [SerializeField, FormerlySerializedAs("testDMG")]
    private BlockDefinition startingActionCode;
    [SerializeField] private BlockDefinition tutorialPlusCode;

    private readonly List<CodeChain> chains = new List<CodeChain>();
    private int validChainCount;

    public IReadOnlyList<CodeChain> Chains => chains;
    public bool IsReady { get; private set; }

    private void Awake()
    {
        IsReady = false;
    }

    private void Start()
    {
        CreateStartingCode();
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

    public string CheckInstalledCode(CodeBlock selectedBlock)
    {
        RefreshCode();
        if (chains.Count == 0)
            return "No code installed";

        string summary = $"Valid chains: {validChainCount} / {chains.Count}";
        if (selectedBlock == null)
            return summary;

        CodeChain chain = chains.Find(candidate => candidate.sourceBlocks.Contains(selectedBlock));
        WeaponDefinition weapon = chain?.FirstBlock?.Definition?.targetWeapon;
        if (weapon == null || !CodeChainEvaluator.TryGetWeaponActions(chain, out CodeNode actions))
            return summary;

        var values = new List<string>();
        BlockType? orbit = CodeChainEvaluator.GetOrbitModifier(chain);
        if (orbit.HasValue)
            values.Add(orbit == BlockType.SIN ? "SIN (figure eight)" : "COS (ellipse)");
        int actionCount = CodeChainEvaluator.GetWeaponActionCount(actions);
        for (int i = 0; i < actionCount; i++)
        {
            BlockType action = CodeChainEvaluator.GetWeaponAction(actions, i);
            float baseValue = CodeChainEvaluator.GetWeaponBaseValue(weapon, action);
            float result = CodeChainEvaluator.CalculateValue(baseValue, chain, 2, action);
            float preview = action == BlockType.DMG ? WeaponBase.ClampDamage(result) : Mathf.Max(0f, result);
            values.Add($"{action} {baseValue:0.##} > {preview:0.##}");
        }

        return $"{summary}\n{weapon.weaponName}: {string.Join(" / ", values)}";
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
        if (chain.nodes.Count < 4 || chain.nodes[0].blockType is not (BlockType.WEAP or BlockType.PL) ||
            chain.nodes[1].blockType != BlockType.PARENTHESIS_OPEN)
            return;

        int closeIndex = chain.nodes.FindIndex(2, node => node.blockType == BlockType.PARENTHESIS_CLOSE);
        if (closeIndex <= 2)
            return;

        // Only the group in the action slot represents multiple targets. Later groups are arithmetic.
        CodeNode actions = chain.nodes[1];
        actions.blockType = BlockType.PARENTHESIS_BUNDLE;
        for (int i = 2; i < closeIndex; i++)
            actions.groupTargets.Add(chain.nodes[i].blockType);
        chain.nodes.RemoveRange(2, closeIndex - 1);
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
        validChainCount = 0;
        WaveManager waveManager = WaveManager.Instance;
        List<string> errors = waveManager != null && waveManager.IsCodingLogUnlocked
            ? new List<string>() : null;

        foreach (CodeChain chain in chains)
        {
            ValidationResult result = ValidateChain(chain);
            if (result.IsValid)
            {
                validChainCount++;
                continue;
            }

            foreach (CodeBlock block in result.InvalidBlocks)
                block.SetError(true);

            if (errors != null)
            {
                Vector2Int start = chain.sourceBlocks[0].GridPosition;
                string sequence = string.Join(" → ", chain.sourceBlocks.Select(block => block.GetDisplayText()));
                errors.Add($"[문법 오류 · {start.y + 1}행 {start.x + 1}열] {sequence}\n{result.ErrorMessage}");
            }
        }

        if (errors != null)
            waveManager.UpdateCodingErrors(errors);
    }

    private ValidationResult ValidateChain(CodeChain chain)
    {
        switch (chain.nodes[0].blockType)
        {
            case BlockType.PL:
                return CodeValidator.IsValidPlayerStatChain(chain.nodes) ? ValidationResult.Success() :
                    new ValidationResult(false, chain.sourceBlocks, CodeValidator.PlayerStatSyntaxError);
            case BlockType.WEAP:
                return CodeValidator.IsValidWeaponChain(chain.nodes) ? ValidationResult.Success() :
                    new ValidationResult(false, chain.sourceBlocks, CodeValidator.WeaponSyntaxError);
            case BlockType.DTH:
                return CodeValidator.IsValidDeathChain(chain.nodes) ? ValidationResult.Success() :
                    new ValidationResult(false, chain.sourceBlocks, "DTH 뒤에는 POINT와 연산 블록을 연결하세요.");
            default:
                return validator.Validate(chain.sourceBlocks);
        }
    }

    private void CreateStartingCode()
    {
        ClearAllBlocks();
        if (tutorialPlusCode != null && grid.Width >= 9 && grid.Height >= 4)
        {
            CreateTutorialCode();
            RefreshCode();
            return;
        }

        CodeBlock weaponBlock = CreateBlock(startingWeaponCode, Vector2Int.zero);
        int actionColumn = weaponBlock != null ? weaponBlock.GridWidth : 0;
        CreateBlock(startingActionCode, new Vector2Int(actionColumn, 0));
        RefreshCode();
    }

    private void CreateTutorialCode()
    {
        int targetRow = grid.Height / 2;
        var rows = new List<int>();
        for (int row = 0; row < grid.Height; row++)
            if (row != targetRow)
                rows.Add(row);

        var blocks = new CodeBlock[3];
        BlockDefinition[] definitions = { startingWeaponCode, startingActionCode, tutorialPlusCode };
        for (int i = 0; i < blocks.Length; i++)
        {
            int rowIndex = UnityEngine.Random.Range(0, rows.Count);
            int column = UnityEngine.Random.Range(0, grid.Width - 2);
            blocks[i] = CreateBlock(definitions[i], new Vector2Int(column, rows[rowIndex]), i == 2 ? 10 : 0);
            rows.RemoveAt(rowIndex);
        }
        grid.BeginTutorial(blocks, new Vector2Int((grid.Width - 9) / 2, targetRow));
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
        foreach (Transform child in block.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = grid.gameObject.layer;
        block.Initialize(definition, grid, value, runtimeBlockType);
        block.SetGridPosition(position);
        grid.RegisterBlock(block);
        return block;
    }

    public CodeBlock CreateDropBlock(BlockDefinition definition, Vector2Int position, int value)
    {
        if (WaveManager.IsBattleActive)
        {
            Debug.LogWarning("[CodeEditor] 전투 중에는 코드를 수정할 수 없습니다.", this);
            return null;
        }

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
            CodeBlock close = CreateBlock(
                definition,
                new Vector2Int(position.x + 1, position.y),
                0,
                BlockType.PARENTHESIS_CLOSE
            );
            open.PairWith(close);
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

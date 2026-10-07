using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CodeValidator : MonoBehaviour
{
    [SerializeField]
    private CodeGrammarRules grammarRules;

    // =========================================================
    // Validate
    // =========================================================

    public ValidationResult Validate(List<CodeBlock> blocks)
    {
        if (blocks == null || blocks.Count == 0)
        {
            Debug.LogWarning("[CodeValidator] 블록이 없습니다.");

            return new ValidationResult(false, errorMessage: "코드가 없습니다.");
        }

        Debug.Log($"[CodeValidator] Validate 호출 - " + $"blocks.Count: {blocks.Count}");

        BlockCategory firstCategory = blocks[0].Definition.category;

        Debug.Log($"[CodeValidator] 첫 블록 카테고리: " + $"{firstCategory}");

        // =====================================================
        // WEAP
        // =====================================================

        if (firstCategory == BlockCategory.Weapon)
        {
            Debug.Log("[CodeValidator] WEAP 체인 감지");

            return ValidateWeaponChain(blocks);
        }

        // =====================================================
        // TOU
        // =====================================================

        if (firstCategory == BlockCategory.Trigger)
        {
            Debug.Log("[CodeValidator] TOU 체인 감지");

            return ValidatePlayerChain(blocks);
        }

        // =====================================================
        // 지원하지 않는 시작
        // =====================================================

        Debug.LogWarning($"[CodeValidator] 지원하지 않는 첫 블록: " + $"{firstCategory}");

        return new ValidationResult(
            false,
            new List<CodeBlock> { blocks[0] },
            "코드는 Trigger(TOU) 또는 Weapon(WEAP)으로 시작해야 합니다."
        );
    }

    // =========================================================
    // WEAP
    // =========================================================

    /// <summary>
    /// WEAP 문법
    ///
    /// WEAP → Action → Modifier...
    ///
    /// 예:
    ///
    /// WEAP → DMG → PLUS
    /// WEAP → DMG → PLUS → MULT
    /// WEAP → SPD → MULT
    /// WEAP → DST → PLUS
    /// WEAP → SZ → MULT
    /// </summary>
    private ValidationResult ValidateWeaponChain(List<CodeBlock> blocks)
    {
        List<CodeNode> nodes = blocks
            .Select(block => new CodeNode
            {
                blockType = block.CodeType,
            })
            .ToList();

        if (IsValidWeaponChain(nodes))
            return ValidationResult.Success();

        return new ValidationResult(
            false,
            new List<CodeBlock>(blocks),
            "무기 문법은 WEAP → 대상 → Modifier 또는 WEAP → (대상 대상...) → Modifier 순서여야 합니다."
        );
    }

    public static bool IsValidWeaponChain(IReadOnlyList<CodeNode> nodes)
    {
        if (nodes == null || nodes.Count < 2 || nodes[0] == null || nodes[1] == null)
            return false;

        if (nodes[0].blockType != BlockType.WEAP)
            return false;

        if (nodes[1].blockType == BlockType.PARENTHESIS_BUNDLE)
        {
            if (
                nodes[1].groupTargets == null
                || nodes[1].groupTargets.Count == 0
                || nodes[1].groupTargets.Any(target => !IsWeaponAction(target))
                || nodes[1].groupTargets.Distinct().Count() != nodes[1].groupTargets.Count
            )
            {
                return false;
            }

            for (int i = 2; i < nodes.Count; i++)
            {
                if (nodes[i] == null || !IsModifier(nodes[i].blockType))
                    return false;
            }

            return true;
        }

        if (!IsWeaponAction(nodes[1].blockType))
            return false;

        for (int i = 2; i < nodes.Count; i++)
        {
            if (nodes[i] == null || !IsModifier(nodes[i].blockType))
                return false;
        }

        return true;
    }

    // =========================================================
    // TOU
    // =========================================================

    /// <summary>
    /// TOU 문법
    ///
    /// TOU → EN → DMG → Modifier...
    ///
    /// 예:
    ///
    /// TOU → EN → DMG → PLUS
    /// TOU → EN → DMG → PLUS → MULT
    /// </summary>
    private ValidationResult ValidatePlayerChain(List<CodeBlock> blocks)
    {
        // -----------------------------------------------------
        // 최소 블록 수
        // -----------------------------------------------------

        if (blocks.Count < 4)
        {
            Debug.LogWarning($"[CodeValidator] TOU 체인 블록 부족: " + $"{blocks.Count}개");

            return new ValidationResult(
                false,
                errorMessage: "플레이어 코드는 최소 4개 블록 "
                    + "(TOU → EN → DMG → Modifier)이 필요합니다."
            );
        }

        // -----------------------------------------------------
        // 첫 번째 블록
        // -----------------------------------------------------

        if (blocks[0].CodeType != BlockType.TOU)
        {
            return ValidationResult.Error(
                blocks[0],
                blocks[1],
                "플레이어 체인은 TOU로 시작해야 합니다."
            );
        }

        // -----------------------------------------------------
        // 두 번째 블록
        // -----------------------------------------------------

        if (blocks[1].CodeType != BlockType.EN)
        {
            return ValidationResult.Error(
                blocks[0],
                blocks[1],
                "TOU 다음에는 EN 블록이 와야 합니다."
            );
        }

        // -----------------------------------------------------
        // 세 번째 블록
        // -----------------------------------------------------

        if (blocks[2].CodeType != BlockType.DMG)
        {
            return ValidationResult.Error(
                blocks[1],
                blocks[2],
                "EN 다음에는 DMG 블록이 와야 합니다."
            );
        }

        // -----------------------------------------------------
        // 네 번째 이후 = Modifier
        // -----------------------------------------------------

        for (int i = 3; i < blocks.Count; i++)
        {
            BlockType modifierType = blocks[i].CodeType;

            Debug.Log($"[CodeValidator] TOU Modifier 확인: " + $"{modifierType}");

            if (!IsModifier(modifierType))
            {
                Debug.LogWarning($"[CodeValidator] 잘못된 TOU Modifier: " + $"{modifierType}");

                CodeBlock previousBlock = blocks[i - 1];

                CodeBlock invalidBlock = blocks[i];

                return ValidationResult.Error(
                    previousBlock,
                    invalidBlock,
                    "DMG 다음에는 Modifier만 올 수 있습니다."
                );
            }
        }

        Debug.Log("[CodeValidator] TOU 체인 검증 성공");

        return ValidationResult.Success();
    }

    // =========================================================
    // Weapon Action
    // =========================================================

    private static bool IsWeaponAction(BlockType blockType)
    {
        switch (blockType)
        {
            case BlockType.DMG:
            case BlockType.SPD:
            case BlockType.DST:
            case BlockType.SZ:

                return true;

            default:

                return false;
        }
    }

    // =========================================================
    // Modifier
    // =========================================================

    private static bool IsModifier(BlockType blockType)
    {
        switch (blockType)
        {
            case BlockType.PLUS:
            case BlockType.MINUS:
            case BlockType.MULT:
            case BlockType.DIV:

                return true;

            default:

                return false;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

public static class CodeChainEvaluator
{
    public static bool IsOrbitModifier(BlockType type) => type is BlockType.SIN or BlockType.COS;

    public static BlockType? GetOrbitModifier(CodeChain chain)
    {
        BlockType? type = chain.nodes.Count > 2 ? chain.nodes[2]?.blockType : null;
        return type.HasValue && IsOrbitModifier(type.Value) ? type : null;
    }

    public static bool TryGetWeaponActions(CodeChain chain, out CodeNode actions)
    {
        actions = null;
        if (chain == null || !CodeValidator.IsValidWeaponChain(chain.nodes))
            return false;

        actions = chain.nodes[1];
        return true;
    }

    public static int GetWeaponActionCount(CodeNode actions)
    {
        return actions.blockType == BlockType.PARENTHESIS_BUNDLE
            ? actions.groupTargets.Count
            : 1;
    }

    public static BlockType GetWeaponAction(CodeNode actions, int index)
    {
        return actions.blockType == BlockType.PARENTHESIS_BUNDLE
            ? actions.groupTargets[index]
            : actions.blockType;
    }

    public static float GetWeaponBaseValue(WeaponDefinition definition, BlockType action)
    {
        if (definition == null)
            return 0f;

        float baseValue = action switch
        {
            BlockType.DMG => definition.damage,
            BlockType.SPD => definition.rotationSpeed,
            BlockType.DST => definition.distance,
            BlockType.SZ => definition.size,
            _ => 0f,
        };

        if (action == BlockType.DMG && PassiveEffectManager.Instance != null)
            baseValue += PassiveEffectManager.Instance.GetWeaponDamageBonus(definition);

        return baseValue;
    }

    public static float CalculateValue(float value, CodeChain chain, int startIndex, BlockType action = default)
    {
        if (chain == null || chain.nodes == null)
            return value;

        PassiveEffectManager passiveEffects = PassiveEffectManager.Instance;
        float additiveUnit = action is BlockType.DST or BlockType.SZ ? 0.1f : 1f;
        return EvaluateModifiers(value, chain.nodes, ref startIndex, additiveUnit, passiveEffects);
    }

    private static float EvaluateModifiers(float value, IReadOnlyList<CodeNode> nodes, ref int index,
        float additiveUnit, PassiveEffectManager passiveEffects)
    {
        float sum = 0f;
        float term = value;
        bool subtractTerm = false;
        while (index < nodes.Count)
        {
            CodeNode node = nodes[index++];
            if (node == null || IsOrbitModifier(node.blockType))
                continue;

            if (node.blockType == BlockType.PARENTHESIS_CLOSE)
                break;
            if (node.blockType == BlockType.PARENTHESIS_OPEN)
            {
                float accumulated = subtractTerm ? sum - term : sum + term;
                term = EvaluateModifiers(accumulated, nodes, ref index, additiveUnit, passiveEffects);
                sum = 0f;
                subtractTerm = false;
                continue;
            }

            float modifier = node.value;
            if (passiveEffects != null)
                modifier += passiveEffects.GetModifierBonus(node.blockType);

            switch (node.blockType)
            {
                case BlockType.PLUS:
                case BlockType.MINUS:
                    sum = subtractTerm ? sum - term : sum + term;
                    term = modifier * additiveUnit;
                    subtractTerm = node.blockType == BlockType.MINUS;
                    break;
                case BlockType.MULT:
                    term *= modifier;
                    break;
                case BlockType.DIV:
                    if (modifier != 0f)
                        term /= modifier;
                    break;
            }
        }

        return subtractTerm ? sum - term : sum + term;
    }
}

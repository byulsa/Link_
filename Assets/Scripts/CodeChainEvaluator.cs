using UnityEngine;

public static class CodeChainEvaluator
{
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

    public static float CalculateValue(float value, CodeChain chain, int startIndex)
    {
        if (chain == null || chain.nodes == null)
            return value;

        PassiveEffectManager passiveEffects = PassiveEffectManager.Instance;
        float sum = 0f;
        float term = value;
        bool subtractTerm = false;
        for (int i = startIndex; i < chain.nodes.Count; i++)
        {
            CodeNode node = chain.nodes[i];
            if (node == null)
                continue;

            float modifier = node.value;
            if (passiveEffects != null)
                modifier += passiveEffects.GetModifierBonus(node.blockType);

            switch (node.blockType)
            {
                case BlockType.PLUS:
                case BlockType.MINUS:
                    sum = subtractTerm ? sum - term : sum + term;
                    term = modifier;
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

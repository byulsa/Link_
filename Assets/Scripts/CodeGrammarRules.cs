using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "CodeEditor/Grammar Rules",
    fileName = "GrammarRules"
)]
public class CodeGrammarRules : ScriptableObject
{
    [System.Serializable]
    public struct Transition
    {
        public BlockCategory from;
        public BlockCategory to;
    }

    public List<Transition> allowedTransitions = new List<Transition>
{
    // Trigger → Target
    new Transition
    {
        from = BlockCategory.Trigger,
        to = BlockCategory.Target
    },

    // Trigger → Action
    new Transition
    {
        from = BlockCategory.Trigger,
        to = BlockCategory.Action
    },

    // Target → Action
    new Transition
    {
        from = BlockCategory.Target,
        to = BlockCategory.Action
    },

    // Action → Modifier
    new Transition
    {
        from = BlockCategory.Action,
        to = BlockCategory.Modifier
    },

    // Modifier → Modifier
    new Transition
    {
        from = BlockCategory.Modifier,
        to = BlockCategory.Modifier
    },

    // Weapon → Action
    new Transition
    {
        from = BlockCategory.Weapon,
        to = BlockCategory.Action
    }
};

    public bool CanConnect(
        BlockCategory from,
        BlockCategory to)
    {
        foreach (Transition transition in allowedTransitions)
        {
            if (transition.from == from &&
                transition.to == to)
            {
                return true;
            }
        }

        return false;
    }
}
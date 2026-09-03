using System.Collections.Generic;

[System.Serializable]
public class CodeNode
{
    public BlockType blockType;
    public float value;

    [System.NonSerialized]
    public CodeBlock sourceBlock;
}

[System.Serializable]
public class CodeChain
{
    public List<CodeNode> nodes = new List<CodeNode>();

    public CodeBlock FirstBlock
    {
        get
        {
            if (nodes.Count == 0)
                return null;

            return nodes[0].sourceBlock;
        }
    }

    public CodeBlock LastBlock
    {
        get
        {
            if (nodes.Count == 0)
                return null;

            return nodes[nodes.Count - 1].sourceBlock;
        }
    }
}
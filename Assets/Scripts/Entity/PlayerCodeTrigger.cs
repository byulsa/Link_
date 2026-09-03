using UnityEngine;

public class PlayerCodeTrigger : MonoBehaviour
{
    [SerializeField] private CodeExecutor executor;
    [SerializeField] private CodeEditor editor;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Entity target = other.GetComponent<Entity>();

        if (target == null)
            return;

        Entity source = GetComponent<Entity>();

        if (source == null)
            return;

        if (!target.Is(EntityType.Enemy))
            return;

        foreach (CodeChain chain in editor.Chains)
        {
            executor.Execute(
                chain,
                source,
                target
            );
        }
    }
}
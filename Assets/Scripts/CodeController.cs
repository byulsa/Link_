using System.Collections.Generic;
using UnityEngine;

public class CodeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private CodeEditor editor;

    [Header("Owner")]
    [SerializeField]
    private Entity owner;

    public Entity Owner => owner;

    private void Awake()
    {
        if (owner == null)
        {
            owner =
                GetComponent<Entity>();
        }

        if (editor == null)
        {
            editor =
                GetComponent<CodeEditor>();
        }

        Debug.Log(
            $"[CodeController] " +
            $"Owner = {owner}"
        );

        Debug.Log(
            $"[CodeController] " +
            $"Editor = {editor}"
        );
    }

    // WEAP 트리거 체인만 실행 (무기 충돌 시 호출)
    public bool ExecuteWeapon(
        Entity target = null)
    {
        return ExecuteByTrigger(
            BlockType.WEAP,
            target
        );
    }

    // TOU 트리거 체인만 실행 (플레이어 몸 충돌 시 호출)
    public bool ExecuteTouch(
        Entity target = null)
    {
        return ExecuteByTrigger(
            BlockType.TOU,
            target
        );
    }

    // 주어진 트리거 타입의 체인만 골라서 실행
    // 다른 트리거 체인은 아예 건드리지 않음
    private bool ExecuteByTrigger(
        BlockType triggerType,
        Entity target)
    {
        if (editor == null)
        {
            Debug.LogError(
                "[CodeController] " +
                "CodeEditor가 없습니다."
            );

            return false;
        }

        if (owner == null)
        {
            Debug.LogError(
                "[CodeController] " +
                "Owner가 없습니다."
            );

            return false;
        }

        if (!editor.IsReady)
        {
            Debug.LogWarning(
                "[CodeController] " +
                "CodeEditor가 아직 준비되지 않았습니다."
            );

            return false;
        }

        IReadOnlyList<CodeChain> chains =
            editor.Chains;

        Debug.Log(
            $"[CodeController] " +
            $"{owner.name}의 Chain Count = " +
            $"{chains.Count}, " +
            $"Trigger = {triggerType}"
        );

        bool dealtDamage = false;

        foreach (CodeChain chain in chains)
        {
            if (chain == null ||
                chain.nodes == null ||
                chain.nodes.Count == 0)
            {
                continue;
            }

            // 이 트리거에 해당하지 않는 체인은 스킵
            // (예: WEAP 충돌인데 체인이 TOU/DTH로 시작하면 무시)
            if (chain.nodes[0].blockType != triggerType)
            {
                continue;
            }

            Debug.Log(
                $"[CodeController] " +
                $"{owner.name} 실행: " +
                $"{chain.nodes[0].blockType}"
            );

            bool result =
                CodeExecutor.Instance.Execute(
                    chain,
                    owner,
                    target
                );

            if (result)
            {
                dealtDamage = true;
            }
        }

        return dealtDamage;
    }

    public void ExecuteDeath(Entity target)
    {
        IReadOnlyList<CodeChain> chains = editor.Chains;

        foreach (CodeChain chain in chains)
        {
            if (chain == null || chain.nodes == null || chain.nodes.Count == 0)
                continue;

            if (chain.nodes[0].blockType != BlockType.DTH)
                continue;

            CodeExecutor.Instance.Execute(chain, owner, target);

            return;
        }

        // DTH 코드가 없으면 기본 Point 지급
        Enemy enemy = target.GetComponent<Enemy>();

        if (enemy != null)
        {
            PointManager.Instance.AddPoint(
                enemy.GetPointAmount()
            );
        }
    }
}
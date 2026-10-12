using UnityEngine;

public static class CodeExecutor
{
    // 코드가 직접 데미지를 처리했으면 true
    // 그렇지 않으면 false
    public static bool Execute(CodeChain chain, Entity source, Entity target)
    {
        if (
            chain == null
            || chain.nodes == null
            || chain.nodes.Count == 0
            || chain.nodes[0] == null
            || source == null
        )
        {
            return false;
        }

        BlockType firstType = chain.nodes[0].blockType;

        switch (firstType)
        {
            case BlockType.TOU:
                return ExecuteTouch(chain, source, target);

            case BlockType.WEAP:
                return ExecuteWeapon(chain, source, target);

            case BlockType.DTH:
                return ExecuteDeath(chain, target);

            default:
                Debug.LogWarning($"[CodeExecutor] " + $"지원하지 않는 코드: {firstType}");

                return false;
        }
    }

    private static bool ExecuteTouch(CodeChain chain, Entity source, Entity target)
    {
        if (target == null || !target.Is(EntityType.Enemy))
        {
            return false;
        }

        Enemy enemy = target.GetComponent<Enemy>();

        if (enemy == null)
            return false;

        if (chain.nodes.Count < 4)
            return false;

        if (chain.nodes[1].blockType != BlockType.EN || chain.nodes[2].blockType != BlockType.DMG)
        {
            return false;
        }

        float value = enemy.BaseDamage;

        value = CodeChainEvaluator.CalculateValue(value, chain, 3);

        value = Mathf.Max(0f, value);

        enemy.TakeDamage(value);

        return true;
    }

    private static bool ExecuteWeapon(CodeChain chain, Entity source, Entity target)
    {
        if (!CodeChainEvaluator.TryGetWeaponActions(chain, out CodeNode actions))
            return false;

        if (
            source == null
            || !source.Is(EntityType.Weapon)
            || target == null
            || !target.Is(EntityType.Enemy)
        )
            return false;

        WeaponBase weapon = source.GetComponent<WeaponBase>();
        if (weapon == null)
            return false;

        WeaponDefinition scope = chain.FirstBlock?.Definition?.targetWeapon;
        if (scope != null && scope != weapon.Definition)
            return false;

        Enemy enemy = target.GetComponent<Enemy>();
        if (enemy == null)
            return false;

        bool dealtDamage = false;
        int actionCount = CodeChainEvaluator.GetWeaponActionCount(actions);
        for (int i = 0; i < actionCount; i++)
        {
            BlockType action = CodeChainEvaluator.GetWeaponAction(actions, i);
            float baseValue = CodeChainEvaluator.GetWeaponBaseValue(weapon.Definition, action);
            float finalValue = Mathf.Max(
                0f,
                CodeChainEvaluator.CalculateValue(baseValue, chain, 2)
            );

            if (action == BlockType.DMG)
            {
                enemy.TakeDamage(finalValue);
                dealtDamage = true;
            }
            else
            {
                weapon.SetStat(action, finalValue);
            }
        }

        return dealtDamage;
    }

    // ⭐ DTH → POINT 처리 (중복 지급 방지)
    private static bool ExecuteDeath(CodeChain chain, Entity target)
    {
        if (target == null || !target.Is(EntityType.Enemy))
            return false;

        Enemy enemy = target.GetComponent<Enemy>();
        if (enemy == null)
        {
            Debug.LogError("[CodeExecutor] Enemy 컴포넌트가 없습니다.", target);
            return false;
        }

        PointManager pointManager = PointManager.Instance;
        if (pointManager == null)
        {
            Debug.LogError("[CodeExecutor] PointManager 인스턴스를 찾을 수 없습니다.");
            return false;
        }

        float finalPoint = enemy.GetPointAmount();
        if (chain.nodes.Count > 1 && chain.nodes[1] != null
            && chain.nodes[1].blockType == BlockType.POINT)
        {
            finalPoint = CodeChainEvaluator.CalculateValue(finalPoint, chain, 2);
        }

        pointManager.AddPoint(Mathf.Max(0, (int)finalPoint));
        return true;
    }
}

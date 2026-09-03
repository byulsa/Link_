using UnityEngine;

public class CodeExecutor : MonoBehaviour
{
    public static CodeExecutor Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // 코드가 직접 데미지를 처리했으면 true
    // 그렇지 않으면 false
    public bool Execute(
        CodeChain chain,
        Entity source,
        Entity target)
    {
        if (chain == null ||
            chain.nodes == null ||
            chain.nodes.Count == 0 ||
            source == null)
        {
            return false;
        }

        BlockType firstType =
            chain.nodes[0].blockType;

        switch (firstType)
        {
            case BlockType.TOU:
                return ExecuteTouch(
                    chain,
                    source,
                    target
                );

            case BlockType.WEAP:
                return ExecuteWeapon(
                    chain,
                    source,
                    target
                );

            case BlockType.DTH:
                return ExecuteDeath(
                    chain,
                    source,
                    target
                );

            default:
                Debug.LogWarning(
                    $"[CodeExecutor] " +
                    $"지원하지 않는 코드: {firstType}"
                );

                return false;
        }
    }

    private bool ExecuteTouch(
        CodeChain chain,
        Entity source,
        Entity target)
    {
        if (target == null ||
            !target.Is(EntityType.Enemy))
        {
            return false;
        }

        Enemy enemy =
            target.GetComponent<Enemy>();

        if (enemy == null)
            return false;

        if (chain.nodes.Count < 4)
            return false;

        if (chain.nodes[1].blockType != BlockType.EN ||
            chain.nodes[2].blockType != BlockType.DMG)
        {
            return false;
        }

        float value = enemy.BaseDamage;

        value = CalculateValue(
            value,
            chain,
            3
        );

        value = Mathf.Max(0f, value);

        enemy.TakeDamage(value);

        return true;
    }

    private bool ExecuteWeapon(
        CodeChain chain,
        Entity source,
        Entity target)
    {
        if (source == null ||
            !source.Is(EntityType.Weapon) ||
            target == null ||
            !target.Is(EntityType.Enemy))
        {
            return false;
        }

        WeaponBase weapon =
            source.GetComponent<WeaponBase>();

        if (weapon == null)
            return false;

        Enemy enemy =
            target.GetComponent<Enemy>();

        if (enemy == null)
            return false;

        if (chain.nodes.Count < 2)
            return false;

        BlockType action =
            chain.nodes[1].blockType;

        if (!IsWeaponAction(action))
        {
            return false;
        }

        float baseValue =
            GetWeaponBaseValue(
                weapon,
                action
            );

        float finalValue =
            CalculateValue(
                baseValue,
                chain,
                2
            );

        finalValue =
            Mathf.Max(0f, finalValue);

        switch (action)
        {
            case BlockType.DMG:

                enemy.TakeDamage(
                    finalValue
                );

                // 이번 코드가 데미지를 처리했음
                return true;

            case BlockType.DST:
            case BlockType.SPD:
            case BlockType.SZ:

                weapon.SetStat(
                    action,
                    finalValue
                );

                // 스탯만 변경했으므로
                // 기본 공격은 따로 필요함
                return false;

            default:
                return false;
        }
    }

    // ⭐ DTH → POINT 처리 (중복 지급 방지)
    private bool ExecuteDeath(
        CodeChain chain,
        Entity source,
        Entity target)
    {
        Debug.Log("[CodeExecutor] ===== ExecuteDeath 시작 =====");

        if (target == null || !target.Is(EntityType.Enemy))
        {
            Debug.Log("[CodeExecutor] Target이 Enemy가 아님");
            return false;
        }

        Debug.Log("[CodeExecutor] Target이 Enemy임");

        Enemy enemy = target.GetComponent<Enemy>();

        if (enemy == null)
        {
            Debug.LogError("[CodeExecutor] Enemy 컴포넌트 없음!");
            return false;
        }

        Debug.Log("[CodeExecutor] Enemy 컴포넌트 획득");

        // ⭐ 기본값 획득
        float basePoint = 0f;

        try
        {
            basePoint = enemy.GetPointAmount();
            Debug.Log($"[CodeExecutor] 기본 Point 획득: {basePoint}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[CodeExecutor] GetPointAmount() 중 예외!!!");
            Debug.LogError($"[CodeExecutor] 예외: {ex.Message}");
            Debug.LogError($"[CodeExecutor] 스택:\n{ex.StackTrace}");
            return false;
        }

        // DTH → POINT 체인인지 확인
        if (chain.nodes.Count < 2 || chain.nodes[1].blockType != BlockType.POINT)
        {
            // 코드 없음 → 기본값만 지급
            Debug.Log(
                $"[CodeExecutor] POINT 블록 없음, 기본값만 지급"
            );

            try
            {
                PointManager.Instance.AddPoint((int)basePoint);
                Debug.Log($"[CodeExecutor] AddPoint({(int)basePoint}) 완료");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[CodeExecutor] AddPoint() 중 예외!!!");
                Debug.LogError($"[CodeExecutor] 예외: {ex.Message}");
                Debug.LogError($"[CodeExecutor] 스택:\n{ex.StackTrace}");
                return false;
            }

            Debug.Log("[CodeExecutor] ===== ExecuteDeath 종료 (기본값) =====");
            return true;
        }

        // ⭐ DTH → POINT 코드 있음 → 수정자 적용
        Debug.Log(
            $"[CodeExecutor] DTH → POINT 체인 발견"
        );

        float finalPoint = 0f;

        try
        {
            finalPoint = CalculateValue(
                basePoint,
                chain,
                2  // POINT 블록 다음부터 수정자 시작
            );

            finalPoint = Mathf.Max(0f, finalPoint);

            Debug.Log(
                $"[CodeExecutor] 수정자 계산 완료: {basePoint} → {finalPoint}"
            );
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[CodeExecutor] CalculateValue() 중 예외!!!");
            Debug.LogError($"[CodeExecutor] 예외: {ex.Message}");
            Debug.LogError($"[CodeExecutor] 스택:\n{ex.StackTrace}");
            return false;
        }

        // 한 번만 지급 (중복 방지)
        try
        {
            PointManager.Instance.AddPoint((int)finalPoint);
            Debug.Log($"[CodeExecutor] AddPoint({(int)finalPoint}) 완료");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[CodeExecutor] AddPoint() 중 예외!!!");
            Debug.LogError($"[CodeExecutor] 예외: {ex.Message}");
            Debug.LogError($"[CodeExecutor] 스택:\n{ex.StackTrace}");
            return false;
        }

        Debug.Log("[CodeExecutor] ===== ExecuteDeath 종료 (수정자 적용) =====");
        return true;
    }

    private float CalculateValue(
        float value,
        CodeChain chain,
        int startIndex)
    {
        for (
            int i = startIndex;
            i < chain.nodes.Count;
            i++)
        {
            CodeNode node =
                chain.nodes[i];

            if (node == null)
                continue;

            value = ApplyModifier(
                value,
                node.blockType,
                node.value
            );
        }

        return value;
    }

    private float ApplyModifier(
        float value,
        BlockType type,
        float modifier)
    {
        switch (type)
        {
            case BlockType.PLUS:
                return value + modifier;

            case BlockType.MINUS:
                return value - modifier;

            case BlockType.MULT:
                return value * modifier;

            case BlockType.DIV:
                return modifier == 0f
                    ? value
                    : value / modifier;

            default:
                return value;
        }
    }

    private float GetWeaponBaseValue(
        WeaponBase weapon,
        BlockType action)
    {
        WeaponDefinition def =
            weapon.Definition;

        if (def == null)
            return 0f;

        switch (action)
        {
            case BlockType.DMG:
                return def.damage;

            case BlockType.SPD:
                return def.rotationSpeed;

            case BlockType.DST:
                return def.distance;

            case BlockType.SZ:
                return def.size;

            default:
                return 0f;
        }
    }

    private bool IsWeaponAction(
        BlockType type)
    {
        switch (type)
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
}
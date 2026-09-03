using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Entity entity;

    [SerializeField]
    private CodeController codeController;

    private void Awake()
    {
        if (entity == null)
        {
            entity = GetComponent<Entity>();
        }

        if (codeController == null)
        {
            codeController = GetComponent<CodeController>();
        }
    }

    private void OnEnable()
    {
        Debug.Log("[Player] OnEnemyDeath 구독");
        Enemy.OnEnemyDeath += HandleEnemyDeath;
    }

    private void OnDisable()
    {
        Enemy.OnEnemyDeath -= HandleEnemyDeath;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Entity target =
            other.GetComponent<Entity>();

        if (target == null)
            return;

        if (!target.Is(EntityType.Enemy))
            return;

        Debug.Log(
            $"[Player] Enemy 충돌 감지: {target.name}"
        );

        codeController.ExecuteTouch(target);
    }

    private void HandleEnemyDeath(Enemy enemy)
    {
        Debug.Log("[Player] ===== HandleEnemyDeath 시작 =====");

        if (enemy == null)
        {
            Debug.LogError("[Player] enemy == null!");
            return;
        }

        Debug.Log($"[Player] enemy: {enemy.name}");

        Entity target = enemy.GetComponent<Entity>();

        if (target == null)
        {
            Debug.LogError("[Player] target == null!");
            return;
        }

        Debug.Log($"[Player] target: {target.name}");
        Debug.Log($"[Player] Enemy 사망 감지: {enemy.name}");

        try
        {
            Debug.Log("[Player] ExecuteDeath() 호출 전");
            codeController.ExecuteDeath(target);
            Debug.Log("[Player] ExecuteDeath() 호출 완료");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[Player] ExecuteDeath() 중 예외 발생!!!");
            Debug.LogError($"[Player] 예외 메시지: {ex.Message}");
            Debug.LogError($"[Player] 스택 트레이스:\n{ex.StackTrace}");
        }

        Debug.Log("[Player] ===== HandleEnemyDeath 종료 =====");
    }
}
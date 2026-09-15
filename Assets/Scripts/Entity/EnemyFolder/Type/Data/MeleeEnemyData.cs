using UnityEngine;

[CreateAssetMenu(
    fileName = "MeleeEnemyData_",
    menuName = "Scriptable Objects/Enemy/Melee"
)]
public class MeleeEnemyData : EnemyData
{
    [Header("Melee")]
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;
}
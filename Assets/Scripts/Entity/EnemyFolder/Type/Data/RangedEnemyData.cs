using UnityEngine;

public enum RangedBehavior
{
    ApproachAndShoot,
    MaintainDistance
}

public enum RangedAttackType
{
    Projectile,
    Laser,
    Flame
}
[CreateAssetMenu(
    fileName = "RangedEnemyData_",
    menuName = "Scriptable Objects/Enemy/Ranged"
)]
public class RangedEnemyData : EnemyData
{
    [Header("Ranged")]
    public float attackRange = 7f;
    public float retreatRange = 3f;
    public float retreatEndRange = 5f;
    public float attackCooldown = 1f;

    [Header("Behavior")]
    public RangedBehavior behavior;

    [Header("Attack")]
    public RangedAttackType attackType;

    [Header("Projectile")]
    public GameObject projectilePrefab;

    [Header("Projectile Pattern")]
    public int projectileCount = 1;
    public float[] projectileAngles;
}
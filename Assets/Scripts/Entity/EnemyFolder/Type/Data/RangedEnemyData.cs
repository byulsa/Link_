using UnityEngine;

public enum RangedBehavior
{
    ApproachAndShoot,
    MaintainDistance,
}

public enum RangedAttackType
{
    Projectile,
    Laser,
    Flame,
}

[CreateAssetMenu(fileName = "RangedEnemyData_", menuName = "Scriptable Objects/Enemy/Ranged")]
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

    [Header("Laser")]
    [Min(0.01f)] public float laserMaxDistance = 10f;
    [Min(0f)] public float laserDamageMultiplier = 1f;
    [Min(0.01f)] public float laserWidth = 0.12f;
    [Min(0.01f)] public float laserDuration = 0.15f;
    public Color laserColor = Color.red;
}

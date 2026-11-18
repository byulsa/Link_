using UnityEngine;

public enum SelfDestructCondition { OnDeath, PlayerNearby, OnDeathOrPlayerNearby }

[CreateAssetMenu(fileName = "SuicideEnemyData_", menuName = "Scriptable Objects/Enemy/Suicide")]
public class SuicideEnemyData : EnemyData
{
    public SelfDestructCondition condition = SelfDestructCondition.OnDeathOrPlayerNearby;
    [Min(0f)] public float triggerRange = 1.5f;
    [Min(1f)] public float deathChaseSpeedMultiplier = 1.75f;

    [Header("Explosion")]
    public float explosionRange = 2.5f;
    public float explosionDamage = 30f;
}

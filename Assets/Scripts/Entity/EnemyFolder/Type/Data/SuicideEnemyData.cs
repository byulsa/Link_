using UnityEngine;

[CreateAssetMenu(
    fileName = "SuicideEnemyData_",
    menuName = "Scriptable Objects/Enemy/Suicide"
)]
public class SuicideEnemyData : EnemyData
{
    [Header("Explosion")]
    public float explosionRange = 2.5f;
    public float explosionDamage = 30f;
}

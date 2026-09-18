using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemyData_",
    menuName = "Scriptable Objects/EnemyData"
)]
public class EnemyData : ScriptableObject
{
    [Header("Base Info")]
    public string enemyName = "Dummy";

    [Header("Stats")]
    public float maxHealth = 20f;
    public float baseDamage = 5f;
    public float moveSpeed = 3f;
    public float knockbackForce = 5f;
    public float knockbackDuration = 0.2f;

    [Header("Attack")]
    public float attackRecoveryTime = 0.5f;

    [Header("Prefab")]
    public GameObject enemyPrefab;

    [Header("PointDrop")]
    public int PointDropNum;
}
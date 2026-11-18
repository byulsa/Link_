using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WaveTimeLineData
{
    public float startTime;
    public float endTime;
    public List<EnemyData> enemyData;
    [Min(0.5f)] public float spawnInterval = 2f;
    public bool overrideSelfDestructCondition;
    public SelfDestructCondition selfDestructCondition = SelfDestructCondition.OnDeathOrPlayerNearby;
}

[CreateAssetMenu(fileName = "Wave_", menuName = "Wave/Wave", order = 1)]
public class WaveData : ScriptableObject
{
    public float maxTime = 90f;
    public List<WaveTimeLineData> timeLines;

    [Header("Wave Balance")]
    [Min(0)] public int enemyPoolLimitOverride;
    [Min(0)] public int bonusPointsPerKill;

    [Header("Boss Wave")]
    public EnemyData boss;
    public AudioClip bossMusic;

    [Header("Intermission Dialogue")]
    public WaveDialogue dialogueAfterWave;
    public WaveDialogue dialogueAfterCodeCollection;
    public WaveDialogue dialogueAfterShop;
    public WaveDialogue dialogueAfterCodeEditor;
}

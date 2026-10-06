using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WaveTimeLineData
{
    public float startTime;
    public float endTime;
    public List<EnemyData> enemyData;
    [Min(0.5f)] public float spawnInterval = 2f;
}

[CreateAssetMenu(fileName = "Wave_", menuName = "Wave/Wave", order = 1)]
public class WaveData : ScriptableObject
{
    public float maxTime = 90f;
    public List<WaveTimeLineData> timeLines;
}

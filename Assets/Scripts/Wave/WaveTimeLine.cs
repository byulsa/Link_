using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class WaveTimeLineData
{
    public float startTime;
    public float endTime;

    public List<EnemyData> enemyData;
}

[CreateAssetMenu(fileName = "WaveTimeLine", menuName = "Wave/WaveTimeLine", order = 1)]
public class WaveTimeLine : ScriptableObject
{
    public float maxTime = 300f;

    public List<WaveTimeLineData> waveData;
}
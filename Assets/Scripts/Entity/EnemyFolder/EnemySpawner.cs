using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner")]
    [SerializeField] private WaveData currentWave;
    [SerializeField] private int poolLimit = 10;

    private readonly List<PooledEnemy> poolList = new List<PooledEnemy>();
    private float elapsedTime;
    private float spawnTimer;
    private float spawnInterval = 2f;
    private bool waveEnded;

    private class PooledEnemy
    {
        public GameObject Prefab { get; }
        public GameObject Instance { get; }

        public PooledEnemy(GameObject prefab, GameObject instance)
        {
            Prefab = prefab;
            Instance = instance;
        }
    }

    public void StartWave(WaveData wave)
    {
        currentWave = wave;
        elapsedTime = 0f;
        spawnTimer = 0f;
        waveEnded = false;

        SetNextSpawnInterval();
    }

    private void Update()
    {
        if (currentWave == null || waveEnded)
            return;

        elapsedTime += Time.deltaTime;
        spawnTimer += Time.deltaTime;

        if (elapsedTime >= currentWave.maxTime)
        {
            EndWave();
            return;
        }

        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            Spawn();
            SetNextSpawnInterval();
        }
    }

    private WaveTimeLineData GetCurrentTimeline()
    {
        if (currentWave.timeLines == null)
            return null;

        foreach (WaveTimeLineData data in currentWave.timeLines)
        {
            if (elapsedTime >= data.startTime && elapsedTime < data.endTime)
                return data;
        }

        return null;
    }

    private void SetNextSpawnInterval()
    {
        WaveTimeLineData timeline = GetCurrentTimeline();

        if (timeline == null)
        {
            spawnInterval = 2f;
            return;
        }

        spawnInterval = Random.Range(
            timeline.spawnInterval * 0.5f,
            timeline.spawnInterval * 2f
        );

        spawnInterval = Mathf.Clamp(spawnInterval, 0.5f, 10f);
    }

    private void Spawn()
    {
        WaveTimeLineData timeline = GetCurrentTimeline();

        if (timeline == null)
            return;

        if (timeline.enemyData == null || timeline.enemyData.Count == 0)
            return;

        EnemyData enemyData = timeline.enemyData[
            Random.Range(0, timeline.enemyData.Count)
        ];

        if (enemyData == null || enemyData.enemyPrefab == null)
            return;

        GameObject enemyObj = GetObjectFromPool(enemyData);

        if (enemyObj == null)
            return;

        enemyObj.transform.position = transform.position;
        enemyObj.transform.rotation = transform.rotation;
        enemyObj.SetActive(true);
    }

    private GameObject GetObjectFromPool(EnemyData enemyData)
    {
        for (int i = 0; i < poolList.Count; i++)
        {
            PooledEnemy pooledEnemy = poolList[i];

            if (pooledEnemy.Prefab == enemyData.enemyPrefab && !pooledEnemy.Instance.activeSelf)
            {
                Enemy pooledEnemyComp = pooledEnemy.Instance.GetComponent<Enemy>();

                if (pooledEnemyComp != null)
                    pooledEnemyComp.InitializeFromData(enemyData);

                return pooledEnemy.Instance;
            }
        }

        if (poolList.Count >= poolLimit)
            return null;

        GameObject newObj = Instantiate(
            enemyData.enemyPrefab,
            transform.position,
            transform.rotation
        );

        Enemy enemyComp = newObj.GetComponent<Enemy>();

        if (enemyComp != null)
            enemyComp.InitializeFromData(enemyData);

        poolList.Add(new PooledEnemy(enemyData.enemyPrefab, newObj));
        return newObj;
    }

    private void EndWave()
    {
        waveEnded = true;
        currentWave = null;

        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveEnded();
    }
}
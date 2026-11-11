using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner")]
    [SerializeField] private WaveData currentWave;
    [SerializeField] private int poolLimit = 10;

    [Header("Off-screen Spawn")]
    [SerializeField] private Camera spawnCamera;
    [SerializeField, Min(0.1f)] private float screenPadding = 1f;
    [SerializeField, Min(0f)] private float extraSpawnDistance = 2f;
    private readonly Vector3[] cameraCorners = new Vector3[4];
    private bool missingCameraWarningShown;

    private readonly List<PooledEnemy> poolList = new List<PooledEnemy>();
    private float statMultiplier = 1f;
    private float elapsedTime;
    private float spawnTimer;
    private float spawnInterval = 2f;
    private bool waveEnded;
    private bool spawningFinished;

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

    public void StartWave(WaveData wave, float enemyStatMultiplier = 1f)
    {
        currentWave = wave;
        statMultiplier = Mathf.Max(1f, enemyStatMultiplier);
        elapsedTime = 0f;
        spawnTimer = 0f;
        waveEnded = false;
        spawningFinished = false;

        SetNextSpawnInterval();
    }

    private void Update()
    {
        if (currentWave == null || waveEnded)
            return;

        elapsedTime += Time.deltaTime;
        spawnTimer += Time.deltaTime;

        if (elapsedTime >= currentWave.maxTime)
            spawningFinished = true;

        if (spawningFinished)
        {
            if (!HasActiveEnemies())
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

    private bool HasActiveEnemies()
    {
        foreach (PooledEnemy pooledEnemy in poolList)
        {
            if (pooledEnemy.Instance != null && pooledEnemy.Instance.activeInHierarchy)
                return true;
        }

        return false;
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

        if (!TryGetSpawnPosition(enemyData.enemyPrefab, out Vector3 spawnPosition)) return;
        GameObject enemyObj = GetObjectFromPool(enemyData, spawnPosition);

        if (enemyObj == null)
            return;

        if (enemyObj.TryGetComponent<SuicideEnemy>(out var suicide))
            suicide.SetConditionOverride(timeline.overrideSelfDestructCondition ? timeline.selfDestructCondition : (SelfDestructCondition?)null);
        enemyObj.transform.position = spawnPosition;
        enemyObj.transform.rotation = transform.rotation;
        enemyObj.SetActive(true);
    }

    private GameObject GetObjectFromPool(EnemyData enemyData, Vector3 spawnPosition)
    {
        poolList.RemoveAll(entry => entry.Instance == null);
        for (int i = 0; i < poolList.Count; i++)
        {
            PooledEnemy pooledEnemy = poolList[i];

            if (pooledEnemy.Prefab == enemyData.enemyPrefab && !pooledEnemy.Instance.activeSelf)
            {
                Enemy pooledEnemyComp = pooledEnemy.Instance.GetComponent<Enemy>();

                if (pooledEnemyComp != null)
                    pooledEnemyComp.InitializeForSpawn(enemyData, statMultiplier);

                return pooledEnemy.Instance;
            }
        }

        if (poolList.Count >= poolLimit)
        {
            // Inactive enemies of an earlier wave must not block new enemy types.
            int unused = poolList.FindIndex(entry => !entry.Instance.activeSelf);
            if (unused < 0) return null;
            Destroy(poolList[unused].Instance);
            poolList.RemoveAt(unused);
        }

        GameObject newObj = Instantiate(
            enemyData.enemyPrefab,
            spawnPosition,
            transform.rotation
        );

        Enemy enemyComp = newObj.GetComponent<Enemy>();

        if (enemyComp != null)
            enemyComp.InitializeForSpawn(enemyData, statMultiplier);

        poolList.Add(new PooledEnemy(enemyData.enemyPrefab, newObj));
        return newObj;
    }

    private bool TryGetSpawnPosition(GameObject prefab, out Vector3 position)
    {
        position = default;
        Camera camera = spawnCamera != null && spawnCamera.isActiveAndEnabled ? spawnCamera : Camera.main;
        if (camera == null)
        {
            if (!missingCameraWarningShown)
                Debug.LogWarning("[EnemySpawner] No active spawn camera. Skipping spawn.", this);
            missingCameraWarningShown = true;
            return false;
        }
        missingCameraWarningShown = false;
        Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, transform.position.z));
        Vector2[] viewportCorners = { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
        Vector3 center = Vector3.zero;
        float perimeter = 0f;
        // Recalculate at every spawn so camera motion, rotation, zoom and aspect changes apply immediately.
        for (int i = 0; i < 4; i++)
        {
            Ray ray = camera.ViewportPointToRay(viewportCorners[i]);
            if (!plane.Raycast(ray, out float distance)) return false;
            cameraCorners[i] = ray.GetPoint(distance);
            center += cameraCorners[i] * 0.25f;
        }
        for (int i = 0; i < 4; i++)
            perimeter += Vector3.Distance(cameraCorners[i], cameraCorners[(i + 1) % 4]);
        if (perimeter <= 0f) return false;
        float roll = Random.Range(0f, perimeter);
        for (int i = 0; i < 4; i++)
        {
            Vector3 start = cameraCorners[i];
            Vector3 edge = cameraCorners[(i + 1) % 4] - start;
            float length = edge.magnitude;
            if (roll > length && i < 3) { roll -= length; continue; }
            Vector3 outward = new Vector3(edge.y, -edge.x, 0f).normalized;
            if (Vector3.Dot(outward, start + edge * 0.5f - center) < 0f) outward = -outward;
            float radius = 0f;
            foreach (SpriteRenderer renderer in prefab.GetComponentsInChildren<SpriteRenderer>())
                radius = Mathf.Max(radius, Vector3.Distance(renderer.bounds.center, prefab.transform.position) + renderer.bounds.extents.magnitude);
            float padding = Mathf.Max(0.1f, screenPadding) + radius + Random.Range(0f, Mathf.Max(0f, extraSpawnDistance));
            position = start + edge * Mathf.Clamp01(roll / Mathf.Max(length, 0.001f)) + outward * padding;
            return true;
        }
        return false;
    }

    private void EndWave()
    {
        waveEnded = true;
        currentWave = null;

        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveEnded();
    }
}
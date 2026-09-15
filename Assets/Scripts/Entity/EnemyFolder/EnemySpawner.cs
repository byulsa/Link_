using UnityEngine;
using System.Collections.Generic;
public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Data")]
    [SerializeField] private WaveTimeLine waveTimeLine;
    [SerializeField] private int poolLimit = 10;

    private readonly List<GameObject> poolList = new List<GameObject>();
    private float elapsedTime; // 게임 시작 후 전체 시간
    private float spawnTimer;  // 다음 스폰까지 시간

    private void Update()
    {
        elapsedTime += Time.deltaTime;
        spawnTimer += Time.deltaTime;

        if (elapsedTime >= waveTimeLine.maxTime)
            return;

        if (spawnTimer >= 2f)
        {
            spawnTimer = 0f;
            Spawn();
        }
    }
    private void Spawn()
    {
        if (waveTimeLine == null || waveTimeLine.waveData == null)
            return;

        foreach (var waveData in waveTimeLine.waveData)
        {
            if (elapsedTime >= waveData.startTime && elapsedTime < waveData.endTime)
            {
                if (waveData.enemyData == null || waveData.enemyData.Count == 0)
                    return;

                EnemyData enemyData = waveData.enemyData[Random.Range(0, waveData.enemyData.Count)];

                GameObject enemyObj = GetObjectFromPool(enemyData);

                if (enemyObj != null)
                {
                    enemyObj.transform.position = transform.position;

                    enemyObj.transform.rotation = transform.rotation;

                    enemyObj.SetActive(true);
                }

                return;
            }
        }
    }

    private GameObject GetObjectFromPool(EnemyData enemyData)
    {
        for (int i = 0; i < poolList.Count; i++)
        {
            if (!poolList[i].activeSelf)
            {
                return poolList[i];
            }
        }

        if (poolList.Count < poolLimit)
        {
            GameObject newObj = Instantiate(enemyData.enemyPrefab, transform.position, transform.rotation);

            Enemy enemyComp = newObj.GetComponent<Enemy>();
            if (enemyComp != null)
            {
                enemyComp.InitializeFromData(enemyData);
            }

            poolList.Add(newObj);
            return newObj;
        }

        return null;
    }
}

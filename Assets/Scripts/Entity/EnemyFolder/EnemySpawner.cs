using UnityEngine;
using System.Collections.Generic;
public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Data")]
    [SerializeField] private EnemyData enemyData;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private int poolLimit = 10;

    private readonly List<GameObject> poolList = new List<GameObject>();
    private float timer;

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            Spawn();
        }
    }

    private void Spawn()
    {
        if (enemyData == null || enemyData.enemyPrefab == null) return;

        GameObject enemyObj = GetObjectFromPool();

        if (enemyObj != null)
        {
            enemyObj.transform.position = transform.position;
            enemyObj.transform.rotation = transform.rotation;
            enemyObj.SetActive(true);
        }
    }

    private GameObject GetObjectFromPool()
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

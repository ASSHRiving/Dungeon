using UnityEngine;
using System.Collections.Generic;

public class Room01 : Room
{
    [Header("敵人生成設定")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private List<Transform> enemySpawnPoints;
    [SerializeField] private GameObject reward;
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    public override void Init()
    {
        reward.SetActive(false);
        foreach (var point in enemySpawnPoints)
        {
            GameObject enemyGo = Instantiate(enemyPrefab, point.position, point.rotation);
            spawnedEnemies.Add(enemyGo);
            EnemyMovementSystem enemyMovement = enemyGo.GetComponent<EnemyMovementSystem>();
            if (enemyMovement != null)
            {
                enemyMovement.SetSpawnPoint(point);
            }
        }
    }
    private void Update()
    {
        if (IsRoomCleared() && !reward.activeSelf)
        {
            reward.SetActive(true);
        }
    }
    private bool IsRoomCleared()
    {
        foreach (var enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                return false;
            }
        }
        return true;
    }
}

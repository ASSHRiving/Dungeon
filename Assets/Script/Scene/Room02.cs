using UnityEngine;
using System.Collections.Generic;

public class Room02: Room
{
    [Header("敵人生成設定")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private List<Transform> enemySpawnPoints;
    public override void Init()
    {
        foreach (var point in enemySpawnPoints)
        {
            GameObject enemyGo = Instantiate(enemyPrefab, point.position, point.rotation);
            EnemyMovementSystem enemyMovement = enemyGo.GetComponent<EnemyMovementSystem>();
            if (enemyMovement != null)
            {
                enemyMovement.SetSpawnPoint(point);
            }
        }
    }
}

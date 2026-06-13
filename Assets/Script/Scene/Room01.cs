using UnityEngine;

public class Room01 : Room
{
    [Header("敵人生成設定")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform enemySpawnPoint;
    public override void Init()
    {
        GameObject enemyGo = Instantiate(enemyPrefab, enemySpawnPoint.position, enemySpawnPoint.rotation);
    }
}

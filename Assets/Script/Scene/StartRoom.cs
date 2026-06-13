using UnityEngine;

public class StartRoom : Room
{
    [Header("玩家生成設定")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform playerSpawnPoint;
    public override void Init()
    {
        GameObject playerGo = Instantiate(playerPrefab, playerSpawnPoint.position, playerSpawnPoint.rotation);
        GameAssets.Instance.SetUpPlayerCamera(playerGo.transform);
    }
}

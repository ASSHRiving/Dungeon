using UnityEngine;
using Unity.Cinemachine;

public class StartRoom : Room
{
    [Header("玩家生成設定")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform playerSpawnPoint;
    public override void Init()
    {
        GameObject playerGo = Instantiate(playerPrefab, playerSpawnPoint.position, playerSpawnPoint.rotation);
        GameObject playerCamera = GameObject.FindGameObjectWithTag("PlayerCamera");
        if(playerCamera != null && playerCamera.TryGetComponent<CinemachineCamera>(out var camera))
        {
            camera.Target.TrackingTarget = playerGo.transform;
        }
    }
}

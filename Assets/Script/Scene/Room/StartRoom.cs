using UnityEngine;
using Unity.Cinemachine;

public class StartRoom : Room
{
    [Header("玩家生成設定")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform playerSpawnPoint;
    public override void Init()
    {
        //場上唯一Player
        GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
        if(playerGo != null)
        {
            CharacterController characterController = playerGo.GetComponent<CharacterController>();
            if(characterController != null)
            {
                characterController.enabled = false;
                playerGo.transform.position = playerSpawnPoint.position;
                characterController.enabled = true;
            }
            else
            {
                playerGo.transform.position = playerSpawnPoint.position;
            }
        }
        else
        {
            playerGo = Instantiate(playerPrefab, playerSpawnPoint.position, playerSpawnPoint.rotation);
        }
        DontDestroyOnLoad(playerGo);

        // 設定相機追蹤玩家
        GameObject playerCamera = GameObject.FindGameObjectWithTag("PlayerCamera");
        if(playerCamera != null && playerCamera.TryGetComponent<CinemachineCamera>(out var camera))
        {
            camera.Target.TrackingTarget = playerGo.transform;
        }
    }
}

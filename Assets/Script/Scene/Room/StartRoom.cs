using UnityEngine;
using Unity.Cinemachine;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;

public class StartRoom : Room
{
    [Header("玩家生成設定")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform playerSpawnPoint;
    public List<GameObject> initEquipment;
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
                playerGo.transform.rotation = playerSpawnPoint.rotation;
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
            CharacterEquipment equipment = playerGo.GetComponent<CharacterEquipment>();
            if(initEquipment != null)
            {
                foreach(var item in initEquipment)
                {
                    if(item == null) continue;
                    GameObject itemGo = Instantiate(item);
                    itemGo.GetComponent<Equipment>().SetIndex(0);
                    equipment.EquipItem(itemGo);
                }
            }

            // 套用局外養成屬性加成 (生命、防禦、攻擊)
            int charId = GameManager.Instance != null ? GameManager.Instance.characterID : 0;
            if (CharacterUpgradeManager.Instance != null)
            {
                CharacterUpgradeManager.Instance.ApplyUpgradesToPlayer(playerGo, charId);
            }
        }
        DontDestroyOnLoad(playerGo);

        // 設定相機追蹤玩家
        GameObject playerCamera = GameObject.FindGameObjectWithTag("PlayerCamera");
        if(playerCamera != null && playerCamera.TryGetComponent<CinemachineCamera>(out var camera))
        {
            camera.Target.TrackingTarget = playerGo.transform;
        }
        GameEvent.PlayerSpawn(playerGo.transform);
    }
}

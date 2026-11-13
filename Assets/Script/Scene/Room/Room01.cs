using UnityEngine;
using System.Collections.Generic;
using Unity.Cinemachine;
using System.Collections;


public class Room01 : FightingRoom
{
    [SerializeField] private List<GameObject> rewards;
    [SerializeField] private CinemachineCamera rewardCam;
    private float focusTime = 2.5f;

    private int level;
    protected override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);

        if (other.CompareTag("Player"))
        {
            if (!isClear)
            {
                GameAssets.Instance.PlayeliteFightMusic();
            }
        }
    }
    public override void Init()
    {
        base.Init();
        level = GameManager.Instance.currentLevel;
        foreach(var reward in rewards)
        {
            reward.SetActive(false);
        }
        if (rewardCam != null)
        {
            rewardCam.Priority = 0;
        }
    }
    protected override void OnRoomCleared()
    {
        base.OnRoomCleared();
        StartCoroutine(ChestFocusRoutine());
    }
    private IEnumerator ChestFocusRoutine()
    {
        // 1. 啟用當前關卡的獎勵寶箱
        int rewardIndex = level - 1;
        if (rewardIndex >= 0 && rewardIndex < rewards.Count && rewards[rewardIndex] != null)
        {
            GameObject currentReward = rewards[rewardIndex];
            currentReward.SetActive(true);

            // 如果每關寶箱位置不同，可動態將相機 Target 設為當前寶箱
            if (rewardCam != null)
            {
                rewardCam.Target.LookAtTarget = currentReward.transform; 
            }
        }

        // 2. 切換相機特寫
        if (rewardCam != null)
        {
            rewardCam.Priority = 20;
            yield return new WaitForSeconds(focusTime);
            rewardCam.Priority = 0;
        }
    }

}

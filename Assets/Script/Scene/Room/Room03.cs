using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Room03 : FightingRoom
{ 
    [SerializeField] private GameObject reward;
    protected override void OnBattleStarted()
    {
        base.OnBattleStarted();
        if (GameAssets.Instance != null)
        {
            GameAssets.Instance.PlayBossMusic();
        }
    }
    public override void Init()
    {
        base.Init();
        reward.SetActive(false);
    }

    void Update()
    {
        if (isClear && !reward.activeSelf)
        {
            reward.SetActive(true);
        }
    }
}
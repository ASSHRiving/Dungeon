using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Room03 : FightingRoom
{ 
    [SerializeField] private GameObject reward;
    protected override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);

        if (other.CompareTag("Player"))
        {
            if (!isBattleStarted && !isClear)
            {
                
            }
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
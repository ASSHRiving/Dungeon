using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Room03 : FightingRoom
{ 

    protected override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);

        if (other.CompareTag("Player"))
        {
            if (!isBattleStarted && !isClear)
            {
                GameAssets.Instance.PlayBossMusic();
            }
        }
    }
}
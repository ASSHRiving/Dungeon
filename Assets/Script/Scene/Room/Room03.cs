using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Room03 : FightingRoom
{ 

    public override void Init()
    {
        isBattleStarted = false;
        isSpawningWave = false;
        isClear = false;
        currentWaveIndex = 0;
    }

    protected override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);

        if (other.CompareTag("Player"))
        {
            if (!isBattleStarted && !isClear)
            {
                isBattleStarted = true;
                playerTransform = other.transform;

                CloseGate();
                GameAssets.Instance.PlayBossMusic();

                // 啟動第一波生成協程
                StartCoroutine(SpawnWaveRoutine(currentWaveIndex));
            }
        }
    }
}
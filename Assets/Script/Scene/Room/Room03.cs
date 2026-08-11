using UnityEngine;
using System.Collections.Generic;

public class Room03: Room
{
    [Header("敵人生成設定")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private List<Transform> enemySpawnPoints;
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private bool isClear = false;
    public override void Init()
    {
        if(enemyPrefab != null && enemySpawnPoints != null)
        {
            foreach (var point in enemySpawnPoints)
            {
                if(point == null) continue;
                GameObject enemyGo = Instantiate(enemyPrefab, point.position, point.rotation);
                spawnedEnemies.Add(enemyGo);
                EnemyCombatSystem combat = enemyGo.GetComponentInChildren<EnemyCombatSystem>();
                if (combat != null)
                {
                    combat.SetSpawnPoint(point);
                }
            }
        }
    }

    void Update()
    {
        if(IsRoomCleared() && !isClear)
        {
            isClear = true;
            OpenGate();
            GameAssets.Instance.PlayInGameMusic();
        }
    }

    protected override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);
        if (other.CompareTag("Player"))
        {
            if (!isClear)                  //首次進房
            {
                CloseGate();
                GameAssets.Instance.PlayBossMusic();
                foreach(var enemy in spawnedEnemies)
                {
                    EnemyCombatSystem combat = enemy.GetComponentInChildren<EnemyCombatSystem>();
                    if (combat != null)
                    {
                        combat.SetCurrentTarget(other.transform);
                    }
                }
            }
        }
    }
    private bool IsRoomCleared()
    {
        foreach (var enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                return false;
            }
        }
        return true;
    }
    private void CloseGate()
    {
        foreach(var exit in exitSets)
        {
            if (exit.door.activeSelf)
            {
                Gate gate = exit.door.GetComponent<Gate>();
                if(gate != null)
                {
                    gate.CloseGate();
                }
            }
        }
    }
    private void OpenGate()
    {
        foreach(var exit in exitSets)
        {
            if (exit.door.activeSelf)
            {
                Gate gate = exit.door.GetComponent<Gate>();
                if(gate != null)
                {
                    gate.OpenGate();
                }
            }
        }
    }
}

using UnityEngine;
using System.Collections.Generic;

public class Room02: Room
{
    [Header("敵人生成設定")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private List<Transform> enemySpawnPoints;
    public override void Init()
    {
        foreach (var point in enemySpawnPoints)
        {
            GameObject enemyGo = Instantiate(enemyPrefab, point.position, point.rotation);
            EnemyCombatSystem combat = enemyGo.GetComponent<EnemyCombatSystem>();
            if (combat != null)
            {
                combat.SetSpawnPoint(point);
            }
        }
    }
    protected override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);
        if (other.CompareTag("Player"))
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
    }
}

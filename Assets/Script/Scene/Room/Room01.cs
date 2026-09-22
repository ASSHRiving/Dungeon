using UnityEngine;
using System.Collections.Generic;

public class Room01 : FightingRoom
{
    [SerializeField] private List<GameObject> rewards;
    private int level;
    protected override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);

        if (other.CompareTag("Player"))
        {
            if (!isClear)
            {
                
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
    }
    void Update()
    {
        if (isClear && !rewards[level-1].activeSelf)
        {
            rewards[level-1].SetActive(true);
        }
    }
}

using UnityEngine;
using System;

public static class GameEvent
{
    //***************************************
    // 事件廣播：玩家生成
    //***************************************
    public static event Action<Transform> OnPlayerSpawn;
    public static void PlayerSpawn(Transform playerTransform)
    {
        OnPlayerSpawn?.Invoke(playerTransform);
    }

    //***************************************
    // 事件廣播：起始房間門鎖控制
    //***************************************
    public static event Action<bool> OnStartRoomDoorLockChanged;
    public static void StartRoomDoorLockChanged(bool locked)
    {
        OnStartRoomDoorLockChanged?.Invoke(locked);
    }

    //***************************************
    // 事件廣播：教學期間玩家攻擊鎖控制
    //***************************************
    public static event Action<bool> OnPlayerAttackLockChanged;
    public static void PlayerAttackLockChanged(bool locked)
    {
        OnPlayerAttackLockChanged?.Invoke(locked);
    }

    //***************************************
    // 事件廣播：戰鬥與玩家行為
    //***************************************
    public static event Action OnPlayerAttacked;
    public static void PlayerAttacked()
    {
        OnPlayerAttacked?.Invoke();
    }

    public static event Action OnPlayerRolled;
    public static void PlayerRolled()
    {
        OnPlayerRolled?.Invoke();
    }

    public static event Action OnPlayerPerfectDodge;
    public static void PlayerPerfectDodge()
    {
        OnPlayerPerfectDodge?.Invoke();
    }

    public static event Action<Transform> OnEnemyAttackStarted;
    public static void EnemyAttackStarted(Transform attacker)
    {
        OnEnemyAttackStarted?.Invoke(attacker);
    }

    public static event Action OnEnemyKilled;
    public static void EnemyKilled()
    {
        OnEnemyKilled?.Invoke();
    }

    public static event Action OnFightingRoomCleared;
    public static void FightingRoomCleared()
    {
        OnFightingRoomCleared?.Invoke();
    }
}
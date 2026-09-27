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
}
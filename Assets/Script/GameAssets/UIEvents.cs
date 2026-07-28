using UnityEngine;
using System;

public static class UIEvents
{
    //***************************************
    // 事件廣播：小地圖
    //***************************************
    public static event Action<Room, Room, Room.Direction> OnRoomSpawned;
    public static event Action<Room> OnRoomEntered;
    public static void RoomSpawned(Room room, Room parentRoom, Room.Direction dir)
    {
        OnRoomSpawned?.Invoke(room, parentRoom, dir);
    }
    public static void RoomEntered(Room room)
    {
        OnRoomEntered?.Invoke(room);
    }

    //***************************************
    // 事件廣播：金幣
    //***************************************
    public static event Action<int> OnGoldChanged;
    public static void GoldChanged(int newGoldAmount)
    {
        OnGoldChanged?.Invoke(newGoldAmount);
    }
    //***************************************
    // 事件廣播：交互
    //***************************************
    public static event Action<string> OnInteractableDetected;
    public static void InteractableDetected(string interactableName)
    {
        OnInteractableDetected?.Invoke(interactableName);
    }
}
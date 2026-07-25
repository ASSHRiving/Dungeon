using UnityEngine;
using System.Collections.Generic;

public abstract class Room : MonoBehaviour
{
    public enum Direction { North, South, East, West }
    [SerializeField] private Transform Bounds;
    public bool isVisited = false;

    [System.Serializable]
    public class ExitSet
    {
        public Direction direction;
        public Transform anchor;
        public GameObject door;
        public GameObject wall;
    }
    
    [SerializeField] private List<ExitSet> exitSets;

    void Awake()
    {
        foreach (var exit in exitSets)
        {
            if (exit.door != null) exit.door.SetActive(false);
            if (exit.wall != null) exit.wall.SetActive(true);
        }
    }

    public Transform GetExitAnchor(Direction dir)
    {
        foreach (var anchor in exitSets)
        {
            if (anchor.direction == dir) return anchor.anchor;
        }
        Debug.LogWarning($"{gameObject.name} 找不到 {dir} 方向的出口！");
        return null;
    }
    public void OpenExit(Direction dir)
    {
        foreach (var anchor in exitSets)
        {
            if (anchor.direction == dir)
            {
                if (anchor.door != null) anchor.door.SetActive(true);
                if (anchor.wall != null) anchor.wall.SetActive(false);
                return;
            }
        }
        Debug.LogWarning($"{gameObject.name} 找不到 {dir} 方向的出口！");
    }
    public void CloseExit(Direction dir)
    {
        foreach (var anchor in exitSets)
        {
            if (anchor.direction == dir)
            {
                if (anchor.door != null) anchor.door.SetActive(false);
                if (anchor.wall != null) anchor.wall.SetActive(true);
                return;
            }
        }
        Debug.LogWarning($"{gameObject.name} 找不到 {dir} 方向的出口！");
    }
    public Transform GetBounds()
    {
        if(Bounds != null) return Bounds;
        return null;
    }
    private void OnTriggerEnter(Collider other)
    {
        // 確認踩進來的是 Player
        if (other.CompareTag("Player"))
        {
            isVisited = true;

            // 【廣播】通知小地圖：玩家進這個房間了！
            MinimapEvents.RoomEntered(this);
        }
    }
    public abstract void Init();
    
}

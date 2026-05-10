using UnityEngine;
using System.Collections.Generic;

public abstract class Room : MonoBehaviour
{
    public enum Direction { North, South, East, West }

    [System.Serializable]
    public class ExitAnchor
    {
        public Direction direction;
        public Transform transform;
    }
    public List<ExitAnchor> exitAnchors;

    public Transform GetExitAnchor(Direction dir)
    {
        foreach (var anchor in exitAnchors)
        {
            if (anchor.direction == dir) return anchor.transform;
        }
        Debug.LogWarning($"{gameObject.name} 找不到 {dir} 方向的出口！");
        return null;
    }
    [SerializeField] private Transform Bounds;
    public Transform GetBounds()
    {
        if(Bounds != null) return Bounds;
        return null;
    }
    
}

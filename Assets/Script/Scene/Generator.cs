using UnityEngine;
using System.Collections.Generic;

public class Generator : MonoBehaviour
{
    public GameObject startRoomPrefab;
    public List<GameObject> roomPrefabs;
    public GameObject tunnelPrefab;
    private int count = 5;
    private Dictionary<Vector2Int, Room> gridMap = new Dictionary<Vector2Int, Room>();

    public LayerMask roomBoundsLayer;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Generate();
    }
    void Generate()
    {
        Room currentRoom = Instantiate(startRoomPrefab, Vector3.zero, Quaternion.identity).GetComponent<Room>();
        Room prevRoom = null;
        Vector2Int currentPos = Vector2Int.zero;
        gridMap.Add(currentPos, currentRoom);
        int trys = 0;

        for(int i = 0; i < count; i++)
        {
            if(trys > 5)
            {
                Debug.Log("嘗試次數過多，停止生成...");
                break;
            }
            Vector2Int nextDir = GetNextPosition(currentPos);
            Room.Direction direction = Vector2ToDirection(nextDir);
            Vector2Int nextPos = currentPos + nextDir;
            if (gridMap.ContainsKey(nextPos)) {
                trys++;
                i--;
                continue;
            }
            GameObject prefab = roomPrefabs[Random.Range(0, roomPrefabs.Count)];
            Room nextRoom = SpawnRoom(prefab, nextPos, direction, currentRoom);
            if(nextRoom == null)
            {
                i--;
                trys++;
                continue;
            }
            trys = 0;
            prevRoom = currentRoom;
            currentRoom = nextRoom;
            currentPos = nextPos;
        }
    }
    Room SpawnRoom(GameObject prefab, Vector2Int pos, Room.Direction dir, Room currentRoom)
    {
        Transform ExitPos = currentRoom.GetExitAnchor(dir);
        GameObject goTunnel = Instantiate(tunnelPrefab, Vector3.zero, ExitPos.rotation);
        Tunnel tunnel = goTunnel.GetComponent<Tunnel>();
        Transform tunnelEntry = tunnel.getEntry();
        Transform tunnelExit = tunnel.getExit();
        if (tunnel != null)
        {
            goTunnel.transform.position = ExitPos.position - (tunnelEntry.position - goTunnel.transform.position);
        }

        GameObject goB = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        Room newRoom = goB.GetComponent<Room>();
        Room.Direction entryDir = GetOpposite(dir);
        Transform entryB = newRoom.GetExitAnchor(entryDir);
        if(entryB != null)
        {
            goB.transform.position = tunnelExit.position - (entryB.position - goB.transform.position);
        }
        Physics.SyncTransforms();
        Transform bounds = newRoom.GetBounds();
        if(bounds != null)
        {
            BoxCollider box = bounds.GetComponent<BoxCollider>();
            Collider[] colliders = Physics.OverlapBox(
                box.bounds.center, 
                box.bounds.extents * 0.95f, 
                bounds.rotation, 
                roomBoundsLayer
            );
            foreach(var hit in colliders)
            {
                if(hit.transform != bounds)
                {
                    Debug.Log("生成失敗，與現有房間重疊，重新生成...");
                    Destroy(goB);
                    Destroy(goTunnel);
                    return null;
                }
            }
        }
        currentRoom.OpenExit(dir);
        newRoom.OpenExit(entryDir);
        gridMap.Add(pos, newRoom);
        return newRoom;
    }
    Vector2Int GetNextPosition(Vector2Int currentPos)
    {
        List<Vector2Int> directions = new List<Vector2Int>
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };
        return directions[Random.Range(0, directions.Count)];
    }
    Room.Direction Vector2ToDirection(Vector2Int dir)
    {
        if(dir == Vector2Int.up)return Room.Direction.North;
        if(dir == Vector2Int.down)return Room.Direction.South;
        if(dir == Vector2Int.right)return Room.Direction.East;
        if(dir == Vector2Int.left)return Room.Direction.West;
        return Room.Direction.North;
    }
    Room.Direction GetOpposite(Room.Direction dir)
    {
        switch (dir)
        {
            case Room.Direction.North: return Room.Direction.South;
            case Room.Direction.South: return Room.Direction.North;
            case Room.Direction.East:  return Room.Direction.West;
            case Room.Direction.West:  return Room.Direction.East;
            default: return Room.Direction.North;
        }
    }
}

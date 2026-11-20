using UnityEngine;
using System.Collections.Generic;
using Unity.AI.Navigation;

public class Generator : MonoBehaviour
{
    [Header("Room Prefabs")]
    [Header("起始房間(出生點)")]
    [SerializeField] private GameObject startRoomPrefab;
    [Header("終點房間(前往下一關的傳送門房)")]
    [SerializeField] private GameObject endRoomPrefab;
    [Header("Boss房間(遊戲最終關卡)")]
    [SerializeField] private GameObject bossRoomPrefab;
    [Header("一般房間(隨機選取)")]
    [SerializeField] private List<GameObject> roomPrefabs;
    [Header("特殊房間(隨機選取)")]
    [SerializeField] private List<GameObject> extraRoomPrefabs;
    [Header("通道 Prefab(連接房間之間的通道)")]
    [SerializeField] private GameObject tunnelPrefab;
    private int count;
    private int level = 1;

    //儲存已生成房間的陣列
    private List<Room> spawnedRooms = new List<Room>();
    private List<Room> extraRooms = new List<Room>();
    private Room lastRoom;

    public LayerMask roomBoundsLayer;

    private int maxGenerateAttempts = 20;
    private Transform mapRoot;

    [Header("AI 導航組件")]
    public List<NavMeshSurface> navMeshSurfaces;

    void Start()
    {
        if(GameManager.Instance != null)
        {
            level = GameManager.Instance.currentLevel;
        }
        count = level + 1;
        Debug.Log($"[Generator] 開始生成第 {level} 關地圖，目標房間數：{count}");

        mapRoot = new GameObject("GeneratedMap").transform;
        mapRoot.SetParent(transform);

        bool success = false;
        for(int i = 0; i < maxGenerateAttempts; i++)
        {
            ClearMap();
            spawnedRooms.Clear();
            extraRooms.Clear();
            if (Generate())
            {
                success = true;
                break;
            }
            Debug.LogWarning($"[Generator] 第 {i + 1} 次生成失敗，重新生成地圖。");
        }
        if (!success)
        {
            Debug.LogError("[Generator] 超過最大生成次數，無法生成完整地圖！");
            return;
        }

        GenerateExtraRooms();
        InitNavMesh();
        InitRooms();

        UIEvents.LevelChanged($"1 - {level}");
    }
    private bool Generate()
    {
        count = level + 1;
        //生成起始房間
        Room currentRoom = Instantiate(startRoomPrefab, Vector3.zero, Quaternion.identity, mapRoot).GetComponent<Room>();
        UIEvents.RoomSpawned(currentRoom, null, Room.Direction.North);
        spawnedRooms.Add(currentRoom);
        //生成第一個房間
        GameObject prefab = roomPrefabs[Random.Range(0, roomPrefabs.Count)];
        Room nextRoom = SpawnRoom(prefab, Room.Direction.North, currentRoom);
        if (nextRoom == null)
        {
            Debug.LogWarning("[Generator] 第一個房間生成失敗，停止本次生成。");
            return false;
        }
        Room prevRoom = currentRoom;
        currentRoom = nextRoom;
        spawnedRooms.Add(currentRoom);
        count--;

        int trys = 0;
        for(int i = 0; i < count; i++)
        {
            if(trys > 5)
            {
                Debug.Log("嘗試次數過多，停止生成...");
                return false;
            }
            Room.Direction direction = (Room.Direction)Random.Range(0, 4);

            prefab = roomPrefabs[Random.Range(0, roomPrefabs.Count)];
            nextRoom = SpawnRoom(prefab, direction, currentRoom);
            if(nextRoom == null)
            {
                i--;
                trys++;
                continue;
            }
            trys = 0;
            prevRoom = currentRoom;
            currentRoom = nextRoom;
            spawnedRooms.Add(currentRoom);
        }

        //生成終點房間
        trys = 0;
        if(level < 4)
        {
            while (true)
            {
                if(trys > 5)
                {
                    Debug.LogWarning("終點房間生成失敗，請檢查房間配置！");
                    return false;
                }
                Room.Direction dir = (Room.Direction)Random.Range(0, 4);
                lastRoom = SpawnRoom(endRoomPrefab, dir, currentRoom);
                if(lastRoom != null)
                {
                    //spawnedRooms.Add(nextRoom);
                    break;
                }
                trys++;
            }
        }
        else
        {
            while (true)
            {
                if(trys > 5)
                {
                    Debug.LogWarning("Boss房間生成失敗，請檢查房間配置！");
                    return false;
                }
                Room.Direction dir = (Room.Direction)Random.Range(0, 4);
                lastRoom = SpawnRoom(bossRoomPrefab, dir, currentRoom);
                if(lastRoom != null)
                {
                    //spawnedRooms.Add(nextRoom);
                    break;
                }
                trys++;
            }
        }
        return true;
    }
    private void GenerateExtraRooms()
    {
        int extraCount = level;
        int trys = 0;
        if(level > 1)
        {
            while (true)
            {
                if(trys > 10)
                {
                    Debug.Log("嘗試次數過多，停止生成...");
                    return;
                }
                Room randomRoom = spawnedRooms[Random.Range(1, spawnedRooms.Count)];
                Room.Direction dir = (Room.Direction)Random.Range(0, 4);
                
                GameObject prefab = extraRoomPrefabs[0];
                Room newRoom = SpawnExtraRoom(prefab, dir, randomRoom);
                if(newRoom != null)
                {
                    extraRooms.Add(newRoom);
                    extraCount--;
                    break;
                }else
                {
                    trys++;
                }
            }
        }
        trys = 0;
        for(int i = 0; i < extraCount; i++)
        {
            if(trys > 10)
            {
                Debug.Log("嘗試次數過多，停止生成...");
                return;
            }
            Room randomRoom = spawnedRooms[Random.Range(1, spawnedRooms.Count)];
            Room.Direction dir = (Room.Direction)Random.Range(0, 4);
            
            GameObject prefab = extraRoomPrefabs[Random.Range(0, extraRoomPrefabs.Count)];
            Room newRoom = SpawnExtraRoom(prefab, dir, randomRoom);
            if(newRoom == null)
            {
                i--;
                trys++;
                continue;
                
            }
            trys = 0;
            extraRooms.Add(newRoom);
        }
    }
    Room SpawnRoom(GameObject prefab, Room.Direction dir, Room currentRoom)
    {
        //連接通道
        GameObject goTunnel = SpawnTunnel(dir, currentRoom);
        if (goTunnel == null) return null;
        Transform tunnelExit = goTunnel.GetComponent<Tunnel>().getExit();

        //房間B
        GameObject goB = Instantiate(prefab, Vector3.zero, Quaternion.identity, mapRoot);
        Room newRoom = goB.GetComponent<Room>();
        Room.Direction entryDir = GetOpposite(dir);
        Transform entryB = newRoom != null ? newRoom.GetExitAnchor(entryDir) : null;
        if (newRoom == null || entryB == null)
        {
            DestroyImmediate(goB);
            DestroyImmediate(goTunnel);
            return null;
        }
        goB.transform.position = tunnelExit.position - (entryB.position - goB.transform.position);

        //檢測重疊（忽略剛連接的通道）
        if (!CheckBounds(newRoom.transform, goTunnel.transform))
        {
            DestroyImmediate(goB);
            DestroyImmediate(goTunnel);
            return null;
        }
        //開門
        currentRoom.OpenExit(dir);
        newRoom.OpenExit(entryDir);
        UIEvents.RoomSpawned(newRoom, currentRoom, dir);
        return newRoom;
    }
    private Room SpawnExtraRoom(GameObject prefab, Room.Direction dir, Room currentRoom)
    {
        Transform ExitPos = currentRoom.GetExitAnchor(dir);
        if (ExitPos == null) return null;

        //連接通道
        GameObject goTunnel = SpawnTunnel(dir, currentRoom);
        if (goTunnel == null) return null;
        Transform tunnelExit = goTunnel.GetComponent<Tunnel>().getExit();

        //生成額外房間
        GameObject goB = Instantiate(prefab, Vector3.zero, ExitPos.rotation, mapRoot);
        Room newRoom = goB.GetComponent<Room>();
        Room.Direction entryDir = GetOpposite(dir);
        Transform entryB = newRoom != null ? newRoom.GetExitAnchor(entryDir) : null;
        if (newRoom == null || entryB == null)
        {
            DestroyImmediate(goB);
            DestroyImmediate(goTunnel);
            return null;
        }
        goB.transform.position = tunnelExit.position - (entryB.position - goB.transform.position);

        //檢測重疊（忽略剛連接的通道）
        if (!CheckBounds(newRoom.transform, goTunnel.transform))
        {
            DestroyImmediate(goB);
            DestroyImmediate(goTunnel);
            return null;
        }
        //開門
        currentRoom.OpenExit(dir);
        newRoom.OpenExit(entryDir);
        UIEvents.RoomSpawned(newRoom, currentRoom, dir);
        return newRoom;
    }
    private GameObject SpawnTunnel(Room.Direction dir, Room currentRoom)
    {
        Transform ExitPos = currentRoom.GetExitAnchor(dir);
        if (ExitPos == null) return null;

        GameObject goTunnel = Instantiate(tunnelPrefab, Vector3.zero, ExitPos.rotation, mapRoot);
        Tunnel tunnel = goTunnel.GetComponent<Tunnel>();
        if (tunnel == null)
        {
            Debug.LogError("Tunnel prefab does not have a Tunnel component.");
            DestroyImmediate(goTunnel);
            return null;
        }
        Transform tunnelEntry = tunnel.getEntry();
        Transform tunnelExit = tunnel.getExit();
        if (tunnelEntry == null || tunnelExit == null)
        {
            Debug.LogError("Tunnel prefab 缺少 Entry 或 Exit 錨點。");
            DestroyImmediate(goTunnel);
            return null;
        }
        goTunnel.transform.position = ExitPos.position - (tunnelEntry.position - goTunnel.transform.position);

        //檢測通道是否與現有房間或其他通道重疊（忽略出發房間）
        if (!CheckBounds(goTunnel.transform, currentRoom.transform))
        {
            DestroyImmediate(goTunnel);
            return null;
        }
        return goTunnel;
    }
    private bool CheckBounds(Transform target, Transform ignore = null)
    {
        if (target == null) return false;

        Physics.SyncTransforms();
        BoxCollider box = target.GetComponent<BoxCollider>();
        if (box == null) return false;

        Vector3 worldCenter = box.transform.TransformPoint(box.center);
        Vector3 scale = box.transform.lossyScale;
        Vector3 halfExtents = new Vector3(
            Mathf.Abs(box.size.x * scale.x),
            Mathf.Abs(box.size.y * scale.y),
            Mathf.Abs(box.size.z * scale.z)
        ) * 0.5f * 0.95f;

        Collider[] colliders = Physics.OverlapBox(
            worldCenter,
            halfExtents,
            box.transform.rotation,
            roomBoundsLayer
        );

        foreach (var hit in colliders)
        {
            if (hit.transform == target || hit.transform == ignore)
                continue;

            Debug.Log("生成失敗，與現有房間或通道重疊，重新生成...");
            return false;
        }
        return true;
    }
    private void ClearMap()
    {
        lastRoom = null;
        UIEvents.MapCleared();
        for (int i = mapRoot.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(mapRoot.GetChild(i).gameObject);
        }
    }

    private void InitRooms()
    {
        foreach(var room in spawnedRooms)
        {
            room.Init();
        }
        foreach(var room in extraRooms)
        {
            room.Init();
        }
        if (lastRoom != null)
        {
            lastRoom.Init();
        }
    }
    private void InitNavMesh()
    {
        if (navMeshSurfaces != null)
        {
            Physics.SyncTransforms();
            foreach(var surface in navMeshSurfaces)
            {
                surface.BuildNavMesh();
            }
        }
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

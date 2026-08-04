using UnityEngine;
using System.Collections.Generic;
using Unity.AI.Navigation;

public class Generator : MonoBehaviour
{
    [Header("Room Prefabs")]
    [SerializeField] private GameObject startRoomPrefab;
    [SerializeField] private GameObject endRoomPrefab;
    [SerializeField] private List<GameObject> roomPrefabs;
    [SerializeField] private List<GameObject> extraRoomPrefabs;
    [SerializeField] private GameObject tunnelPrefab;
    [SerializeField] private int count;
    [SerializeField] private int level = 1;
    private List<Room> spawnedRooms = new List<Room>();
    private List<Room> extraRooms = new List<Room>();
    public LayerMask roomBoundsLayer;

    [Header("AI 導航組件")]
    public NavMeshSurface navMeshSurface;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;

        if(GameManager.Instance != null)
        {
            level = GameManager.Instance.currentLevel;
        }
        count = level;
        Debug.Log($"[Generator] 開始生成第 {level} 關地圖，目標房間數：{count}");
        Generate();
        InitRooms();
        UIEvents.LevelChanged($"1 - {level}");
    }
    void Generate()
    {
        //生成起始房間
        Room currentRoom = Instantiate(startRoomPrefab, Vector3.zero, Quaternion.identity).GetComponent<Room>();
        UIEvents.RoomSpawned(currentRoom, null, Room.Direction.North);
        spawnedRooms.Add(currentRoom);
        //生成第一個房間
        GameObject prefab = roomPrefabs[Random.Range(0, roomPrefabs.Count)];
        Room nextRoom = SpawnRoom(prefab, Room.Direction.North, currentRoom);
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
                break;
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
        while (true)
        {
            if(trys > 5)
            {
                Debug.LogWarning("終點房間生成失敗，請檢查房間配置！");
                break;
            }
            Room.Direction dir = (Room.Direction)Random.Range(0, 4);
            nextRoom = SpawnRoom(endRoomPrefab, dir, currentRoom);
            if(nextRoom != null)
            {
                //spawnedRooms.Add(nextRoom);
                break;
            }
            trys++;
        }
        GenerateExtraRooms();
        if (navMeshSurface != null)
        {
            Debug.Log("地圖生成完全結束，開始即時烘焙 NavMesh...");
            navMeshSurface.BuildNavMesh(); 
        }
    }
    private void GenerateExtraRooms()
    {
        int extraCount = Random.Range(1, 3);

        for(int i = 0; i < extraCount; i++)
        {
            Room randomRoom = spawnedRooms[Random.Range(1, spawnedRooms.Count)];
            Room.Direction dir = (Room.Direction)Random.Range(0, 4);
            
            GameObject prefab = extraRoomPrefabs[Random.Range(0, extraRoomPrefabs.Count)];
            Room newRoom = SpawnExtraRoom(prefab, dir, randomRoom);
            if(newRoom == null)
            {
                i--;
                continue;
            }
            extraRooms.Add(newRoom);
        }
    }
    Room SpawnRoom(GameObject prefab, Room.Direction dir, Room currentRoom)
    {
        //連接通道
        Transform ExitPos = currentRoom.GetExitAnchor(dir);
        GameObject goTunnel = Instantiate(tunnelPrefab, Vector3.zero, ExitPos.rotation);
        Tunnel tunnel = goTunnel.GetComponent<Tunnel>();
        Transform tunnelEntry = tunnel.getEntry();
        Transform tunnelExit = tunnel.getExit();
        if (tunnel != null)
        {
            goTunnel.transform.position = ExitPos.position - (tunnelEntry.position - goTunnel.transform.position);
        }
        //房間B
        GameObject goB = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        Room newRoom = goB.GetComponent<Room>();
        Room.Direction entryDir = GetOpposite(dir);
        Transform entryB = newRoom.GetExitAnchor(entryDir);
        if(entryB != null)
        {
            goB.transform.position = tunnelExit.position - (entryB.position - goB.transform.position);
        }

        //檢測重疊
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
        //開門
        currentRoom.OpenExit(dir);
        newRoom.OpenExit(entryDir);
        UIEvents.RoomSpawned(newRoom, currentRoom, dir);
        return newRoom;
    }
    private Room SpawnExtraRoom(GameObject prefab, Room.Direction dir, Room currentRoom)
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
        //旋轉房間
        GameObject goB = Instantiate(prefab, Vector3.zero, ExitPos.rotation);
        Room newRoom = goB.GetComponent<Room>();
        Room.Direction entryDir = GetOpposite(dir);
        Transform entryB = newRoom.GetExitAnchor(entryDir);
        if(entryB != null)
        {
            goB.transform.position = tunnelExit.position - (entryB.position - goB.transform.position);
        }

        //檢測重疊
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
        //開門
        currentRoom.OpenExit(dir);
        newRoom.OpenExit(entryDir);
        UIEvents.RoomSpawned(newRoom, currentRoom, dir);
        return newRoom;
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

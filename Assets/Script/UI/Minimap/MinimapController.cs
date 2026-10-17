using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MinimapController : MonoBehaviour
{
    public const string MinimapModePrefKey = "MinimapModeSetting";

    [SerializeField] private UIController uiController;

    public enum MinimapMode
    {
        RotateMap, // 旋轉地圖（以玩家視角為前方）
        FixedMap   // 固定地圖（以正北北方為上方，旋轉指針）
    }

    [Header("模式設定")]
    public MinimapMode currentMode = MinimapMode.RotateMap; // 預設模式

    [Header("UI 相關繫結")]
    public RectTransform mapContainer;       // 負責「旋轉」的容器 (MapContainer)
    public RectTransform mapContent;         // 負責「位移」的容器 (MapContent)
    public RectTransform playerIcon;         // 玩家指針 (UI_PlayerIcon)
    public TMP_Dropdown modeDropdown;        // 下拉選單 (UI_Dropdown) 用於切換模式
    public GameObject roomNodePrefab;        // UI_RoomNode Prefab
    public GameObject connectionLinePrefab;  // UI_ConnectionLine Prefab

    [Header("地圖距離與跟隨設定")]
    public float roomGridDistance = 60f;      // 房間之間的像素距離
    public float moveSmoothSpeed = 8f;        // 地圖平滑移動速度
    public float rotateSmoothSpeed = 10f;     // 地圖平滑旋轉速度

    [Header("狀態色彩設定")]
    public Color currentRoomColor = Color.green;                           // 當前房間 (亮綠)
    public Color visitedRoomColor = Color.white;                           // 已探索過的房間 (白色)
    public Color unvisitedRoomColor = new Color(0.3f, 0.3f, 0.3f, 1f);   // 尚未探索

    // 單一字典統一管理實體 Room 與 RoomNodeUI 的映射
    private readonly Dictionary<Room, RoomNodeUI> roomNodes = new Dictionary<Room, RoomNodeUI>();

    // 核心數據：紀錄每一條 UI 通道連接著哪兩個 Room，以及對應的通道 GameObject
    private class UIConnection
    {
        public Room roomA;
        public Room roomB;
        public GameObject lineGO;
    }

    private readonly List<UIConnection> allConnections = new List<UIConnection>();

    // 目標 UI 偏移量
    private Vector2 targetMapPosition = Vector2.zero;

    private void OnEnable()
    {
        UIEvents.OnRoomSpawned += HandleRoomSpawned;
        UIEvents.OnRoomEntered += HandlePlayerEnteredRoom;
    }

    private void OnDisable()
    {
        UIEvents.OnRoomSpawned -= HandleRoomSpawned;
        UIEvents.OnRoomEntered -= HandlePlayerEnteredRoom;
    }

    private void Start()
    {
        int savedMode = PlayerPrefs.GetInt(MinimapModePrefKey, (int)MinimapMode.RotateMap);
        SetMinimapMode((MinimapMode)savedMode);

        if (modeDropdown != null)
        {
            modeDropdown.SetValueWithoutNotify(savedMode);
            modeDropdown.RefreshShownValue();
        }
    }

    private void Update()
    {
        if (mapContent != null)
        {
            // 1. 平滑移動 mapContent，讓當前房間推到 (0,0) 正中央
            mapContent.anchoredPosition = Vector2.Lerp(
                mapContent.anchoredPosition,
                targetMapPosition,
                Time.deltaTime * moveSmoothSpeed
            );
        }

        UpdateMapRotation();
    }

    private void UpdateMapRotation()
    {
        if (uiController == null || mapContainer == null) return;

        Transform camTransform = uiController.GetCameraTransform();
        if (camTransform == null) return;

        float playerYRotation = camTransform.eulerAngles.y;

        if (currentMode == MinimapMode.RotateMap)
        {
            // 【方案 A：轉地圖】
            Quaternion targetMapRot = Quaternion.Euler(0f, 0f, playerYRotation);
            mapContainer.rotation = Quaternion.Slerp(
                mapContainer.rotation,
                targetMapRot,
                Time.deltaTime * rotateSmoothSpeed
            );

            if (playerIcon != null)
            {
                playerIcon.localRotation = Quaternion.Slerp(
                    playerIcon.localRotation,
                    Quaternion.identity,
                    Time.deltaTime * rotateSmoothSpeed
                );
            }
        }
        else if (currentMode == MinimapMode.FixedMap)
        {
            // 【方案 B：固定地圖轉指針】
            mapContainer.rotation = Quaternion.Slerp(
                mapContainer.rotation,
                Quaternion.identity,
                Time.deltaTime * rotateSmoothSpeed
            );

            if (playerIcon != null)
            {
                Quaternion targetIconRot = Quaternion.Euler(0f, 0f, -playerYRotation);
                playerIcon.localRotation = Quaternion.Slerp(
                    playerIcon.localRotation,
                    targetIconRot,
                    Time.deltaTime * rotateSmoothSpeed
                );
            }
        }
    }

    /// <summary>
    /// 提供給 UI 設定面板或外部呼叫的動態切換接口
    /// </summary>
    public void SetMinimapMode(MinimapMode newMode)
    {
        currentMode = newMode;
        PlayerPrefs.SetInt(MinimapModePrefKey, (int)newMode);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// UI Dropdown (下拉選單) 用的 Int 轉換接口
    /// </summary>
    public void SetMinimapModeFromDropdown(int modeIndex)
    {
        SetMinimapMode((MinimapMode)modeIndex);
    }

    /// <summary>
    /// 清空小地圖上的所有房間節點與連線（可用於重置關卡時呼叫）
    /// </summary>
    public void ClearMinimap()
    {
        foreach (var kvp in roomNodes)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value.gameObject);
            }
        }
        roomNodes.Clear();

        foreach (var conn in allConnections)
        {
            if (conn.lineGO != null)
            {
                Destroy(conn.lineGO);
            }
        }
        allConnections.Clear();
        targetMapPosition = Vector2.zero;
    }

    // 收到「房間生成」時執行
    private void HandleRoomSpawned(Room newRoom, Room fromRoom, Room.Direction dir)
    {
        if (newRoom == null || roomNodePrefab == null || mapContent == null) return;

        Vector2 newUIPos = Vector2.zero;

        if (fromRoom != null && roomNodes.TryGetValue(fromRoom, out RoomNodeUI fromNode))
        {
            Vector2 fromUIPos = fromNode.UIPosition;
            Vector2 offset = GetDirectionOffset(dir) * roomGridDistance;
            newUIPos = fromUIPos + offset;

            // 繪製兩房間之間的通道線段
            GameObject connectionGO = DrawConnection(fromUIPos, newUIPos);
            if (connectionGO != null)
            {
                allConnections.Add(new UIConnection { roomA = fromRoom, roomB = newRoom, lineGO = connectionGO });
                connectionGO.SetActive(false);
            }
        }

        // 生成房間 UI 節點並初始化
        GameObject nodeGO = Instantiate(roomNodePrefab, mapContent);
        if (!nodeGO.TryGetComponent(out RoomNodeUI nodeUI))
        {
            nodeUI = nodeGO.AddComponent<RoomNodeUI>();
        }

        nodeUI.Initialize(newUIPos, newRoom.RoomImage, unvisitedRoomColor);
        nodeUI.SetVisible(false);

        roomNodes[newRoom] = nodeUI;
    }

    // 收到「玩家切換房間」時執行
    private void HandlePlayerEnteredRoom(Room currentActiveRoom)
    {
        if (currentActiveRoom == null) return;

        if (roomNodes.TryGetValue(currentActiveRoom, out RoomNodeUI currentNode))
        {
            targetMapPosition = -currentNode.UIPosition;
        }

        RevealRoomAndConnectedEdges(currentActiveRoom);

        foreach (var pair in roomNodes)
        {
            Room room = pair.Key;
            RoomNodeUI nodeUI = pair.Value;
            if (room == null || nodeUI == null) continue;

            if (room == currentActiveRoom)
            {
                nodeUI.SetColor(currentRoomColor);
            }
            else if (room.isVisited)
            {
                nodeUI.SetColor(visitedRoomColor);
            }
            else
            {
                nodeUI.SetColor(unvisitedRoomColor);
            }
        }
    }

    private void RevealRoomAndConnectedEdges(Room currentRoom)
    {
        // A. 先將當前房間 UI 顯示出來
        if (roomNodes.TryGetValue(currentRoom, out RoomNodeUI currentNode))
        {
            currentNode.SetVisible(true);
        }

        // B. 遍歷所有的通道紀錄，找出跟 currentRoom 連接的線段
        foreach (var conn in allConnections)
        {
            if (conn.roomA == currentRoom || conn.roomB == currentRoom)
            {
                if (conn.lineGO != null)
                {
                    conn.lineGO.SetActive(true);
                }

                Room neighborRoom = (conn.roomA == currentRoom) ? conn.roomB : conn.roomA;
                if (neighborRoom != null && roomNodes.TryGetValue(neighborRoom, out RoomNodeUI neighborNode))
                {
                    neighborNode.SetVisible(true);
                }
            }
        }
    }

    // 將枚舉方位轉為 UI 座標向量
    private Vector2 GetDirectionOffset(Room.Direction dir)
    {
        switch (dir)
        {
            case Room.Direction.North: return Vector2.up;
            case Room.Direction.South: return Vector2.down;
            case Room.Direction.East:  return Vector2.right;
            case Room.Direction.West:  return Vector2.left;
            default: return Vector2.zero;
        }
    }

    // 繪製 UI 通道線段
    private GameObject DrawConnection(Vector2 posA, Vector2 posB)
    {
        if (connectionLinePrefab == null || mapContent == null) return null;

        GameObject lineGO = Instantiate(connectionLinePrefab, mapContent);
        lineGO.transform.SetAsFirstSibling();

        RectTransform rect = lineGO.GetComponent<RectTransform>();
        if (rect != null)
        {
            Vector2 dir = (posB - posA).normalized;
            float distance = Vector2.Distance(posA, posB);

            rect.anchoredPosition = posA;
            rect.sizeDelta = new Vector2(distance, 4f);
            rect.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        }

        return lineGO;
    }
}
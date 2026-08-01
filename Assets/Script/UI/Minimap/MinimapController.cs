using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MinimapController : MonoBehaviour
{
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

    [Header("玩家參照 (負責讀取旋轉)")]
    public Transform playerTransform;        // 玩家角色的 Transform

    [Header("地圖距離與跟隨設定")]
    public float roomGridDistance = 60f;      // 房間之間的像素距離
    public float moveSmoothSpeed = 8f;        // 地圖平滑移動速度
    public float rotateSmoothSpeed = 10f;     // 地圖平滑旋轉速度

    [Header("狀態色彩設定")]
    public Color currentRoomColor = Color.green;                           // 當前房間 (亮綠)
    public Color visitedRoomColor = Color.white;                           // 已探索過的房間 (白色)
    public Color unvisitedRoomColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);   // 尚未探索 (半透明灰色)

    // 紀錄實體 Room 對應的 UI 座標與 Image 組件
    private Dictionary<Room, Vector2> roomUIPositions = new Dictionary<Room, Vector2>();
    private Dictionary<Room, GameObject> roomUINodes = new Dictionary<Room, GameObject>();
    private Dictionary<Room, Image> roomUIImages = new Dictionary<Room, Image>();

    // 核心數據：紀錄每一條 UI 通道連接著哪兩個 Room，以及對應的通道 GameObject
    private class UIConnection
    {
        public Room roomA;
        public Room roomB;
        public GameObject lineGO;
    }
    private List<UIConnection> allConnections = new List<UIConnection>();  // 紀錄所有的 UI 通道連接資訊

    // 目標 UI 偏移量與角度
    private Vector2 targetMapPosition = Vector2.zero;

    // 打開面板時收聽廣播
    private void OnEnable()
    {
        UIEvents.OnRoomSpawned += HandleRoomSpawned;
        UIEvents.OnRoomEntered += HandlePlayerEnteredRoom;
    }

    // 關閉面板時取消收聽
    private void OnDisable()
    {
        UIEvents.OnRoomSpawned -= HandleRoomSpawned;
        UIEvents.OnRoomEntered -= HandlePlayerEnteredRoom;
    }

    private void Start()
    {
        if (Camera.main != null)
        {
            playerTransform = Camera.main.transform;
        }
        // 讀取玩家先前的設定紀錄 (預設為 0: RotateMap)
        int savedMode = PlayerPrefs.GetInt("MinimapModeSetting", (int)MinimapMode.RotateMap);
        SetMinimapMode((MinimapMode)savedMode);
        if (modeDropdown != null)
        {
            modeDropdown.value = savedMode;
            modeDropdown.RefreshShownValue(); // 刷新顯示文字
        }
    }

    private void Update()
    {
        // 1. 平滑移動 mapContent，讓當前房間推到 (0,0) 正中央
        mapContent.anchoredPosition = Vector2.Lerp(
            mapContent.anchoredPosition, 
            targetMapPosition, 
            Time.deltaTime * moveSmoothSpeed
        );

        UpdateMapRotation();
    }
    private void UpdateMapRotation()
    {
        if (playerTransform == null) return;

        float playerYRotation = playerTransform.eulerAngles.y;

        if (currentMode == MinimapMode.RotateMap)
        {
            // 【方案 A：轉地圖】
            // 1. 地圖容器往反方向旋轉
            Quaternion targetMapRot = Quaternion.Euler(0f, 0f, playerYRotation);
            mapContainer.rotation = Quaternion.Slerp(
                mapContainer.rotation, 
                targetMapRot, 
                Time.deltaTime * rotateSmoothSpeed
            );

            // 2. 玩家指針歸零，永遠朝正上方 (0 度)
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
            // 1. 地圖容器角度歸零 (正北向上)
            mapContainer.rotation = Quaternion.Slerp(
                mapContainer.rotation, 
                Quaternion.identity, 
                Time.deltaTime * rotateSmoothSpeed
            );

            // 2. 玩家指針跟隨玩家 Y 軸轉動 (UI 的 Z 軸旋轉要加負號，因為 UI 旋轉方向相反)
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
        // 儲存設定到本地檔案
        PlayerPrefs.SetInt("MinimapModeSetting", (int)newMode);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// UI Dropdown (下拉選單) 用的 Int 轉換接口
    /// </summary>
    public void SetMinimapModeFromDropdown(int modeIndex)
    {
        SetMinimapMode((MinimapMode)modeIndex);
    }

    // 收到「房間生成」時執行
    private void HandleRoomSpawned(Room newRoom, Room fromRoom, Room.Direction dir)
    {
        Vector2 newUIPos = Vector2.zero;

        if (fromRoom == null)
        {
            // 起始房間放正中央 (0,0)
            newUIPos = Vector2.zero;
        }
        else
        {
            // 根據來源房間座標 + 方向偏移，算出新房間座標
            Vector2 fromUIPos = roomUIPositions[fromRoom];
            Vector2 offset = GetDirectionOffset(dir) * roomGridDistance;
            newUIPos = fromUIPos + offset;

            // 繪製兩房間之間的通道線段
            GameObject connectionGO = DrawConnection(fromUIPos, newUIPos);
            allConnections.Add(new UIConnection { roomA = fromRoom, roomB = newRoom, lineGO = connectionGO });
            connectionGO.SetActive(false);
        }

        // 生成房間 UI Icon
        GameObject nodeGO = Instantiate(roomNodePrefab, mapContent);
        RectTransform rect = nodeGO.GetComponent<RectTransform>();
        rect.anchoredPosition = newUIPos;

        Image img = nodeGO.GetComponent<Image>();
        img.color = unvisitedRoomColor; // 初始設為未探索顏色

        // 保存映射
        roomUIPositions.Add(newRoom, newUIPos);
        roomUINodes.Add(newRoom, nodeGO);
        roomUIImages.Add(newRoom, img);

        nodeGO.SetActive(false);
    }

    // 收到「玩家切換房間」時執行
    private void HandlePlayerEnteredRoom(Room currentActiveRoom)
    {
        if (roomUIPositions.TryGetValue(currentActiveRoom, out Vector2 currentRoomUIPos))
        {
            targetMapPosition = -currentRoomUIPos;
        }

        RevealRoomAndConnectedEdges(currentActiveRoom);

        foreach (var pair in roomUIImages)
        {
            Room room = pair.Key;
            Image uiImage = pair.Value;

            if (room == currentActiveRoom)
            {
                uiImage.color = currentRoomColor;
            }
            else if (room.isVisited)
            {
                uiImage.color = visitedRoomColor;
            }
            else
            {
                uiImage.color = unvisitedRoomColor;
            }
        }
    }
    private void RevealRoomAndConnectedEdges(Room currentRoom)
    {
        // A. 先將當前房間 UI 顯示出來
        if (roomUINodes.TryGetValue(currentRoom, out GameObject currentUI))
        {
            currentUI.SetActive(true);
        }

        // B. 遍歷所有的通道紀錄，找出跟 currentRoom 連接的線段
        foreach (var conn in allConnections)
        {
            // 如果這條通道的起點或終點包含當前房間
            if (conn.roomA == currentRoom || conn.roomB == currentRoom)
            {
                // 1. 顯示這條通道線段
                if (conn.lineGO != null)
                {
                    conn.lineGO.SetActive(true);
                }

                // 2. 順藤摸瓜找到「另一端的鄰居房間」，並將其 UI 顯示出來
                Room neighborRoom = (conn.roomA == currentRoom) ? conn.roomB : conn.roomA;

                if (neighborRoom != null && roomUINodes.TryGetValue(neighborRoom, out GameObject neighborUI))
                {
                    neighborUI.SetActive(true);
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
        GameObject lineGO = Instantiate(connectionLinePrefab, mapContent);
        lineGO.transform.SetAsFirstSibling();

        RectTransform rect = lineGO.GetComponent<RectTransform>();
        Vector2 dir = (posB - posA).normalized;
        float distance = Vector2.Distance(posA, posB);

        rect.anchoredPosition = posA;
        rect.sizeDelta = new Vector2(distance, 4f);
        rect.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

        return lineGO;
    }
}
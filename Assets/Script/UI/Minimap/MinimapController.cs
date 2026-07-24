using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapController : MonoBehaviour
{
    [Header("UI 相關繫結")]
    public RectTransform mapContainer;       // 負責「旋轉」的容器 (MapContainer)
    public RectTransform mapContent;         // 負責「位移」的容器 (MapContent)
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
    private Dictionary<Room, Image> roomUIImages = new Dictionary<Room, Image>();

    // 目標 UI 偏移量與角度
    private Vector2 targetMapPosition = Vector2.zero;

    // 打開面板時收聽廣播
    private void OnEnable()
    {
        MinimapEvents.OnRoomSpawned += HandleRoomSpawned;
        MinimapEvents.OnRoomEntered += HandlePlayerEnteredRoom;
    }

    // 關閉面板時取消收聽 (防記憶體洩漏)
    private void OnDisable()
    {
        MinimapEvents.OnRoomSpawned -= HandleRoomSpawned;
        MinimapEvents.OnRoomEntered -= HandlePlayerEnteredRoom;
    }

    private void Start()
    {
        if (Camera.main != null)
        {
            playerTransform = Camera.main.transform;
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

        // 2. 隨玩家面向旋轉地圖 (地圖旋轉方向要跟玩家 Y 軸相反)
        if (playerTransform != null)
        {
            float playerYRotation = playerTransform.eulerAngles.y;
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, playerYRotation);

            mapContainer.rotation = Quaternion.Slerp(
                mapContainer.rotation, 
                targetRotation, 
                Time.deltaTime * rotateSmoothSpeed
            );
        }
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
            DrawConnection(fromUIPos, newUIPos);
        }

        // 生成房間 UI Icon
        GameObject nodeGO = Instantiate(roomNodePrefab, mapContent);
        RectTransform rect = nodeGO.GetComponent<RectTransform>();
        rect.anchoredPosition = newUIPos;

        Image img = nodeGO.GetComponent<Image>();
        img.color = unvisitedRoomColor; // 初始設為未探索顏色

        // 保存映射
        roomUIPositions.Add(newRoom, newUIPos);
        roomUIImages.Add(newRoom, img);
    }

    // 收到「玩家切換房間」時執行
    private void HandlePlayerEnteredRoom(Room currentActiveRoom)
    {
        if (roomUIPositions.TryGetValue(currentActiveRoom, out Vector2 currentRoomUIPos))
        {
            targetMapPosition = -currentRoomUIPos;
        }
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
    private void DrawConnection(Vector2 posA, Vector2 posB)
    {
        GameObject lineGO = Instantiate(connectionLinePrefab, mapContent);
        lineGO.transform.SetAsFirstSibling(); // 放到最底層，避免蓋住房間 Icon

        RectTransform rect = lineGO.GetComponent<RectTransform>();
        Vector2 dir = (posB - posA).normalized;
        float distance = Vector2.Distance(posA, posB);

        rect.anchoredPosition = posA;
        rect.sizeDelta = new Vector2(distance, 4f); // 4f 為線段粗細
        rect.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }
}
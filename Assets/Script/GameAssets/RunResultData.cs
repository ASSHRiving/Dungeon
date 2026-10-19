using System;
using UnityEngine;

[Serializable]
public class EquipmentSlotData
{
    public string slotCategory; // 例如："頭盔"、"胸甲"、"主武器"
    public string itemName;     // 例如："噬星者"、"岩峰重鎧"
    public string subInfo;      // 例如："攻擊 +42"
    public Sprite icon;         // 裝備圖示 (若無圖示可為 null)
}

[Serializable]
public class RunResultData
{
    [Header("勝負與進度")]
    public bool isVictory;
    public string stageProgress = "1 - 1"; // 例如 "1 - 3"
    public string stageName = "深淵石牢";

    [Header("統計數據")]
    public string playTimeText = "00:00:00";
    public int killCount;
    public int goldCount;

    [Header("角色最終屬性")]
    public string characterName = "戰 士";
    public float hp;
    public float def;
    public float atk;

    [Header("武器與裝備快照")]
    public EquipmentSlotData mainWeapon = new EquipmentSlotData();
    public EquipmentSlotData[] armors = new EquipmentSlotData[6];
}
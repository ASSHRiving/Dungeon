using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 單項屬性加成設定 (可用於裝備、飾品、套裝被動)
/// </summary>
[Serializable]
public struct StatBonus
{
    [Tooltip("要加成的屬性類型")]
    public StatType statType;
    [Tooltip("計算模式：Flat (固定值加算), PercentAdd (百分比加算)")]
    public StatModType modType;
    [Tooltip("加成數值 (例如 Flat 填 25 代表 +25；PercentAdd 填 0.1 代表 +10%)")]
    public float value;

    public StatBonus(StatType statType, float value, StatModType modType = StatModType.Flat)
    {
        this.statType = statType;
        this.value = value;
        this.modType = modType;
    }
}

/// <summary>
/// 裝備資料資產：定義單件裝備的名稱、部位、動態屬性清單、售價、圖標與外觀 Mesh
/// </summary>
[CreateAssetMenu(fileName = "Equip_", menuName = "Dungeon/Item/Equipment Data")]
public class EquipmentDataSO : ScriptableObject, IItem
{
    [Header("裝備識別")]
    public string equipmentName = "新裝備";
    public Equipment.EquipmentType equipmentType = Equipment.EquipmentType.Head;
    public Sprite itemIcon;
    public int price = 10;

    [Header("外觀模型 (Mesh)")]
    [Tooltip("角色穿戴在身上時替換的 SkinnedMesh")]
    public Mesh characterMesh;
    [Tooltip("掉落於地面或展示在商店時的靜態網格")]
    public Mesh prefabMesh;

    [Header("屬性加成清單 (動態配置，隨插即用)")]
    [Tooltip("在此自由新增屬性加成項目，不需在代碼寫死任何變數")]
    public List<StatBonus> statBonuses = new List<StatBonus>();

    #region 向下相容屬性 (方便現有 UI 與舊代碼讀取)
    public float damage => GetStatBonusValue(StatType.AttackDamage);
    public float shield => GetStatBonusValue(StatType.Shield);
    public float health => GetStatBonusValue(StatType.MaxHealth);
    public float critRate => GetStatBonusValue(StatType.CritRate);

    public float GetStatBonusValue(StatType type)
    {
        if (statBonuses == null) return 0f;
        float total = 0f;
        for (int i = 0; i < statBonuses.Count; i++)
        {
            if (statBonuses[i].statType == type)
            {
                total += statBonuses[i].value;
            }
        }
        return total;
    }
    #endregion

    // IItem 介面實作
    public string ItemName => equipmentName;
    public Sprite ItemIcon => itemIcon;

    public string GetTooltipStatsText()
    {
        if (statBonuses == null || statBonuses.Count == 0)
            return "無附加屬性";

        System.Text.StringBuilder sb = new System.Text.StringBuilder(64);
        for (int i = 0; i < statBonuses.Count; i++)
        {
            var bonus = statBonuses[i];
            if (Mathf.Approximately(bonus.value, 0f)) continue;

            string name = GetStatDisplayName(bonus.statType);
            string sign = bonus.value > 0 ? "+" : "";
            string valStr;

            if (bonus.statType == StatType.CritRate)
            {
                valStr = $"{bonus.value:0.#}%";
            }
            else if (bonus.statType == StatType.CritDamage || bonus.statType == StatType.HitStunReduction)
            {
                // 儲存為 0.15 代表 +15%
                valStr = $"{bonus.value * 100f:0.#}%";
            }
            else if (bonus.modType == StatModType.PercentAdd || bonus.modType == StatModType.PercentMult)
            {
                valStr = $"{bonus.value * 100f:0.#}%";
            }
            else
            {
                // Flat 固定值：整數顯示整數，小數 (如 MoveSpeed +0.5) 顯示小數
                valStr = Mathf.Approximately(bonus.value, Mathf.Round(bonus.value))
                    ? Mathf.RoundToInt(bonus.value).ToString()
                    : bonus.value.ToString("0.0");
            }

            sb.AppendLine($"{name}: {sign}{valStr}");
        }

        return sb.Length == 0 ? "無附加屬性" : sb.ToString().TrimEnd();
    }

    private string GetStatDisplayName(StatType type)
    {
        switch (type)
        {
            case StatType.MaxHealth: return "最大生命";
            case StatType.Shield: return "護甲";
            case StatType.AttackDamage: return "攻擊力";
            case StatType.CritRate: return "暴擊率";
            case StatType.MoveSpeed: return "移動速度";
            case StatType.Poise: return "韌性";
            case StatType.CritDamage: return "暴擊傷害";
            case StatType.LifeOnKill: return "擊殺回血";
            case StatType.HitStunReduction: return "縮短受擊硬直";
            default: return type.ToString();
        }
    }
}

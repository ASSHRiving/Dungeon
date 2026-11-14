using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 武器規格資料資產 (ScriptableObject)
/// 定義武器的名稱、圖示、售價、動態屬性加成 (攻擊、移速、暴擊等)、
/// 動畫覆蓋控制器 (AnimatorOverrideController)、打擊手感與打擊反饋
/// </summary>
[CreateAssetMenu(fileName = "Weapon_", menuName = "Dungeon/Item/Weapon Data")]
public class WeaponDataSO : ScriptableObject, IItem
{
    [Header("武器識別與基本設定")]
    public string weaponName = "新武器";
    public Sprite itemIcon;
    public int price = 30;
    [TextArea(2, 4)]
    public string description = "";

    [Header("屬性加成清單 (動態配置，隨插即用)")]
    [Tooltip("在此自由配置 StatModifier 加成，例如 AttackDamage: Flat 30, MoveSpeed: Flat +3 或 -2 等")]
    public List<StatBonus> statBonuses = new List<StatBonus>();

    [Header("戰鬥手感與動畫規格")]
    [Tooltip("對應的動作覆蓋控制器 (例如 OneHandOverride / TwoHandOverride)")]
    public AnimatorOverrideController overrideController;
    [Tooltip("武器種類編號 (0: 單手劍, 1: 雙手大劍, 2: 太刀等)")]
    public int weaponType = 0;
    public SoundAssetsType weaponSoundType = SoundAssetsType.Sword;
    [Tooltip("命中敵人的微頓幀時間 (秒)")]
    public float hitStopDuration = 0.045f;
    [Tooltip("命中特效 Prefab (例如 HolyHitImpact)")]
    public GameObject hitImpactPrefab;

    // IItem 介面實作
    public string ItemName => weaponName;
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
                valStr = $"{bonus.value * 100f:0.#}%";
            }
            else if (bonus.modType == StatModType.PercentAdd || bonus.modType == StatModType.PercentMult)
            {
                valStr = $"{bonus.value * 100f:0.#}%";
            }
            else
            {
                // Flat 固定值
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

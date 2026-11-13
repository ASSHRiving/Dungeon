using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

/// <summary>
/// 單項屬性容器：負責封裝基礎數值與所有掛載的修正器 (Modifiers)
/// 採用 Dirty Flag 機制在數值變更時才重新計算，高效且精準。
/// 計算公式：最終值 = (基礎值 + 總固定加值) × (1 + 總百分比加算) × (總百分比乘算)
/// </summary>
[Serializable]
public class CharacterStat
{
    [SerializeField] private float baseValue;
    private bool isDirty = true;
    private float cachedValue;

    private readonly List<StatModifier> statModifiers = new List<StatModifier>();
    public ReadOnlyCollection<StatModifier> StatModifiers => statModifiers.AsReadOnly();

    public float BaseValue
    {
        get => baseValue;
        set
        {
            if (Mathf.Approximately(baseValue, value)) return;
            baseValue = value;
            isDirty = true;
        }
    }

    public float Value
    {
        get
        {
            if (isDirty)
            {
                cachedValue = CalculateFinalValue();
                isDirty = false;
            }
            return cachedValue;
        }
    }

    public CharacterStat(float baseValue = 0f)
    {
        this.baseValue = baseValue;
    }

    public void AddModifier(StatModifier mod)
    {
        isDirty = true;
        statModifiers.Add(mod);
        statModifiers.Sort(CompareModifierOrder);
    }

    public bool RemoveModifier(StatModifier mod)
    {
        if (statModifiers.Remove(mod))
        {
            isDirty = true;
            return true;
        }
        return false;
    }

    public bool RemoveAllModifiersFromSource(object source)
    {
        int removed = statModifiers.RemoveAll(mod => mod.source == source);
        if (removed > 0)
        {
            isDirty = true;
            return true;
        }
        return false;
    }

    public void ClearModifiers()
    {
        if (statModifiers.Count > 0)
        {
            statModifiers.Clear();
            isDirty = true;
        }
    }

    private int CompareModifierOrder(StatModifier a, StatModifier b)
    {
        return a.order.CompareTo(b.order);
    }

    private float CalculateFinalValue()
    {
        float finalValue = baseValue;
        float sumPercentAdd = 0f;

        for (int i = 0; i < statModifiers.Count; i++)
        {
            StatModifier mod = statModifiers[i];

            if (mod.modType == StatModType.Flat)
            {
                finalValue += mod.value;
            }
            else if (mod.modType == StatModType.PercentAdd)
            {
                sumPercentAdd += mod.value;

                // 若這是最後一個 modifier，或者下一個 modifier 不是 PercentAdd，結算累計的加算百分比
                if (i + 1 >= statModifiers.Count || statModifiers[i + 1].modType != StatModType.PercentAdd)
                {
                    finalValue *= 1f + sumPercentAdd;
                    sumPercentAdd = 0f;
                }
            }
            else if (mod.modType == StatModType.PercentMult)
            {
                finalValue *= 1f + mod.value;
            }
        }

        return finalValue;
    }
}

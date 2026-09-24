using System;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Transition", menuName = "StateMachine/Transition")]
public class TransitionSO : ScriptableObject
{
    [Serializable]
    private class StateConfig
    {
        public StateActionSO fromState;
        public StateActionSO toState;
        public List<ConditionSO> conditions;
    }

    [SerializeField, Header("轉移資料")] private List<StateConfig> stateConfigDatas = new List<StateConfig>();



    // 【核心改動】直接傳入「目前正在執行 Tick 的那隻怪物的狀態機」
    public void TryGetApplyCondition(StateMachineSystem currentSystem)
    {
        int conditionPriority = 0;
        int statePriority = 0;
        List<StateActionSO> toStates = new List<StateActionSO>();
        StateActionSO toState = null;

        // 直接用過濾後的列表來跑，不用轉成字典（因為 SO 只有一份，轉字典會互相覆蓋）
        // 對於中小型遊戲，一隻怪一幀跑這幾個 Config 對 CPU 來說完全沒感覺
        foreach (var stateItem in stateConfigDatas)
        {
            // 精準判斷：這筆設定的起始狀態，是不是「目前這隻怪」的當前狀態
            if (stateItem.fromState == currentSystem.currrentState)
            {
                foreach (var condition in stateItem.conditions)
                {
                    // 傳入正確的狀態機給條件腳本
                    if (condition.ConditionSetUp(currentSystem))
                    {
                        if (condition.GetPriority() >= conditionPriority)
                        {
                            conditionPriority = condition.GetPriority();
                            toStates.Add(stateItem.toState);
                        }
                    }
                }
            }
        }

        // 選出符合條件的最高優先級狀態
        if (toStates.Count != 0)
        {
            foreach (var state in toStates)
            {
                if (state.GetPriority() >= statePriority)
                {
                    statePriority = state.GetPriority();
                    toState = state;
                }
            }
        }

        // 完成轉換
        if (toState != null)
        {
            // 精準修改「當前這隻怪」的狀態，再也不會改到別人！
            currentSystem.currrentState?.OnExit(currentSystem);
            
            currentSystem.currrentState = toState;
            
            currentSystem.currrentState?.OnEnter(currentSystem); // 這裡絕對會順利播動畫！
            
            toStates.Clear();
            conditionPriority = 0;
            statePriority = 0;
            toState = null;
        }
    }
}

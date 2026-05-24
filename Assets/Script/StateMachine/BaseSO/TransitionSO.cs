using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.AdaptivePerformance;

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

    private Dictionary<StateActionSO, List<StateConfig>> states = new Dictionary<StateActionSO, List<StateConfig>>();
    [SerializeField, Header("轉移資料")] private List<StateConfig> stateConfigDatas = new List<StateConfig>();

    private StateMachineSystem stateMachineSystem;

    public void Init(StateMachineSystem stateMachineSystem)
    {
        this.stateMachineSystem = stateMachineSystem;
        SaveAllStateConfig();
    }

    //初始化states字典 拆解stateConfigDatas
    private void SaveAllStateConfig()
    {
        foreach(var item in stateConfigDatas)  //item是每一筆狀態轉移數據
        {
            if (!states.ContainsKey(item.fromState))  //如果目前沒有該狀態的資料 就新建陣列，否則在原本的key對應的陣列add
            {
                states.Add(item.fromState, new List<StateConfig>());
                states[item.fromState].Add(item);
            }
            else
            {
                states[item.fromState].Add(item);
            }
            foreach(var condition in item.conditions)
            {
                condition.Init(stateMachineSystem);
            }
        }
    }

    //嘗試轉移狀態
    public void TryGetApplyCondition()
    {
        int conditionPriority = 0;
        int statePriority = 0;
        List<StateActionSO> toStates = new List<StateActionSO>();
        StateActionSO toState = null;

        //找出目前狀態的轉移資料並檢查是否達成轉換條件，如符合條件且優先及最高就把toState加進候選名單
        if (states.ContainsKey(stateMachineSystem.currrentState))
        {
            foreach(var stateItem in states[stateMachineSystem.currrentState])
            {
                foreach(var condition in stateItem.conditions)
                {
                    if (condition.ConditionSetUp())
                    {
                        if(condition.GetPriority() >= conditionPriority)
                        {
                            conditionPriority = condition.GetPriority();
                            toStates.Add(stateItem.toState);
                        }
                    }
                }
            }
        }
        else
        {
            return;
        }

        //選出符合條件的最高優先及狀態
        if(toStates.Count != 0 || toStates != null)
        {
            foreach(var state in toStates)
            {
                if(state.GetPriority() > statePriority)
                {
                    statePriority = state.GetPriority();
                    toState = state;
                }
            }
        }

        //完成轉換
        if(toState != null)
        {
            stateMachineSystem.currrentState.OnExit();
            stateMachineSystem.currrentState = toState;
            stateMachineSystem.currrentState.OnEnter(this.stateMachineSystem);
            toStates.Clear();
            conditionPriority = 0;
            statePriority = 0;
            toState = null;
        }
    }
}

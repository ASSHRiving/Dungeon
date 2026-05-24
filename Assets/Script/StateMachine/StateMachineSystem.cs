using UnityEngine;

public class StateMachineSystem : MonoBehaviour
{
    [Header("使用的轉換器腳本")]public TransitionSO transition;
    [Header("目前狀態")]public StateActionSO currrentState;

    private void Awake()
    {
        transition?.Init(this);
        currrentState?.OnEnter(this);
    }
    private void Update()
    {
        StateMachineTick();
    }
    private void StateMachineTick()
    {
        transition?.TryGetApplyCondition();
        currrentState.OnUpdate();
    }

}

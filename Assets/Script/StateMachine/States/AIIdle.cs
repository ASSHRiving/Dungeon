using UnityEngine;

[CreateAssetMenu(fileName = "AIIdle", menuName ="StateMachine/States/AIIdle")]
public class AIIdle : StateActionSO
{
    public override void OnUpdate()
    {
        Debug.Log("Idle State");
    }
}

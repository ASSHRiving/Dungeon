using UnityEngine;

[CreateAssetMenu(fileName = "AICombat", menuName ="StateMachine/States/AICombat")]
public class AICombat : StateActionSO
{
    public override void OnUpdate()
    {
        Debug.Log("Combat State");
    }
}

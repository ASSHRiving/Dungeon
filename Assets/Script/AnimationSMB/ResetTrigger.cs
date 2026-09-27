using UnityEngine;

public class ResetTrigger : StateMachineBehaviour
{
    [SerializeField] private string[] triggerNamesToReset = { "Roll"};

    // 在狀態機進入這個狀態時被呼叫
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        foreach (string triggerName in triggerNamesToReset)
        {
            animator.ResetTrigger(triggerName);
        }
    }
}

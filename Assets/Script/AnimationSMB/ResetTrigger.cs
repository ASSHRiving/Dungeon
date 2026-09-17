using UnityEngine;

public class ResetTrigger : StateMachineBehaviour
{
    public string[] triggers;

    // 在狀態機進入這個狀態時被呼叫
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        foreach (var info in triggers)
        {
            animator.ResetTrigger(info);
        }
    }
}

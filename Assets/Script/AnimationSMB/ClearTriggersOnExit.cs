using UnityEngine;

public class ClearTriggersOnExit : StateMachineBehaviour
{
    // 需要被重置的 Trigger 名稱列表
    [SerializeField] private string[] triggerNamesToReset = { "Roll", "LAtk"};

    // 當離開受傷動畫狀態時觸發
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        foreach (string triggerName in triggerNamesToReset)
        {
            animator.ResetTrigger(triggerName);
        }
    }
}
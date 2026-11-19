using UnityEngine;

/// <summary>
/// 物件互動教學：進場立即顯示卡片引導玩家前往門／寶箱互動，完成互動後推進。
/// </summary>
[CreateAssetMenu(fileName = "Step_Interact", menuName = "Dungeon/Tutorial/Steps/Interact Step")]
public class InteractStepSO : TutorialStepSO
{
    protected override void OnEnter()
    {
        ShowCard();
        UIEvents.OnInteractCompleted -= HandleInteractCompleted;
        UIEvents.OnInteractCompleted += HandleInteractCompleted;
    }

    protected override void OnExit() => UIEvents.OnInteractCompleted -= HandleInteractCompleted;
    private void OnDisable() => UIEvents.OnInteractCompleted -= HandleInteractCompleted;

    private void HandleInteractCompleted(IInteractable interactable) => AddProgress(1f);
}

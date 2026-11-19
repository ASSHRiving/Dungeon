using UnityEngine;

/// <summary>
/// 第 7 步（商店購買教學）：進入商店房時顯示卡片，與商店展示桌互動後完成。
/// </summary>
[CreateAssetMenu(fileName = "Step_ShopInteract", menuName = "Dungeon/Tutorial/Steps/Shop Interact Step")]
public class ShopInteractStepSO : TutorialStepSO
{
    protected override void OnEnter()
    {
        UIEvents.OnRoomEntered -= HandleRoomEntered;
        UIEvents.OnRoomEntered += HandleRoomEntered;
        UIEvents.OnInteractCompleted -= HandleInteractCompleted;
        UIEvents.OnInteractCompleted += HandleInteractCompleted;
    }

    protected override void OnExit()
    {
        UIEvents.OnRoomEntered -= HandleRoomEntered;
        UIEvents.OnInteractCompleted -= HandleInteractCompleted;
    }

    private void OnDisable()
    {
        UIEvents.OnRoomEntered -= HandleRoomEntered;
        UIEvents.OnInteractCompleted -= HandleInteractCompleted;
    }

    private void HandleRoomEntered(Room room)
    {
        if (room is ShopRoom)
        {
            ShowCard();
        }
    }

    private void HandleInteractCompleted(IInteractable interactable)
    {
        if (interactable is ShopTableInteract)
        {
            AddProgress(1f);
        }
    }
}

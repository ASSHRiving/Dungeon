using UnityEngine;

/// <summary>
/// 第 1 步（移動教學）：進場時鎖住出生房大門並立即顯示卡片，累積移動時間後完成。
/// </summary>
[CreateAssetMenu(fileName = "Step_Move", menuName = "Dungeon/Tutorial/Steps/Move Step")]
public class MoveStepSO : TutorialStepSO
{
    protected override void OnEnter()
    {
        GameEvent.StartRoomDoorLockChanged(true);
        ShowCard();
    }

    public override void OnUpdate()
    {
        if (Manager == null || Manager.IsSuppressed || Manager.CharacterInput == null)
        {
            return;
        }

        if (Manager.CharacterInput.playerMovement.sqrMagnitude > 0.01f)
        {
            AddProgress(Time.deltaTime);
        }
    }
}

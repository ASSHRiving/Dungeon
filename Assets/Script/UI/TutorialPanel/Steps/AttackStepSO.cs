using UnityEngine;

/// <summary>
/// 第 3 步（普通攻擊教學）：完成或略過退場時解鎖出生房大門。
/// </summary>
[CreateAssetMenu(fileName = "Step_Attack", menuName = "Dungeon/Tutorial/Steps/Attack Step")]
public class AttackStepSO : TutorialStepSO
{
    protected override void OnEnter()
    {
        ShowCard();
        GameEvent.OnPlayerAttacked -= HandlePlayerAttacked;
        GameEvent.OnPlayerAttacked += HandlePlayerAttacked;
    }

    protected override void OnExit()
    {
        GameEvent.OnPlayerAttacked -= HandlePlayerAttacked;
        GameEvent.StartRoomDoorLockChanged(false);
    }

    private void OnDisable() => GameEvent.OnPlayerAttacked -= HandlePlayerAttacked;

    private void HandlePlayerAttacked() => AddProgress(1f);
}

using UnityEngine;

/// <summary>
/// 第 6 步（擊殺敵人教學）：緊接在完美閃避後顯示卡片，清空該戰鬥房內所有敵人（OnRoomCleared）後完成。
/// </summary>
[CreateAssetMenu(fileName = "Step_KillEnemy", menuName = "Dungeon/Tutorial/Steps/Kill Enemy Step")]
public class KillEnemyStepSO : TutorialStepSO
{
    protected override void OnEnter()
    {
        ShowCard();
        GameEvent.OnFightingRoomCleared -= HandleFightingRoomCleared;
        GameEvent.OnFightingRoomCleared += HandleFightingRoomCleared;
    }

    protected override void OnExit() => GameEvent.OnFightingRoomCleared -= HandleFightingRoomCleared;
    private void OnDisable() => GameEvent.OnFightingRoomCleared -= HandleFightingRoomCleared;

    private void HandleFightingRoomCleared() => AddProgress(1f);
}

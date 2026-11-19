using UnityEngine;

/// <summary>
/// 第 2 步（奔跑教學）：移動時按住奔跑鍵累積時間後完成。
/// </summary>
[CreateAssetMenu(fileName = "Step_Run", menuName = "Dungeon/Tutorial/Steps/Run Step")]
public class RunStepSO : TutorialStepSO
{
    protected override void OnEnter() => ShowCard();

    public override void OnUpdate()
    {
        if (Manager == null || Manager.IsSuppressed || Manager.CharacterInput == null)
        {
            return;
        }

        if (Manager.CharacterInput.playerMovement.sqrMagnitude > 0.01f && Manager.CharacterInput.playerRun)
        {
            AddProgress(Time.deltaTime);
        }
    }
}

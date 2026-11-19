using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 互動式新手教學單一清單狀態機總管。
/// 所有教學步驟皆放在同一份 <see cref="TutorialSequenceSO"/> 清單中：
/// - 未勾選 <see cref="TutorialStepSO.canTriggerAnytime"/> 的步驟依清單順序逐一啟動；
/// - 勾選的步驟則於背景同步待命，條件滿足即可搶先跳出顯示。
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("教學資料與 UI 參照")]
    [SerializeField] private TutorialSequenceSO tutorialSequence;
    [SerializeField] private TutorialHUDController hudController;
    [SerializeField] private UIController uiController;
    [SerializeField] private InputActionAsset fallbackInputActions;

    [Header("略過與測試")]
    [SerializeField] private float skipHoldDuration = 0.8f;
    [SerializeField] private bool forceReplayOnStart = false;

    private readonly List<TutorialStepSO> activeSteps = new List<TutorialStepSO>();

    private TutorialStepSO displayedStep;
    private bool isCompletingStep;
    private bool waitingForPlayerSpawn;
    private float skipHoldTimer;
    private bool waitForSkipKeyRelease;

    public Transform PlayerTransform { get; private set; }
    public PlayerInput PlayerInput { get; private set; }
    public CharacterInputSystem CharacterInput { get; private set; }
    public bool IsSuppressed { get; private set; }

    public static void ResetTutorialSavedState() => TutorialStepSO.ResetAllSavedStates();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (uiController == null) uiController = FindFirstObjectByType<UIController>();
        if (hudController == null) hudController = GetComponentInChildren<TutorialHUDController>(true);
        hudController?.SetSkipClickAction(SkipCurrentStep);
    }

    private void OnEnable()
    {
        GameEvent.OnPlayerSpawn += HandlePlayerSpawned;
        UIEvents.OnBindingsChanged += HandleBindingsChanged;
    }

    private void OnDisable()
    {
        ExitAllActiveSteps();
        GameEvent.StartRoomDoorLockChanged(false);
        GameEvent.PlayerAttackLockChanged(false);
        GameEvent.OnPlayerSpawn -= HandlePlayerSpawned;
        UIEvents.OnBindingsChanged -= HandleBindingsChanged;
    }

    private void OnDestroy()
    {
        ExitAllActiveSteps();
        if (Instance == this) Instance = null;
    }

    private void Start() => StartTutorialFlow(resetSaved: forceReplayOnStart);

    private void Update()
    {
        if (activeSteps.Count == 0 && !isCompletingStep) return;

        bool suppressed = ShouldSuppressTutorial();
        if (suppressed != IsSuppressed)
        {
            IsSuppressed = suppressed;
            hudController?.SetSuppressed(suppressed);
        }

        for (int i = activeSteps.Count - 1; i >= 0; i--)
        {
            if (i < activeSteps.Count) activeSteps[i]?.OnUpdate();
        }

        if (IsSuppressed) { ResetSkipHold(); return; }
        UpdateSkipHoldInput();
    }

    public void RestartTutorial()
    {
        ExitAllActiveSteps();
        isCompletingStep = false;
        ResetSkipHold();
        waitForSkipKeyRelease = false;
        StartTutorialFlow(resetSaved: true);
    }

    public void SkipTutorial() => SkipCurrentStep();

    /// <summary>
    /// 僅略過當前正在 HUD 上顯示的步驟，並推進後續符合條件的步驟。
    /// </summary>
    public void SkipCurrentStep()
    {
        if (displayedStep == null || isCompletingStep) return;

        GameAssets.Instance?.PlayUIClickSound();
        ResetSkipHold();
        waitForSkipKeyRelease = true;

        CompleteAndAdvance(displayedStep, playAnimation: false);
    }

    public bool TryClaimCardDisplay(TutorialStepSO step)
    {
        if (step == null || isCompletingStep || displayedStep != null || !activeSteps.Contains(step))
            return false;

        displayedStep = step;
        return true;
    }

    public void ShowStepCard(TutorialStepSO step)
    {
        if (step == null || step != displayedStep) return;
        string desc = TutorialTextFormatter.FormatStepDescription(step, PlayerInput, fallbackInputActions);
        hudController?.ShowStep(step, desc, step.CurrentProgress);
    }

    public void OnStepProgressChanged(TutorialStepSO step)
    {
        if (step == null || step != displayedStep || isCompletingStep) return;

        hudController?.UpdateProgress(step, step.CurrentProgress);
        if (!step.IsCompleted) return;

        isCompletingStep = true;
        ResetSkipHold();
        CompleteAndAdvance(step, playAnimation: true);
    }

    private void StartTutorialFlow(bool resetSaved)
    {
        if (resetSaved) ResetTutorialSavedState();
        TryBindExistingPlayer();

        IsSuppressed = ShouldSuppressTutorial();
        hudController?.SetSuppressed(IsSuppressed);
        hudController?.HideImmediate();

        if (tutorialSequence == null || tutorialSequence.StepCount == 0)
        {
            waitingForPlayerSpawn = false;
            return;
        }

        if (PlayerTransform == null)
        {
            waitingForPlayerSpawn = true;
            return;
        }

        waitingForPlayerSpawn = false;
        RefreshActiveSteps();
    }

    private void RefreshActiveSteps()
    {
        if (tutorialSequence == null || tutorialSequence.steps == null) return;

        bool enteredNextSequential = false;
        for (int i = 0; i < tutorialSequence.steps.Count; i++)
        {
            TutorialStepSO step = tutorialSequence.steps[i];
            if (step == null || step.IsSavedCompleted) continue;

            bool canEnter = step.canTriggerAnytime || !enteredNextSequential;
            if (!step.canTriggerAnytime && !enteredNextSequential) enteredNextSequential = true;

            if (canEnter && !activeSteps.Contains(step))
            {
                activeSteps.Add(step);
                step.EnterStep(this);
            }
        }
    }

    private void CompleteAndAdvance(TutorialStepSO step, bool playAnimation)
    {
        step.ExitStep();
        step.MarkSavedCompleted();
        activeSteps.Remove(step);
        displayedStep = null;

        if (playAnimation)
        {
            if (hudController != null) hudController.PlayStepCompleted(step, OnCompletionAnimationFinished);
            else OnCompletionAnimationFinished();
            return;
        }

        RefreshActiveSteps();
        if (displayedStep == null) hudController?.PlayHide();
    }

    private void OnCompletionAnimationFinished()
    {
        isCompletingStep = false;
        RefreshActiveSteps();
    }

    private void ExitAllActiveSteps()
    {
        for (int i = 0; i < activeSteps.Count; i++)
            activeSteps[i]?.ExitStep();
        activeSteps.Clear();
        displayedStep = null;
    }

    private void TryBindExistingPlayer()
    {
        if (PlayerTransform != null) return;
        GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null) BindPlayer(playerGo.transform);
        else if (uiController != null && uiController.GetPlayerInput() != null) BindPlayer(uiController.GetPlayerInput().transform);
    }

    private void BindPlayer(Transform player)
    {
        PlayerTransform = player;
        PlayerInput = player != null ? player.GetComponent<PlayerInput>() : null;
        CharacterInput = player != null ? player.GetComponent<CharacterInputSystem>() : null;
    }

    private bool ShouldSuppressTutorial()
    {
        if (uiController != null && uiController.HasOpenPanels) return true;
        return PlayerInput != null && PlayerInput.currentActionMap != null && PlayerInput.currentActionMap.name != "Player";
    }

    private void UpdateSkipHoldInput()
    {
        if (displayedStep == null || isCompletingStep) { ResetSkipHold(); return; }

        bool holdingTab = Keyboard.current != null && Keyboard.current.tabKey.isPressed;
        if (waitForSkipKeyRelease) { if (!holdingTab) waitForSkipKeyRelease = false; return; }

        if (holdingTab)
        {
            skipHoldTimer += Time.unscaledDeltaTime;
            hudController?.SetSkipHoldProgress(skipHoldDuration > 0.01f ? skipHoldTimer / skipHoldDuration : 1f);
            if (skipHoldTimer >= skipHoldDuration) { skipHoldTimer = 0f; SkipCurrentStep(); }
        }
        else if (skipHoldTimer > 0f) ResetSkipHold();
    }

    private void ResetSkipHold() { skipHoldTimer = 0f; hudController?.SetSkipHoldProgress(0f); }

    private void HandlePlayerSpawned(Transform player)
    {
        BindPlayer(player);
        if (waitingForPlayerSpawn)
        {
            waitingForPlayerSpawn = false;
            RefreshActiveSteps();
        }
    }

    private void HandleBindingsChanged()
    {
        if (displayedStep != null)
            hudController?.RefreshDescription(TutorialTextFormatter.FormatStepDescription(displayedStep, PlayerInput, fallbackInputActions));
    }

#if UNITY_EDITOR
    [ContextMenu("重置新手教學存檔")]
    private void EditorResetTutorialSavedState() { ResetTutorialSavedState(); Debug.Log("[TutorialManager] 已清除新手教學存檔。"); }

    [ContextMenu("立即強制重播教學")]
    private void EditorRestartTutorialNow() { if (Application.isPlaying) RestartTutorial(); else EditorResetTutorialSavedState(); }
#endif
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 互動式新手教學流程與條件偵測總管。
/// 管理首次遊玩存檔、步驟狀態機、玩家輸入／情境事件追蹤、戰鬥閃避子彈時間、彈性推進、彈窗避讓與略過重置。
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public const string TutorialCompletedPrefKey = "Tutorial_Completed";
    private const float DefaultFixedDeltaTime = 0.02f;

    public enum TutorialState
    {
        Inactive,
        WaitingForTrigger,
        ActiveStep,
        CompletingStep,
        Completed
    }

    public static TutorialManager Instance { get; private set; }

    [Header("教學資料與 UI 參照")]
    [SerializeField] private TutorialSequenceSO tutorialSequence;
    [SerializeField] private TutorialHUDController hudController;
    [SerializeField] private UIController uiController;
    [SerializeField] private InputActionAsset fallbackInputActions;

    [Header("獨立情境任務（各自用 stepId 存檔，不影響主線）")]
    [SerializeField] private List<TutorialStepSO> independentSteps = new List<TutorialStepSO>();

    [Header("閃避子彈時間設定")]
    [SerializeField] private float dodgeTriggerDistance = 4.5f;
    [SerializeField] private float dodgeBulletTimeDelay = 0.1f;
    [SerializeField] private float dodgeBulletTimeScale = 0.08f;
    [SerializeField] private float maxBulletTimeRealDuration = 4.0f;

    [Header("略過設定")]
    [SerializeField] private float skipHoldDuration = 0.8f;

    [Header("測試與除錯")]
    [Tooltip("勾選後在進入關卡時無視已完成紀錄，強制重播教學（測試用）")]
    [SerializeField] private bool forceReplayOnStart = false;

    private TutorialState state = TutorialState.Inactive;
    private int currentStepIndex = 0;
    private float currentProgress = 0f;
    private float skipHoldTimer = 0f;

    // 玩家與環境追蹤參照
    private Transform playerTransform;
    private PlayerInput playerInput;
    private CharacterInputSystem characterInput;

    // 情境事件狀態快取
    private bool isPlayerSpawned = false;
    private bool hasEnteredFightingRoom = false;
    private FightingRoom currentFightingRoom;
    private int killsInCurrentFightingRoom = 0;
    private string currentDetectedInteractable = string.Empty;
    private bool lastSuppressedState = false;

    // 獨立任務狀態
    private TutorialStepSO activeIndependentStep;
    private bool isPlayingIndependentComplete = false;

    // 戰鬥閃避子彈時間狀態
    private bool hasCompletedDodgeStep = false;
    private bool isDodgeBulletTimeActive = false;
    private float bulletTimeRealTimer = 0f;
    private int resumeStepIndexAfterDodge = -1;
    private Transform pendingAttacker;
    private float pendingAttackDelayTimer = 0f;

    // 暫存 StartRoom 門的原始圖層，供鎖定/解鎖切換 UnInteractable
    private readonly Dictionary<GameObject, int> cachedStartRoomDoorLayers = new Dictionary<GameObject, int>();

    public TutorialState CurrentState => state;

    /// <summary>
    /// 清除本地新手教學完成標記（供設定頁與測試工具呼叫）。
    /// </summary>
    public static void ResetTutorialSavedState()
    {
        PlayerPrefs.DeleteKey(TutorialCompletedPrefKey);
        PlayerPrefs.Save();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (uiController == null)
        {
            uiController = FindFirstObjectByType<UIController>();
        }
        if (hudController == null)
        {
            hudController = GetComponentInChildren<TutorialHUDController>(true);
        }
        if (hudController != null)
        {
            hudController.SetSkipClickAction(SkipTutorial);
        }
    }

    private void OnEnable()
    {
        GameEvent.OnPlayerSpawn += HandlePlayerSpawned;
        UIEvents.OnRoomEntered += HandleRoomEntered;
        UIEvents.OnInteractableDetected += HandleInteractableDetected;
        UIEvents.OnInteractCompleted += HandleInteractCompleted;
        UIEvents.OnEnemyKilled += HandleEnemyKilled;
        UIEvents.OnBindingsChanged += HandleBindingsChanged;
        UIEvents.OnPlayerAttacked += HandlePlayerAttacked;
        UIEvents.OnPlayerRolled += HandlePlayerRolled;
        UIEvents.OnEnemyAttackStarted += HandleEnemyAttackStarted;
        UIEvents.OnPlayerPerfectDodge += HandlePlayerPerfectDodge;
    }

    private void OnDisable()
    {
        ClearPendingAttacker();
        ExitDodgeBulletTime(restoreNormalTimeScale: true);
        SetStartRoomDoorsLocked(false);

        GameEvent.OnPlayerSpawn -= HandlePlayerSpawned;
        UIEvents.OnRoomEntered -= HandleRoomEntered;
        UIEvents.OnInteractableDetected -= HandleInteractableDetected;
        UIEvents.OnInteractCompleted -= HandleInteractCompleted;
        UIEvents.OnEnemyKilled -= HandleEnemyKilled;
        UIEvents.OnBindingsChanged -= HandleBindingsChanged;
        UIEvents.OnPlayerAttacked -= HandlePlayerAttacked;
        UIEvents.OnPlayerRolled -= HandlePlayerRolled;
        UIEvents.OnEnemyAttackStarted -= HandleEnemyAttackStarted;
        UIEvents.OnPlayerPerfectDodge -= HandlePlayerPerfectDodge;
    }

    private void OnDestroy()
    {
        ExitDodgeBulletTime(restoreNormalTimeScale: true);

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        InitializeTutorialOnSceneStart();
    }

    private void Update()
    {
        bool hasMainWork = state != TutorialState.Inactive && state != TutorialState.Completed;
        bool hasSideWork = activeIndependentStep != null;
        if (!hasMainWork && !hasSideWork)
        {
            return;
        }

        // 彈窗避讓同步（設定、角色、暫停選單開啟時淡出隱藏並暫停偵測）
        bool shouldSuppress = ShouldSuppressTutorial();
        if (shouldSuppress != lastSuppressedState)
        {
            lastSuppressedState = shouldSuppress;
            hudController?.SetSuppressed(shouldSuppress);

            if (shouldSuppress && isDodgeBulletTimeActive)
            {
                // 交還時間控制權給 UIController（Esc / 設定面板會將 timeScale 設為 0）
                ExitDodgeBulletTime(restoreNormalTimeScale: false);
            }
        }

        if (shouldSuppress)
        {
            ClearPendingAttacker();
            ResetSkipHold();
            return;
        }

        // 敵人出手後延遲約 0.1s 切入子彈時間，讓玩家先看清出招動作
        UpdatePendingEnemyAttack();

        // 若正處於閃避子彈時間中，倒數真實秒數防卡死
        UpdateDodgeBulletTimeTimeout();

        // 長按 Tab 略過教學（支援游標鎖定期間）
        UpdateSkipHoldInput();

        if (state != TutorialState.ActiveStep)
        {
            return;
        }

        UpdateActiveStepCondition();
    }

    /// <summary>
    /// 供設定頁或測試工具於遊戲中重啟互動式教學。
    /// </summary>
    public void RestartTutorial()
    {
        ExitDodgeBulletTime(restoreNormalTimeScale: true);
        ResetTutorialSavedState();
        ResetIndependentStepsSavedState();
        ResetRuntimeTracking();
        TryBindExistingPlayer();

        if (tutorialSequence == null || tutorialSequence.StepCount == 0)
        {
            state = TutorialState.Inactive;
            hudController?.HideImmediate();
            return;
        }

        currentStepIndex = 0;
        state = TutorialState.WaitingForTrigger;

        lastSuppressedState = ShouldSuppressTutorial();
        hudController?.SetSuppressed(lastSuppressedState);

        UpdateStartRoomDoorLock();
        TryActivateCurrentStepIfReady();
    }

    /// <summary>
    /// 略過剩餘教學步驟並標記為已完成。
    /// </summary>
    public void SkipTutorial()
    {
        bool hasMainWork = state != TutorialState.Inactive && state != TutorialState.Completed;
        bool hasSideWork = activeIndependentStep != null;
        if (!hasMainWork && !hasSideWork) return;

        ClearPendingAttacker();
        ExitDodgeBulletTime(restoreNormalTimeScale: true);

        if (GameAssets.Instance != null)
        {
            GameAssets.Instance.PlayUIClickSound();
        }

        if (activeIndependentStep != null)
        {
            PlayerPrefs.SetInt(GetIndependentPrefKey(activeIndependentStep), 1);
            PlayerPrefs.Save();
            activeIndependentStep = null;
            isPlayingIndependentComplete = false;
        }

        if (hasMainWork)
        {
            MarkTutorialCompleted();
            state = TutorialState.Inactive;
            UpdateStartRoomDoorLock();
        }

        hudController?.PlayHide();
    }

    private void InitializeTutorialOnSceneStart()
    {
        if (tutorialSequence == null || tutorialSequence.StepCount == 0)
        {
            state = TutorialState.Inactive;
            UpdateStartRoomDoorLock();
            hudController?.HideImmediate();
            return;
        }

        bool isAlreadyCompleted = PlayerPrefs.GetInt(TutorialCompletedPrefKey, 0) == 1;
        if (isAlreadyCompleted && !forceReplayOnStart)
        {
            state = TutorialState.Inactive;
            UpdateStartRoomDoorLock();
            hudController?.HideImmediate();
            return;
        }

        if (forceReplayOnStart)
        {
            ResetTutorialSavedState();
            ResetIndependentStepsSavedState();
        }

        currentStepIndex = 0;
        currentProgress = 0f;
        hasCompletedDodgeStep = false;
        resumeStepIndexAfterDodge = -1;
        state = TutorialState.WaitingForTrigger;

        TryBindExistingPlayer();
        UpdateStartRoomDoorLock();
        TryActivateCurrentStepIfReady();
    }

    private void ResetRuntimeTracking()
    {
        currentProgress = 0f;
        skipHoldTimer = 0f;
        hasEnteredFightingRoom = false;
        currentFightingRoom = null;
        killsInCurrentFightingRoom = 0;
        currentDetectedInteractable = string.Empty;
        lastSuppressedState = false;
        hasCompletedDodgeStep = false;
        resumeStepIndexAfterDodge = -1;
        activeIndependentStep = null;
        isPlayingIndependentComplete = false;
        ClearPendingAttacker();
        hudController?.SetSuppressed(false);
        hudController?.SetSkipHoldProgress(0f);
    }

    private void TryBindExistingPlayer()
    {
        if (playerTransform != null) return;

        GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null)
        {
            BindPlayerReferences(playerGo.transform);
        }
        else if (uiController != null)
        {
            PlayerInput input = uiController.GetPlayerInput();
            if (input != null)
            {
                BindPlayerReferences(input.transform);
            }
        }
    }

    private void BindPlayerReferences(Transform spawnedPlayer)
    {
        playerTransform = spawnedPlayer;
        if (spawnedPlayer == null) return;

        isPlayerSpawned = true;
        playerInput = spawnedPlayer.GetComponent<PlayerInput>();
        characterInput = spawnedPlayer.GetComponent<CharacterInputSystem>();
    }

    private bool ShouldSuppressTutorial()
    {
        if (uiController != null && uiController.HasOpenPanels)
        {
            return true;
        }

        if (playerInput != null && playerInput.currentActionMap != null && playerInput.currentActionMap.name != "Player")
        {
            return true;
        }

        return false;
    }

    private void UpdateSkipHoldInput()
    {
        bool canSkipMain = state == TutorialState.ActiveStep || state == TutorialState.WaitingForTrigger;
        bool canSkipSide = activeIndependentStep != null && !isPlayingIndependentComplete;
        if (!canSkipMain && !canSkipSide)
        {
            ResetSkipHold();
            return;
        }

        bool isHoldingSkip = Keyboard.current != null && Keyboard.current.tabKey.isPressed;
        if (isHoldingSkip)
        {
            skipHoldTimer += Time.unscaledDeltaTime;
            float ratio = skipHoldDuration > 0.01f ? skipHoldTimer / skipHoldDuration : 1f;
            hudController?.SetSkipHoldProgress(ratio);

            if (skipHoldTimer >= skipHoldDuration)
            {
                skipHoldTimer = 0f;
                SkipTutorial();
            }
        }
        else if (skipHoldTimer > 0f)
        {
            ResetSkipHold();
        }
    }

    private void ResetSkipHold()
    {
        if (skipHoldTimer > 0f)
        {
            skipHoldTimer = 0f;
        }
        hudController?.SetSkipHoldProgress(0f);
    }

    private void UpdateActiveStepCondition()
    {
        TutorialStepSO step = tutorialSequence != null ? tutorialSequence.GetStep(currentStepIndex) : null;
        if (step == null) return;

        if (characterInput == null)
        {
            TryBindExistingPlayer();
            if (characterInput == null) return;
        }

        bool progressed = false;

        switch (step.conditionType)
        {
            case TutorialConditionType.MoveDuration:
                if (characterInput.playerMovement.sqrMagnitude > 0.01f)
                {
                    currentProgress += Time.deltaTime;
                    progressed = true;
                }
                break;

            case TutorialConditionType.RunDuration:
                if (characterInput.playerMovement.sqrMagnitude > 0.01f && characterInput.playerRun)
                {
                    currentProgress += Time.deltaTime;
                    progressed = true;
                }
                break;

            case TutorialConditionType.RollCount:
            case TutorialConditionType.AttackCount:
            case TutorialConditionType.PerfectDodgeInBulletTime:
                // 改由 UIEvents 事件精準回報，不受 Update 執行順序影響
                break;

            case TutorialConditionType.CameraLookAmount:
                if (characterInput.playerCamera.sqrMagnitude > 0.01f)
                {
                    currentProgress += characterInput.playerCamera.magnitude * Time.deltaTime;
                    progressed = true;
                }
                break;

            case TutorialConditionType.OpenCharacterPanel:
                if (playerInput != null && playerInput.actions != null)
                {
                    InputAction charAction = playerInput.actions.FindAction("Character");
                    if (charAction != null && charAction.triggered)
                    {
                        currentProgress += 1f;
                        progressed = true;
                    }
                }
                break;
        }

        if (progressed)
        {
            OnProgressUpdated(step);
        }
    }

    private void OnProgressUpdated(TutorialStepSO step)
    {
        if (step == null || state != TutorialState.ActiveStep) return;

        hudController?.UpdateProgress(step, currentProgress);

        if (currentProgress >= step.targetProgress)
        {
            CompleteCurrentStep(step);
        }
    }

    private void CompleteCurrentStep(TutorialStepSO step)
    {
        if (state != TutorialState.ActiveStep) return;

        if (step.conditionType == TutorialConditionType.PerfectDodgeInBulletTime)
        {
            hasCompletedDodgeStep = true;
            ExitDodgeBulletTime(restoreNormalTimeScale: true);
        }

        state = TutorialState.CompletingStep;
        ResetSkipHold();

        hudController?.PlayStepCompleted(step, AdvanceToNextStep);
    }

    private void AdvanceToNextStep()
    {
        // 若剛完成插隊的戰鬥閃避教學，恢復先前還沒打完的擊殺敵人步驟
        if (resumeStepIndexAfterDodge >= 0)
        {
            int resumeIdx = resumeStepIndexAfterDodge;
            resumeStepIndexAfterDodge = -1;
            currentStepIndex = resumeIdx;
            currentProgress = 0f;
            state = TutorialState.WaitingForTrigger;
            TryActivateCurrentStepIfReady();
            return;
        }

        currentStepIndex++;
        currentProgress = 0f;

        // 跳過先前已於戰鬥中插隊完成的閃避教學
        while (tutorialSequence != null && currentStepIndex < tutorialSequence.StepCount)
        {
            TutorialStepSO candidate = tutorialSequence.GetStep(currentStepIndex);
            if (candidate != null && candidate.conditionType == TutorialConditionType.PerfectDodgeInBulletTime && hasCompletedDodgeStep)
            {
                currentStepIndex++;
                continue;
            }
            break;
        }

        if (tutorialSequence == null || currentStepIndex >= tutorialSequence.StepCount)
        {
            MarkTutorialCompleted();
            state = TutorialState.Completed;
            UpdateStartRoomDoorLock();
            return;
        }

        state = TutorialState.WaitingForTrigger;
        UpdateStartRoomDoorLock();
        TryActivateCurrentStepIfReady();
    }

    private void TryActivateCurrentStepIfReady()
    {
        if (state != TutorialState.WaitingForTrigger || tutorialSequence == null) return;

        TutorialStepSO step = tutorialSequence.GetStep(currentStepIndex);
        if (step == null)
        {
            MarkTutorialCompleted();
            state = TutorialState.Completed;
            return;
        }

        bool ready = false;
        switch (step.triggerMode)
        {
            case TutorialTriggerMode.Immediate:
                ready = isPlayerSpawned;
                break;

            case TutorialTriggerMode.OnEnterFightingRoom:
                ready = hasEnteredFightingRoom;
                break;

            case TutorialTriggerMode.OnInteractableDetected:
                ready = !string.IsNullOrEmpty(currentDetectedInteractable);
                break;

            case TutorialTriggerMode.OnEnemyAttackInCombat:
                // 僅在偵測到敵人揮砍時由 EnterDodgeBulletTimeStep() 啟動
                ready = false;
                break;
        }

        if (ready)
        {
            ActivateStep(currentStepIndex);
        }
    }

    private void ActivateStep(int index)
    {
        if (tutorialSequence == null) return;

        TutorialStepSO step = tutorialSequence.GetStep(index);
        if (step == null) return;

        currentStepIndex = index;
        currentProgress = 0f;
        state = TutorialState.ActiveStep;
        UpdateStartRoomDoorLock();

        // 容錯保護：若進入「擊殺敵人」步驟前，於同個戰鬥房內已達成擊殺或清空房間，直接給予進度避免卡關
        if (step.conditionType == TutorialConditionType.KillEnemyCount)
        {
            if (killsInCurrentFightingRoom > 0)
            {
                currentProgress = Mathf.Min(killsInCurrentFightingRoom, step.targetProgress);
            }
            else if (currentFightingRoom != null && currentFightingRoom.IsClear)
            {
                currentProgress = step.targetProgress;
            }
        }

        string formattedDesc = FormatStepDescription(step);
        hudController?.ShowStep(step, formattedDesc, currentStepIndex + 1, tutorialSequence.StepCount, currentProgress);

        if (currentProgress >= step.targetProgress)
        {
            CompleteCurrentStep(step);
        }
    }

    private void MarkTutorialCompleted()
    {
        PlayerPrefs.SetInt(TutorialCompletedPrefKey, 1);
        PlayerPrefs.Save();
    }
    private static string GetIndependentPrefKey(TutorialStepSO step)
    {
        string keyId = !string.IsNullOrEmpty(step.stepId) ? step.stepId : step.name;
        return $"Tutorial_Side_{keyId}_Completed";
    }

    private void ResetIndependentStepsSavedState()
    {
        if (independentSteps == null) return;
        for (int i = 0; i < independentSteps.Count; i++)
        {
            if (independentSteps[i] != null)
            {
                PlayerPrefs.DeleteKey(GetIndependentPrefKey(independentSteps[i]));
            }
        }
        PlayerPrefs.Save();
    }

    private void TryTriggerIndependentStep(TutorialTriggerMode triggerMode)
    {
        // 若正在播主線步驟或已經有獨立任務在顯示中，先不搶畫面
        if (activeIndependentStep != null) return;
        if (state == TutorialState.ActiveStep || state == TutorialState.CompletingStep) return;
        if (independentSteps == null) return;

        for (int i = 0; i < independentSteps.Count; i++)
        {
            TutorialStepSO step = independentSteps[i];
            if (step == null || step.triggerMode != triggerMode) continue;

            // 已經完成過這項獨立任務就跳過
            if (!forceReplayOnStart && PlayerPrefs.GetInt(GetIndependentPrefKey(step), 0) == 1)
                continue;

            activeIndependentStep = step;
            isPlayingIndependentComplete = false;
            currentProgress = 0f;

            hudController?.ShowStep(step, FormatStepDescription(step), 1, 1, 0f);
            return;
        }
    }

    private void CompleteIndependentStep()
    {
        if (activeIndependentStep == null || isPlayingIndependentComplete) return;
        isPlayingIndependentComplete = true;

        TutorialStepSO finishedStep = activeIndependentStep;
        PlayerPrefs.SetInt(GetIndependentPrefKey(finishedStep), 1);
        PlayerPrefs.Save();

        hudController?.PlayStepCompleted(finishedStep, () =>
        {
            activeIndependentStep = null;
            isPlayingIndependentComplete = false;

            // 如果主線原本正在待命，順便檢查是否接續
            if (state == TutorialState.WaitingForTrigger)
            {
                TryActivateCurrentStepIfReady();
            }
        });
    }

    private int FindInteractStepIndex()
    {
        if (tutorialSequence == null || tutorialSequence.steps == null) return -1;
        for (int i = 0; i < tutorialSequence.steps.Count; i++)
        {
            TutorialStepSO s = tutorialSequence.steps[i];
            if (s != null && s.conditionType == TutorialConditionType.InteractCount)
            {
                return i;
            }
        }
        return -1;
    }

    private void UpdateStartRoomDoorLock()
    {
        int interactIndex = FindInteractStepIndex();
        bool isTutorialRunning = state == TutorialState.WaitingForTrigger
                              || state == TutorialState.ActiveStep
                              || state == TutorialState.CompletingStep;

        bool shouldLock = isTutorialRunning && interactIndex > 0 && currentStepIndex < interactIndex;
        SetStartRoomDoorsLocked(shouldLock);
    }

    private void SetStartRoomDoorsLocked(bool locked)
    {
        int unInteractableLayer = LayerMask.NameToLayer("UnInteractable");
        if (unInteractableLayer < 0) return;

        if (!locked)
        {
            foreach (var kvp in cachedStartRoomDoorLayers)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.layer = kvp.Value;
                }
            }
            cachedStartRoomDoorLayers.Clear();
            return;
        }

        StartRoom startRoom = FindFirstObjectByType<StartRoom>();
        if (startRoom == null) return;

        int navMeshGroundLayer = LayerMask.NameToLayer("NavMeshGround");
        Door[] doors = startRoom.GetComponentsInChildren<Door>(true);
        for (int i = 0; i < doors.Length; i++)
        {
            if (doors[i] == null) continue;

            Collider[] cols = doors[i].GetComponentsInChildren<Collider>(true);
            for (int j = 0; j < cols.Length; j++)
            {
                if (cols[j] == null) continue;
                GameObject colObj = cols[j].gameObject;
                if (colObj.layer == navMeshGroundLayer || colObj.layer == unInteractableLayer) continue;

                if (!cachedStartRoomDoorLayers.ContainsKey(colObj))
                {
                    cachedStartRoomDoorLayers[colObj] = colObj.layer;
                }
                colObj.layer = unInteractableLayer;
            }
        }
    }

    #region 閃避子彈時間管理
    private void UpdatePendingEnemyAttack()
    {
        if (pendingAttacker == null || isDodgeBulletTimeActive || hasCompletedDodgeStep) return;

        if (!pendingAttacker.gameObject.activeInHierarchy || playerTransform == null)
        {
            ClearPendingAttacker();
            return;
        }

        float dist = Vector3.Distance(pendingAttacker.position, playerTransform.position);
        if (dist > dodgeTriggerDistance + 1.5f)
        {
            ClearPendingAttacker();
            return;
        }

        pendingAttackDelayTimer += Time.deltaTime;
        if (pendingAttackDelayTimer >= dodgeBulletTimeDelay)
        {
            ClearPendingAttacker();
            TryTriggerDodgeBulletTimeStep();
        }
    }

    private void ClearPendingAttacker()
    {
        pendingAttacker = null;
        pendingAttackDelayTimer = 0f;
    }

    private void TryTriggerDodgeBulletTimeStep()
    {
        if (tutorialSequence == null || hasCompletedDodgeStep || isDodgeBulletTimeActive) return;
        if (state != TutorialState.WaitingForTrigger && state != TutorialState.ActiveStep) return;

        int dodgeIndex = FindDodgeStepIndex();
        if (dodgeIndex < 0) return;

        // 若目前正在執行其他尚未完成的步驟，暫存該步驟索引，閃避完再切回
        if (state == TutorialState.ActiveStep && currentStepIndex < dodgeIndex)
        {
            resumeStepIndexAfterDodge = currentStepIndex;
        }

        activeIndependentStep = null;
        isPlayingIndependentComplete = false;
        EnterDodgeBulletTime();
        ActivateStep(dodgeIndex);
    }

    private int FindDodgeStepIndex()
    {
        if (tutorialSequence == null || tutorialSequence.steps == null) return -1;
        for (int i = 0; i < tutorialSequence.steps.Count; i++)
        {
            TutorialStepSO s = tutorialSequence.steps[i];
            if (s != null && (s.triggerMode == TutorialTriggerMode.OnEnemyAttackInCombat || s.conditionType == TutorialConditionType.PerfectDodgeInBulletTime))
            {
                return i;
            }
        }
        return -1;
    }

    private void EnterDodgeBulletTime()
    {
        isDodgeBulletTimeActive = true;
        bulletTimeRealTimer = 0f;

        float safeScale = Mathf.Clamp(dodgeBulletTimeScale, 0.03f, 0.5f);
        Time.timeScale = safeScale;
        Time.fixedDeltaTime = DefaultFixedDeltaTime * safeScale;
    }

    private void ExitDodgeBulletTime(bool restoreNormalTimeScale)
    {
        if (!isDodgeBulletTimeActive) return;
        isDodgeBulletTimeActive = false;
        bulletTimeRealTimer = 0f;

        if (restoreNormalTimeScale)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = DefaultFixedDeltaTime;
        }
    }

    private void UpdateDodgeBulletTimeTimeout()
    {
        if (!isDodgeBulletTimeActive) return;

        bulletTimeRealTimer += Time.unscaledDeltaTime;
        if (bulletTimeRealTimer >= maxBulletTimeRealDuration)
        {
            // 若逾時仍未按下閃避，先恢復正常時間避免永久慢動作，等待下次敵人出招再重新觸發
            ExitDodgeBulletTime(restoreNormalTimeScale: true);
        }
    }
    #endregion

    #region 事件接收
    private void HandlePlayerSpawned(Transform spawnedPlayer)
    {
        BindPlayerReferences(spawnedPlayer);
        UpdateStartRoomDoorLock();
        if (state == TutorialState.WaitingForTrigger)
        {
            TryActivateCurrentStepIfReady();
        }
    }

    private void HandleRoomEntered(Room room)
    {
        // 離開商店並進入戰鬥房時，若獨立任務允許彈性推進且尚未完成，先收起避免擋住戰鬥教學
        if (room is FightingRoom && activeIndependentStep != null && !isPlayingIndependentComplete && activeIndependentStep.allowElasticAdvance)
        {
            activeIndependentStep = null;
            isPlayingIndependentComplete = false;
            hudController?.PlayHide();
        }

        if (room is FightingRoom fightingRoom)
        {
            hasEnteredFightingRoom = true;
            currentFightingRoom = fightingRoom;
            killsInCurrentFightingRoom = 0;

            if (state == TutorialState.WaitingForTrigger || state == TutorialState.ActiveStep)
            {
                TutorialStepSO currentStep = tutorialSequence != null ? tutorialSequence.GetStep(currentStepIndex) : null;
                if (currentStep != null && currentStep.triggerMode != TutorialTriggerMode.OnEnterFightingRoom && currentStep.allowElasticAdvance)
                {
                    int combatStepIndex = tutorialSequence.FindNextStepIndexByTrigger(currentStepIndex, TutorialTriggerMode.OnEnterFightingRoom);
                    if (combatStepIndex > currentStepIndex)
                    {
                        ActivateStep(combatStepIndex);
                        return;
                    }
                }

                if (state == TutorialState.WaitingForTrigger)
                {
                    TryActivateCurrentStepIfReady();
                }
            }
        }else if(room is ShopRoom)
        {
            TryTriggerIndependentStep(TutorialTriggerMode.OnEnterShopRoom);
        }
    }

    private void HandleInteractableDetected(string interactableName)
    {
        currentDetectedInteractable = interactableName ?? string.Empty;

        if (state == TutorialState.WaitingForTrigger && !string.IsNullOrEmpty(currentDetectedInteractable))
        {
            TryActivateCurrentStepIfReady();
        }
    }

    private void HandleInteractCompleted(IInteractable interactable)
    {
        // 先檢查是不是正在做獨立的商店任務
        if (activeIndependentStep != null && !isPlayingIndependentComplete)
        {
            if (activeIndependentStep.conditionType == TutorialConditionType.ShopInteractCount
                && interactable is ShopTableInteract)
            {
                currentProgress += 1f;
                hudController?.UpdateProgress(activeIndependentStep, currentProgress);
                if (currentProgress >= activeIndependentStep.targetProgress)
                {
                    CompleteIndependentStep();
                }
            }
            return;
        }

        if (state != TutorialState.ActiveStep || tutorialSequence == null) return;

        TutorialStepSO step = tutorialSequence.GetStep(currentStepIndex);
        if (step != null && step.conditionType == TutorialConditionType.InteractCount)
        {
            currentProgress += 1f;
            OnProgressUpdated(step);
        }
    }

    private void HandleEnemyKilled()
    {
        killsInCurrentFightingRoom++;

        if (state != TutorialState.ActiveStep || tutorialSequence == null) return;

        TutorialStepSO step = tutorialSequence.GetStep(currentStepIndex);
        if (step != null && step.conditionType == TutorialConditionType.KillEnemyCount)
        {
            currentProgress += 1f;
            OnProgressUpdated(step);
        }
    }

    private void HandlePlayerAttacked()
    {
        if (state != TutorialState.ActiveStep || tutorialSequence == null || ShouldSuppressTutorial()) return;

        TutorialStepSO step = tutorialSequence.GetStep(currentStepIndex);
        if (step != null && step.conditionType == TutorialConditionType.AttackCount)
        {
            currentProgress += 1f;
            OnProgressUpdated(step);
        }
    }

    private void HandlePlayerRolled()
    {
        if (state != TutorialState.ActiveStep || tutorialSequence == null || ShouldSuppressTutorial()) return;

        TutorialStepSO step = tutorialSequence.GetStep(currentStepIndex);
        if (step == null) return;

        if (step.conditionType == TutorialConditionType.RollCount)
        {
            currentProgress += 1f;
            OnProgressUpdated(step);
        }
        else if (step.conditionType == TutorialConditionType.PerfectDodgeInBulletTime)
        {
            // 玩家於敵人出招／子彈時間期間按下閃避，立即恢復正常時間使翻滾無敵幀生效並完成步驟
            ExitDodgeBulletTime(restoreNormalTimeScale: true);
            currentProgress += 1f;
            OnProgressUpdated(step);
        }
    }

    private void HandleEnemyAttackStarted(Transform attacker)
    {
        if (!hasEnteredFightingRoom || hasCompletedDodgeStep || isDodgeBulletTimeActive) return;
        if (state != TutorialState.WaitingForTrigger && state != TutorialState.ActiveStep) return;
        if (ShouldSuppressTutorial() || attacker == null || playerTransform == null) return;

        // 僅在尚未開始基礎移動/互動或已進入戰鬥階段後才觸發閃避子彈時間
        int dodgeIndex = FindDodgeStepIndex();
        if (dodgeIndex < 0) return;

        float dist = Vector3.Distance(attacker.position, playerTransform.position);
        if (dist > dodgeTriggerDistance) return;

        if (pendingAttacker == attacker) return;

        if (dodgeBulletTimeDelay <= 0f)
        {
            TryTriggerDodgeBulletTimeStep();
            return;
        }

        // 敵人出手後等待 dodgeBulletTimeDelay（預設 0.1s）再切入子彈時間
        pendingAttacker = attacker;
        pendingAttackDelayTimer = 0f;
    }

    private void HandlePlayerPerfectDodge()
    {
        if (state != TutorialState.ActiveStep || tutorialSequence == null) return;

        TutorialStepSO step = tutorialSequence.GetStep(currentStepIndex);
        if (step != null && step.conditionType == TutorialConditionType.PerfectDodgeInBulletTime)
        {
            ExitDodgeBulletTime(restoreNormalTimeScale: true);
            currentProgress += 1f;
            OnProgressUpdated(step);
        }
    }

    private void HandleBindingsChanged()
    {
        if (activeIndependentStep != null)
        {
            hudController?.RefreshDescription(FormatStepDescription(activeIndependentStep));
            return;
        }

        if (state != TutorialState.ActiveStep || tutorialSequence == null) return;

        TutorialStepSO step = tutorialSequence.GetStep(currentStepIndex);
        if (step != null)
        {
            hudController?.RefreshDescription(FormatStepDescription(step));
        }
    }
    #endregion

    #region 動態按鍵文案解析
    private string FormatStepDescription(TutorialStepSO step)
    {
        if (step == null || string.IsNullOrEmpty(step.descriptionTemplate)) return string.Empty;

        string result = step.descriptionTemplate;
        if (step.actionBindings == null || step.actionBindings.Count == 0) return result;

        InputActionAsset asset = (playerInput != null && playerInput.actions != null)
            ? playerInput.actions
            : fallbackInputActions;

        for (int i = 0; i < step.actionBindings.Count; i++)
        {
            TutorialActionPlaceholder binding = step.actionBindings[i];
            if (binding == null || string.IsNullOrEmpty(binding.placeholder)) continue;

            string keyLabel = ResolveActionKeyLabel(asset, binding);
            string styledKey = $"<color=#FCECA6><b>[{keyLabel}]</b></color>";
            result = result.Replace(binding.placeholder, styledKey);
        }

        return result;
    }

    private string ResolveActionKeyLabel(InputActionAsset asset, TutorialActionPlaceholder binding)
    {
        if (asset == null || string.IsNullOrEmpty(binding.actionPath))
        {
            return binding.placeholder.Trim('{', '}');
        }

        InputAction action = asset.FindAction(binding.actionPath);
        if (action == null)
        {
            return binding.placeholder.Trim('{', '}');
        }

        if (binding.isCompositeGroup)
        {
            List<string> parts = new List<string>(4);
            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding b = action.bindings[i];
                if (!b.isPartOfComposite) continue;

                string readable = InputControlPath.ToHumanReadableString(
                    b.effectivePath,
                    InputControlPath.HumanReadableStringOptions.OmitDevice
                );
                if (!string.IsNullOrEmpty(readable))
                {
                    parts.Add(FormatReadableKeyName(readable));
                }
            }

            if (parts.Count == 4 && parts[0] == "W" && parts[1] == "S" && parts[2] == "A" && parts[3] == "D")
            {
                return "WASD";
            }
            if (parts.Count > 0)
            {
                return string.Join("/", parts);
            }
        }

        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding b = action.bindings[i];
            if (b.isComposite) continue;

            string readable = InputControlPath.ToHumanReadableString(
                b.effectivePath,
                InputControlPath.HumanReadableStringOptions.OmitDevice
            );
            if (!string.IsNullOrEmpty(readable))
            {
                return FormatReadableKeyName(readable);
            }
        }

        return binding.placeholder.Trim('{', '}');
    }

    private static string FormatReadableKeyName(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;

        switch (raw)
        {
            case "Left Button": return "滑鼠左鍵";
            case "Right Button": return "滑鼠右鍵";
            case "Middle Button": return "滑鼠中鍵";
            case "Left Shift": return "Shift";
            case "Right Shift": return "R-Shift";
            case "Space": return "空白鍵";
            case "Up Arrow": return "↑";
            case "Down Arrow": return "↓";
            case "Left Arrow": return "←";
            case "Right Arrow": return "→";
            default: return raw;
        }
    }
    #endregion

#if UNITY_EDITOR
    [ContextMenu("重置新手教學存檔")]
    private void EditorResetTutorialSavedState()
    {
        ResetTutorialSavedState();
        ResetIndependentStepsSavedState();
        Debug.Log("[TutorialManager] 已清除主線與獨立教學存檔。");
    }

    [ContextMenu("立即強制重播教學")]
    private void EditorRestartTutorialNow()
    {
        if (!Application.isPlaying)
        {
            EditorResetTutorialSavedState();
            return;
        }
        RestartTutorial();
        Debug.Log("[TutorialManager] 已於執行中強制重播新手教學。");
    }
#endif
}

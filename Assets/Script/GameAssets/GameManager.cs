using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : SingletonBase<GameManager>
{
    [Header("遊戲進度")]
    public int characterID = 0;
    public int currentMajorStage = 1;
    public int currentLevel = 1;
    public bool isWin = false;
    public Transform player;

    [Header("統計資料")]
    public int deathCount { get; private set; }
    public float totalPlayTime;       // 歷史總時數
    public float currentRunPlayTime;  // 本局遊玩時數
    public int killCount;
    public int currentGold;           // 本局金幣數

    // 保存最後一局的結算快照 (Model)，供 FinalScene 讀取
    public RunResultData LastRunResult { get; private set; } = new RunResultData();
    private bool isInGameplay = false;

    private void OnEnable()
    {
        GameEvent.OnPlayerSpawn += SetPlayer;
    }
    private void OnDisable()
    {
        GameEvent.OnPlayerSpawn -= SetPlayer;
    }

    protected override void Awake()
    {
        base.Awake();
        deathCount = PlayerPrefs.GetInt("DeathCount", 0);
        totalPlayTime = PlayerPrefs.GetFloat("TotalPlayTime", 0f);
    }
    private void Update()
    {
        totalPlayTime += Time.unscaledDeltaTime;
        if (isInGameplay)
        {
            currentRunPlayTime += Time.deltaTime;
        }
    }
    /// <summary>
    /// 開始新的一局時呼叫（例如從選角大廳點擊開始遊戲時）
    /// </summary>
    public void init()
    {
        currentMajorStage = 1;
        currentLevel = 1;
        killCount = 0;
        currentGold = 0;
        currentRunPlayTime = 0f;
        isWin = false;
        isInGameplay = true;
    }
    private void SetPlayer(Transform transform)
    {
        player = transform;
        isInGameplay = true;
    }

    public void GoToNextLevel()
    {
        currentLevel++;
        Debug.Log($"[GameManager] 前往第 {currentLevel} 關！");

        if (LoadingController.Instance != null)
        {
            LoadingController.Instance.LoadLevel(SceneManager.GetActiveScene().name);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
    public void GameOver()
    {
        isInGameplay = false;
        Cursor.lockState = CursorLockMode.None;


        CaptureRunResult(player);
        if (player != null)
        {
            Destroy(player.gameObject);
        }

        if (LoadingController.Instance != null)
        {
            LoadingController.Instance.LoadLevel("FinalScene");
        }
        else
        {
            SceneManager.LoadScene("FinalScene");
        }
    }
    public void QuitGame()
    {
        isInGameplay = false;
        Cursor.lockState = CursorLockMode.None;

        if (player != null)
        {
            Destroy(player.gameObject);
        }

        if (LoadingController.Instance != null)
        {
            LoadingController.Instance.LoadLevel("MainMenu");
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }

        GameAssets.Instance.PlayMenuMusic();
    }

    /// <summary>
    /// 將本局進度與角色身上的最終屬性/裝備打包進 LastRunResult
    /// </summary>
    private void CaptureRunResult(Transform playerObj)
    {
        LastRunResult = new RunResultData
        {
            isVictory = this.isWin,
            stageProgress = $"{currentMajorStage} - {currentLevel}",
            stageName = $"深淵石牢・第 {currentLevel} 區",
            playTimeText = FormatTime(currentRunPlayTime),
            killCount = this.killCount,
            goldCount = this.currentGold,
            characterName = (characterID == 0) ? "戰 士" : "冒險者"
        };

        // TODO: 若你的 Player 物件上有掛載屬性腳本 (如 CharacterStats) 與裝備腳本 (如 CharacterEquipment)，
        // 在此處從 playerObj.GetComponent<...>() 讀取實際數值填入：
        if (playerObj != null)
        {
            CharacterStats stats = playerObj.GetComponent<CharacterStats>();
            // 範例預設值（請替換為你實際的角色屬性腳本欄位，例如 stats.MaxHP）
            LastRunResult.hp = stats.maxHealth;
            LastRunResult.def = stats.shield;
            LastRunResult.atk = stats.damage;
            
            CharacterCombatBase combat = playerObj.GetComponentInChildren<CharacterCombatBase>();
            LastRunResult.mainWeapon = new EquipmentSlotData
            {
                slotCategory = "主武器",
                itemName = combat.currentWeapon.weaponName,
                subInfo = $"單手劍 | 攻擊 +{combat.currentWeapon.damage}"
            };

            string[] slotNames = { "頭盔", "胸甲", "護腿", "手套", "腰帶", "戰靴" };
            Equipment.EquipmentType[] uiSlotOrder = 
            {
                Equipment.EquipmentType.Head,  // 對應第 0 格：頭盔
                Equipment.EquipmentType.Chest, // 對應第 1 格：胸甲
                Equipment.EquipmentType.Leg,   // 對應第 2 格：護腿
                Equipment.EquipmentType.Arm,   // 對應第 3 格：手套
                Equipment.EquipmentType.Belt,  // 對應第 4 格：腰帶
                Equipment.EquipmentType.Feet   // 對應第 5 格：戰靴
            };
            for (int i = 0; i < 6; i++)
            {
                LastRunResult.armors[i] = new EquipmentSlotData
                {
                    slotCategory = slotNames[i],
                    itemName = "未裝備" // 若該部位有穿裝備則填入裝備名稱與 Icon
                };
            }
            CharacterEquipment charEquip = playerObj.GetComponentInChildren<CharacterEquipment>();
            if (charEquip != null)
            {
                for (int i = 0; i < uiSlotOrder.Length; i++)
                {
                    GameObject equippedGO = charEquip.GetEquippedItem(uiSlotOrder[i]);
                    if (equippedGO != null)
                    {
                        Equipment eq = equippedGO.GetComponent<Equipment>();
                        // 確保列表有資料且索引合法，防止越界報錯
                        if (eq != null && eq.equipmentDataList != null && 
                            eq.currentEquipmentIndex >= 0 && eq.currentEquipmentIndex < eq.equipmentDataList.Count)
                        {
                            LastRunResult.armors[i].itemName = eq.equipmentName;
                            LastRunResult.armors[i].icon = eq.itemIcon;
                        }
                    }
                }
            }
        }
    }


    #region 統計資料
    public void StartStats()
    {
        deathCount = 0;
    }
    public void AddDeath()
    {
        deathCount += 1;
        PlayerPrefs.SetInt("DeathCount", deathCount);
        PlayerPrefs.Save();
    }
    public void AddKill() => killCount += 1;
    public void AddGold(int amount) => currentGold += amount;
    public void ResetStats()
    {
        deathCount = 0;
        totalPlayTime = 0;

        PlayerPrefs.DeleteKey("DeathCount");
        PlayerPrefs.DeleteKey("TotalPlayTime");

        PlayerPrefs.Save();
    }
    public void OnApplicationQuit()
    {
        PlayerPrefs.SetFloat("TotalPlayTime", totalPlayTime);
        PlayerPrefs.Save();
    }
    public void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            PlayerPrefs.SetFloat("TotalPlayTime", totalPlayTime);
            PlayerPrefs.Save();
        }
    }
    public string GetFormattedPlayTime() => FormatTime(totalPlayTime);
    private string FormatTime(float timeInSeconds)
    {
        int totalSeconds = Mathf.FloorToInt(timeInSeconds);
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int seconds = totalSeconds % 60;
        return $"{hours:00}:{minutes:00}:{seconds:00}";
    }
    #endregion
}

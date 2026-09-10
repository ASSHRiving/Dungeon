using UnityEngine;
using System.Collections;

public class GameAssets : SingletonBase<GameAssets>
{
    [SerializeField,Header("資源")] private GameSoundSO soundAssets;
    private AudioSource _audioSource;
    [SerializeField] private AudioClip inGameMusicClip;
    [SerializeField] private AudioClip menuMusicClip;
    [SerializeField] private AudioClip bossMusicClip;
    [SerializeField] private AudioClip uiClickSound;
    
    [Header("Hitstop (頓幀)")]
    private Coroutine currentHitstopCoroutine;

    protected override void Awake()
    {
        base.Awake();
        soundAssets.InitAssets();
        _audioSource = GetComponent<AudioSource>();
    }
    private void Start()
    {
        PlayMenuMusic();
    }

    public void PlaySoundEffect(AudioSource audioSource,SoundAssetsType soundAssetsType)
    {
        audioSource.clip = soundAssets.GetClipAssets(soundAssetsType);
        audioSource.Play();
    }
    public void PlayInGameMusic()
    {
        if(inGameMusicClip != null)
        {
            _audioSource.clip = inGameMusicClip;
            _audioSource.loop = true;
            _audioSource.Play();
        }
    }
    public void PlayMenuMusic()
    {
        if(menuMusicClip != null)
        {
            _audioSource.clip = menuMusicClip;
            _audioSource.loop = true;
            _audioSource.Play();
        }
    }
    public void PlayBossMusic()
    {
        if(bossMusicClip != null)
        {
            _audioSource.clip = bossMusicClip;
            _audioSource.loop = true;
            _audioSource.Play();
        }
    }
    public void PlayUIClickSound()
    {
        if(uiClickSound != null)
        {
            _audioSource.PlayOneShot(uiClickSound, 3f);
        }
    }
    /// <summary>
    /// 觸發 Hitstop (頓幀)
    /// </summary>
    /// <param name="duration">停頓持續時間 (真實時間秒數，建議 0.03f ~ 0.08f)</param>
    /// <param name="timeScale">停頓時的時間比例 (建議 0.01f ~ 0.05f，不建議直接給 0 以免物理或音效異常)</param>
    public void DoHitstop(float duration, float timeScale = 0.02f)
    {
        // 如果已經在 Hitstop 中，先停止舊的，重新開始新的 (讓連續打擊更順暢)
        if (currentHitstopCoroutine != null)
        {
            StopCoroutine(currentHitstopCoroutine);
        }

        currentHitstopCoroutine = StartCoroutine(HitstopRoutine(duration, timeScale));
    }

    private IEnumerator HitstopRoutine(float duration, float timeScale)
    {

        // 1. 設定極小時間縮放，達到看似「畫面凍結」的效果
        Time.timeScale = timeScale;

        // 2. 必須使用 WaitForSecondsRealtime！
        // 因為 Time.timeScale 被降低了，一般的 WaitForSeconds 會跟著變慢導致卡死
        yield return new WaitForSecondsRealtime(duration);

        // 3. 恢復正常時間
        Time.timeScale = 1.0f;
        
        currentHitstopCoroutine = null;
    }

    // 當場景切換或物件被停用時的安全保護
    private void OnDisable()
    {
        Time.timeScale = 1.0f;

    }
}

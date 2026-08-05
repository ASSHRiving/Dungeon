using UnityEngine;
using Unity.Cinemachine;

public class GameAssets : SingletonBase<GameAssets>
{
    [SerializeField,Header("資源")] private GameSoundSO soundAssets;
    private AudioSource _audioSource;
    [SerializeField]private AudioClip inGameMusicClip;
    [SerializeField]private AudioClip menuMusicClip;

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
}

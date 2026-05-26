using UnityEngine;

public class GameAssets : SingletonBase<GameAssets>
{
    [SerializeField,Header("資源")] private GameSoundSO soundAssets;

    private void Awake()
    {
        soundAssets.InitAssets();
    }

    public void PlaySoundEffect(AudioSource audioSource,SoundAssetsType soundAssetsType)
    {
        audioSource.clip = soundAssets.GetClipAssets(soundAssetsType);
        audioSource.Play();
    }
}

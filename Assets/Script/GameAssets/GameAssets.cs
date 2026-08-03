using UnityEngine;
using Unity.Cinemachine;

public class GameAssets : SingletonBase<GameAssets>
{
    [SerializeField,Header("資源")] private GameSoundSO soundAssets;

    protected override void Awake()
    {
        base.Awake();
        soundAssets.InitAssets();
    }

    public void PlaySoundEffect(AudioSource audioSource,SoundAssetsType soundAssetsType)
    {
        audioSource.clip = soundAssets.GetClipAssets(soundAssetsType);
        audioSource.Play();
    }
}

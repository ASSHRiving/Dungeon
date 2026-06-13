using UnityEngine;
using Unity.Cinemachine;

public class GameAssets : SingletonBase<GameAssets>
{
    [SerializeField,Header("資源")] private GameSoundSO soundAssets;
    [SerializeField, Header("相機")] private CinemachineCamera playerTPS;

    private void Awake()
    {
        soundAssets.InitAssets();
    }

    public void PlaySoundEffect(AudioSource audioSource,SoundAssetsType soundAssetsType)
    {
        audioSource.clip = soundAssets.GetClipAssets(soundAssetsType);
        audioSource.Play();
    }
    public void SetUpPlayerCamera(Transform playerTransform)
    {
        if (playerTPS != null)
        {
            playerTPS.Target.TrackingTarget = playerTransform;
            
        }
    }
}

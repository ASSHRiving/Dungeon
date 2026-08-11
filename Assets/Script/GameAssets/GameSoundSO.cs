using UnityEngine;
using System.Collections.Generic;


public enum SoundAssetsType
{
    Hit,
    Sword,
    GSword
}

[CreateAssetMenu(fileName = "SoundAssets", menuName = "CreataAssets/Sound")]
public class GameSoundSO : ScriptableObject
{
    [System.Serializable]
    public class SoundAssetData
    {
        public SoundAssetsType soundType;
        public AudioClip[] assetsClip;
    }
    
    [SerializeField] public List<SoundAssetData> assets = new List<SoundAssetData>();
    private Dictionary<SoundAssetsType, AudioClip[]> assetsDictionary = new Dictionary<SoundAssetsType, AudioClip[]>();

    public void InitAssets()
    {
        assetsDictionary.Clear(); // 確保重新初始化時是乾淨的
        
        for (int i = 0; i < assets.Count; i++)
        {
            if (!assetsDictionary.ContainsKey(assets[i].soundType))
            {
                assetsDictionary.Add(assets[i].soundType, assets[i].assetsClip);
            }
        }
    }
    public AudioClip GetClipAssets(SoundAssetsType soundAssetsType)
    {
        // 💡 關鍵改良：用 TryGetValue 代替直接查 Key，防止找不到時崩潰，並做安全檢查
        if (assetsDictionary.TryGetValue(soundAssetsType, out AudioClip[] clips))
        {
            // 防呆：檢查有沒有漏拖音效檔案
            if (clips != null && clips.Length > 0)
            {
                return clips[Random.Range(0, clips.Length)];
            }
            
            Debug.LogWarning($"[SoundAssets] {soundAssetsType} 欄位裡面沒有任何音效檔案！");
            return null;
        }

        Debug.LogError($"[SoundAssets] 忘記在資料庫中配置 {soundAssetsType} 的音效了！");
        return null;
    }

}

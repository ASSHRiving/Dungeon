using UnityEngine;
using System.IO;

public class TakeIconScreenshot : MonoBehaviour
{
    public KeyCode screenshotKey = KeyCode.P; // 按下 P 鍵截圖
    public string fileName = "EquipmentIcon";

    void Update()
    {
        if (Input.GetKeyDown(screenshotKey))
        {
            TakeSquareScreenshot();
        }
    }

    public void TakeSquareScreenshot()
    {
        Camera cam = GetComponent<Camera>();
        int resWidth = 512;
        int resHeight = 512;

        RenderTexture rt = new RenderTexture(resWidth, resHeight, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        
        Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGBA32, false);
        cam.Render();
        
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
        screenShot.Apply();

        cam.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(rt);

        byte[] bytes = screenShot.EncodeToPNG();
        string path = Path.Combine(Application.dataPath, $"{fileName}_{System.DateTime.Now:yyyyMMdd_HHmmss}.png");
        File.WriteAllBytes(path, bytes);

        Debug.Log($"正方形 Icon 截圖已儲存至: {path}");
        #if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh(); // 自動重新整理 Assets 資料夾
        #endif
    }
}
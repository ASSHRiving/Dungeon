using UnityEngine;

public class CanvasBillboard : MonoBehaviour
{
    private Transform camTransform;

    void Start()
    {
        if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }
    }

    // 必須使用 LateUpdate，確保在敵人移動、轉向以及相機當幀位置更新完畢後，才校正血條角度
    void LateUpdate()
    {
        if (camTransform == null) return;

        // 做法 A（最推薦）：讓血條的朝向與相機的朝向完全平行
        // 這比 transform.LookAt(camTransform) 更好，不會因為敵人在畫面邊緣而產生透視變形或左右反轉
        transform.rotation = camTransform.rotation;
    }
}
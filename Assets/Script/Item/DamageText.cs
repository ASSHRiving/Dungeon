using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.Pool;

public class DamageText : MonoBehaviour
{
    [Header("組件引用")]
    [SerializeField] private TMP_Text textMesh;

    [Header("動畫參數")]
    [SerializeField] private float duration = 0.55f;
    [SerializeField] private float popDuration = 0.08f;     // 極速彈縮時間
    [SerializeField] private float maxPopScale = 2.2f;      // 暴擊/衝擊瞬間放大倍率
    [SerializeField] private float arcHeight = 1.2f;        // 向上弧度

    [Header("漸層色設定")]
    [SerializeField] private TMP_ColorGradient normalGradient;
    [SerializeField] private TMP_ColorGradient critGradient;

    private Transform mainCam;
    private IObjectPool<DamageText> _myPool;

    private void Awake()
    {
        if (textMesh == null) textMesh = GetComponent<TMP_Text>();
        if (Camera.main != null) mainCam = Camera.main.transform;
    }

    public void Setup(float damage, bool isCrit, IObjectPool<DamageText> pool)
    {
        _myPool = pool;
        textMesh.text = Mathf.RoundToInt(damage).ToString();

        // 1. 設置 ARPG 風格漸層與字型大小
        if (isCrit && critGradient != null)
        {
            textMesh.colorGradientPreset = critGradient;
            textMesh.fontSize = 2f;
        }
        else if (normalGradient != null)
        {
            textMesh.colorGradientPreset = normalGradient;
            textMesh.fontSize = 1f;
        }

        StartCoroutine(AnimateRoutine(isCrit));
    }

    private IEnumerator AnimateRoutine(bool isCrit)
    {
        float timer = 0f;
        Vector3 startPos = transform.position;

        // 向左右微幅噴射的隨機偏移量
        Vector3 randomSpread = new Vector3(Random.Range(-0.8f, 0.8f), 0f, Random.Range(-0.8f, 0.8f));
        float currentMaxScale = isCrit ? maxPopScale * 1.3f : maxPopScale;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / duration;

            // --- A. Billboard 永遠對準攝影機 ---
            if (mainCam != null)
            {
                transform.rotation = mainCam.rotation;
            }

            // --- B. 衝撃力彈縮 (Punch Scale) ---
            if (timer < popDuration)
            {
                // 0 -> popDuration 瞬間衝到巨大尺寸
                float popT = timer / popDuration;
                float scale = Mathf.Lerp(0.2f, currentMaxScale, popT);
                transform.localScale = Vector3.one * scale;
            }
            else if (timer < popDuration * 2.5f)
            {
                // 極速回彈至正常大小
                float returnT = (timer - popDuration) / (popDuration * 1.5f);
                float scale = Mathf.Lerp(currentMaxScale, 1f, returnT);
                transform.localScale = Vector3.one * scale;
            }
            else
            {
                transform.localScale = Vector3.one;
            }

            // --- C. 位移軌跡 (Ease Out 上升 + 下落) ---
            // Y 軸使用 Sine 曲線達到拋物停頓感
            float yOffset = Mathf.Sin(t * Mathf.PI) * arcHeight;
            transform.position = startPos + (randomSpread * t) + (Vector3.up * yOffset);

            // --- D. 尾段快速 Fade Out ---
            if (t > 0.6f)
            {
                float fadeT = (t - 0.6f) / 0.4f;
                textMesh.alpha = Mathf.Lerp(1f, 0f, fadeT);
            }

            yield return null;
        }

        // 重置 Alpha 並回收
        textMesh.alpha = 1f;
        if (_myPool != null) _myPool.Release(this);
        else gameObject.SetActive(false);
    }
}

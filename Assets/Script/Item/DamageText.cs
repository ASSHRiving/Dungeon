using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.Pool;

public class DamageText : MonoBehaviour
{
    [Header("組件引用")]
    [SerializeField] private TMP_Text textMesh;

    [Header("動畫參數")]
    [SerializeField] private float moveSpeed = 2.0f;       // 向上飄移速度
    [SerializeField] private float duration = 0.8f;        // 飄字存活時間
    [SerializeField] private Vector3 randomOffset = new Vector3(0.5f, 0.5f, 0f); // 隨機位置偏移

    private Color textColor;
    private Transform mainCameraTransform;
    private IObjectPool<DamageText> _myPool;

    private void Awake()
    {
        if (textMesh == null) textMesh = GetComponent<TMP_Text>();
        if (Camera.main != null) mainCameraTransform = Camera.main.transform;
    }

    /// <summary>
    /// 初始化並播放飄字動畫
    /// </summary>
    public void Setup(float damageAmount, bool isCritical, IObjectPool<DamageText> pool)
    {
        _myPool = pool;

        // 1. 設定文字內容與顏色
        textMesh.text = Mathf.RoundToInt(damageAmount).ToString();

        if (isCritical)
        {
            textMesh.color = Color.yellow;
            transform.localScale = Vector3.one * 1.5f;
        }
        else
        {
            textMesh.color = Color.white;
            transform.localScale = Vector3.one;
        }

        textColor = textMesh.color;

        // 2. 微幅隨機偏移
        Vector3 offset = new Vector3(
            Random.Range(-randomOffset.x, randomOffset.x),
            Random.Range(0f, randomOffset.y),
            Random.Range(-randomOffset.z, randomOffset.z)
        );
        transform.position += offset;

        // 3. 永遠面向攝影機
        if (mainCameraTransform != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - mainCameraTransform.position);
        }

        // 4. 開始動畫 Coroutine
        StartCoroutine(AnimateRoutine());
    }

    private IEnumerator AnimateRoutine()
    {
        float timer = 0f;

        while (timer < duration)
        {
            // 防 Hitstop (頓幀) 卡住：使用 unscaledDeltaTime
            float delta = Time.unscaledDeltaTime;
            timer += delta;

            // 向上移動
            transform.position += Vector3.up * (moveSpeed * delta);

            // 後半段漸漸淡出
            if (timer > duration * 0.5f)
            {
                float alpha = Mathf.Lerp(1f, 0f, (timer - duration * 0.5f) / (duration * 0.5f));
                textMesh.color = new Color(textColor.r, textColor.g, textColor.b, alpha);
            }

            yield return null;
        }

        // 播放結束：釋放回 IObjectPool (會自動觸發 actionOnRelease 並隱藏物件)
        if (_myPool != null)
        {
            _myPool.Release(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}

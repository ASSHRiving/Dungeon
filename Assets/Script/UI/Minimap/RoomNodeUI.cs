using UnityEngine;
using UnityEngine.UI;

public class RoomNodeUI : MonoBehaviour
{
    public Image roomSprite;
    public Image roomBg;

    private RectTransform rectTransform;
    private Image targetBgImage;

    /// <summary>
    /// 此房間節點在小地圖容器中的 UI 座標。
    /// </summary>
    public Vector2 UIPosition { get; private set; }

    /// <summary>
    /// 初始化小地圖房間節點。
    /// </summary>
    public void Initialize(Vector2 uiPos, Sprite iconSprite, Color initialColor)
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        // 優先取自身掛載的 Image（與既有 Prefab 行為完全一致），若無才退回使用 roomBg
        if (targetBgImage == null)
        {
            targetBgImage = GetComponent<Image>();
            if (targetBgImage == null)
            {
                targetBgImage = roomBg;
            }
        }

        UIPosition = uiPos;
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = uiPos;
        }

        if (iconSprite != null && roomSprite != null)
        {
            roomSprite.sprite = iconSprite;
        }

        SetColor(initialColor);
    }

    public void SetColor(Color color)
    {
        if (targetBgImage != null)
        {
            targetBgImage.color = color;
        }
    }

    public void SetVisible(bool visible)
    {
        if (gameObject.activeSelf != visible)
        {
            gameObject.SetActive(visible);
        }
    }
}


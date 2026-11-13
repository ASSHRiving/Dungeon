using UnityEngine;
using UnityEngine.Serialization;
using DG.Tweening;

public class CharacterSelect : MonoBehaviour
{
    [Header("角色配置資產 (ScriptableObject)")]
    [SerializeField] private CharacterDataSO characterData;

    [Header("角色基本設定 (留空時優先讀取 characterData)")]
    [SerializeField] private string characterName = "戰士";
    [SerializeField] private Transform cameraTarget;
    [Tooltip("指定要旋轉的模型 Transform。若留空則預設旋轉本物件")]
    [SerializeField] private Transform modelTransform;

    [Header("角色屬性與介紹 (回退保底，優先由 characterData 驅動)")]
    [SerializeField] private float hp = 100f;
    [SerializeField] private float def = 10f;
    [SerializeField] private float atk = 15f;
    [TextArea(3, 5)]
    [SerializeField] private string description = "擅長近身搏殺，擁有極高的防禦力與生存韌性，是地牢探索中最穩固的前排護盾。";

    public CharacterDataSO CharacterData => characterData;
    public string CharacterName => characterData != null ? characterData.characterName : characterName;
    public Transform CameraTarget => cameraTarget;
    public float HP => characterData != null ? characterData.baseMaxHealth : hp;
    public float DEF => characterData != null ? characterData.baseShield : def;
    public float ATK => characterData != null ? characterData.baseAttackDamage : atk;
    public string Description => characterData != null ? characterData.description : description;

    // 向下相容屬性 (若有外部舊程式碼引用)
    public int id => characterData != null ? characterData.characterID : 0;

    private Quaternion initialRotation;
    private Tween resetRotationTween;

    private Transform TargetTransform => modelTransform != null ? modelTransform : transform;

    private void Awake()
    {
        initialRotation = TargetTransform.rotation;

        // 大廳展示時關閉 Animator 的 root motion，避免動作裁切或覆蓋手動旋轉
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.applyRootMotion = false;
        }
    }

    private void OnDestroy()
    {
        resetRotationTween?.Kill();
    }

    /// <summary>
    /// 左右旋轉角色模型 (繞世界 Y 軸旋轉，支援 360 度無限制旋轉)
    /// </summary>
    /// <param name="deltaAngle">旋轉角度增量 (度)</param>
    public void RotateModel(float deltaAngle)
    {
        resetRotationTween?.Kill();
        TargetTransform.Rotate(Vector3.up, deltaAngle, Space.World);
    }

    /// <summary>
    /// 平滑重置角色旋轉至初始朝向
    /// </summary>
    /// <param name="duration">回正動畫時間 (秒)</param>
    public void ResetRotation(float duration = 0.35f)
    {
        resetRotationTween?.Kill();
        if (duration <= 0f)
        {
            TargetTransform.rotation = initialRotation;
            return;
        }

        // 使用 RotateMode.Fast 取最短旋轉路徑平滑轉回初始朝向
        resetRotationTween = TargetTransform.DORotate(initialRotation.eulerAngles, duration, RotateMode.Fast)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    public void Select()
    {
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.SelectCharacter(this);
        }
    }
}

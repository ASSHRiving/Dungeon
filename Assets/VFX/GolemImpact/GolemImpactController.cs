using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// 巨石兵震地特效控制器：
/// 控制生成時的 Camera Shake 震屏力度，以及光源和碎裂冷卻衰減。
/// </summary>
public class GolemImpactController : MonoBehaviour
{
    [Header("=== 強度與 Cinemachine Impulse 設定 ===")]
    public bool isMajorSlam = true;          // 勾選=槌地(重震)；取消勾選=踩踏(輕震)
    public CinemachineImpulseSource impulseSource;
    public float slamForce = 1.2f;           // 槌地 Impulse 力道倍率
    public float stompForce = 0.45f;         // 踩踏 Impulse 力道倍率

    private ParticleSystem[] _systems;
    private float _timer;

    private void Awake()
    {
        _systems = GetComponentsInChildren<ParticleSystem>(true);
    }

    private void OnEnable()
    {
        _timer = 0f;

        // 播所有粒子
        if (_systems != null)
        {
            for (int i = 0; i < _systems.Length; i++)
            {
                _systems[i].Clear(true);
                _systems[i].Play(true);
            }
        }

        // 觸發震屏
        if (impulseSource != null)
        {
            float force = isMajorSlam ? slamForce : stompForce;
            impulseSource.GenerateImpulseWithForce(force);
        }
    }

    private void Update()
    {
        _timer += Time.deltaTime;

        // 2秒後自然銷毀
        if (_timer >= 2.0f)
        {
            Destroy(gameObject);
        }
    }
}
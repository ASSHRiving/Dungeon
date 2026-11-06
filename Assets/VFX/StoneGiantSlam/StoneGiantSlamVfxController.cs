using UnityEngine;
using System.Reflection;

namespace Game.VFX
{
    /// <summary>
    /// 專供 Unity Animation 視窗直接 K 幀 (開關 GameObject Active) 使用的特效控制器。
    /// 無須物件池與程式碼呼叫，啟用瞬間即自動清空殘留並爆發所有特效層級。
    /// </summary>
    [ExecuteAlways]
    public class StoneGiantSlamVfxController : MonoBehaviour
    {
        [Header("Light Flash Settings")]
        [SerializeField] private Light impactLight;
        [SerializeField] private float maxLightIntensity = 12f;
        [SerializeField] private float lightDuration = 0.45f;
        [SerializeField] private AnimationCurve lightFalloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [Header("Camera Shake (Cinemachine Impulse)")]
        [SerializeField] private float impulseForce = 1.8f;

        private ParticleSystem[] _particleSystems;
        private TrailRenderer[] _trailRenderers;
        private float _elapsedTime;

        // 透過反射快取 CinemachineImpulseSource，避免無套件時報錯
        private Component _impulseSource;
        private MethodInfo _generateImpulseMethod;

        private void Awake()
        {
            CacheComponents();
        }

        private void CacheComponents()
        {
            if (_particleSystems == null || _particleSystems.Length == 0)
                _particleSystems = GetComponentsInChildren<ParticleSystem>(true);

            if (_trailRenderers == null || _trailRenderers.Length == 0)
                _trailRenderers = GetComponentsInChildren<TrailRenderer>(true);

            if (impactLight == null)
                impactLight = GetComponentInChildren<Light>(true);

            if (_impulseSource == null)
            {
                _impulseSource = GetComponent("CinemachineImpulseSource");
                if (_impulseSource != null)
                {
                    _generateImpulseMethod = _impulseSource.GetType().GetMethod(
                        "GenerateImpulseWithForce",
                        new[] { typeof(float) }
                    );
                }
            }
        }

        private void OnEnable()
        {
            CacheComponents();
            _elapsedTime = 0f;

            // 1. 清除所有 TrailRenderer 殘影
            if (_trailRenderers != null)
            {
                for (int i = 0; i < _trailRenderers.Length; i++)
                {
                    if (_trailRenderers[i] != null)
                        _trailRenderers[i].Clear();
                }
            }

            // 2. 重置並立即播放所有子粒子系統 (支援 Animation 視窗時間軸預覽)
            if (_particleSystems != null)
            {
                for (int i = 0; i < _particleSystems.Length; i++)
                {
                    if (_particleSystems[i] != null)
                    {
                        _particleSystems[i].Clear(true);
                        _particleSystems[i].Play(true);
                    }
                }
            }

            // 3. 點亮瞬間爆發點光源
            if (impactLight != null)
            {
                impactLight.enabled = true;
                impactLight.intensity = maxLightIntensity;
            }

            // 4. 在 Play 模式下觸發 Cinemachine 震屏
            if (Application.isPlaying && _impulseSource != null && _generateImpulseMethod != null)
            {
                _generateImpulseMethod.Invoke(_impulseSource, new object[] { impulseForce });
            }
        }

        private void OnDisable()
        {
            // 當動畫切換或關鍵影格關閉物件時，清空所有殘留粒子與光源
            if (_particleSystems != null)
            {
                for (int i = 0; i < _particleSystems.Length; i++)
                {
                    if (_particleSystems[i] != null)
                        _particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }

            if (_trailRenderers != null)
            {
                for (int i = 0; i < _trailRenderers.Length; i++)
                {
                    if (_trailRenderers[i] != null)
                        _trailRenderers[i].Clear();
                }
            }

            if (impactLight != null)
            {
                impactLight.intensity = 0f;
                impactLight.enabled = false;
            }
        }

        private void Update()
        {
            _elapsedTime += Time.deltaTime;

            // 平滑衰減點光源亮度
            if (impactLight != null && impactLight.enabled)
            {
                if (_elapsedTime < lightDuration)
                {
                    float t = Mathf.Clamp01(_elapsedTime / Mathf.Max(0.001f, lightDuration));
                    impactLight.intensity = maxLightIntensity * lightFalloff.Evaluate(t);
                }
                else
                {
                    impactLight.intensity = 0f;
                    impactLight.enabled = false;
                }
            }
        }
    }
}
using UnityEngine;
using System.Reflection;

namespace Game.VFX
{
    /// <summary>
    /// 專供 Unity Animation 視窗直接 K 幀 (開關 GameObject Active) 使用的特效控制器。
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

        private ParticleSystem[] particleSystems;
        private float elapsedTime;

        // 透過反射快取 CinemachineImpulseSource，避免無套件時報錯
        private Component impulseSource;
        private MethodInfo generateImpulseMethod;

        private void Awake()
        {
            CacheComponents();
        }

        private void CacheComponents()
        {
            if (particleSystems == null || particleSystems.Length == 0)
                particleSystems = GetComponentsInChildren<ParticleSystem>(true);


            if (impactLight == null)
                impactLight = GetComponentInChildren<Light>(true);

            if (impulseSource == null)
            {
                impulseSource = GetComponent("CinemachineImpulseSource");
                if (impulseSource != null)
                {
                    generateImpulseMethod = impulseSource.GetType().GetMethod(
                        "GenerateImpulseWithForce",
                        new[] { typeof(float) }
                    );
                }
            }
        }

        private void OnEnable()
        {
            CacheComponents();
            elapsedTime = 0f;

            // 2. 重置並立即播放所有子粒子系統 (支援 Animation 視窗時間軸預覽)
            if (particleSystems != null)
            {
                for (int i = 0; i < particleSystems.Length; i++)
                {
                    if (particleSystems[i] != null)
                    {
                        particleSystems[i].Clear(true);
                        particleSystems[i].Play(true);
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
            if (Application.isPlaying && impulseSource != null && generateImpulseMethod != null)
            {
                generateImpulseMethod.Invoke(impulseSource, new object[] { impulseForce });
            }
        }

        private void OnDisable()
        {
            // 當動畫切換或關鍵影格關閉物件時，清空所有殘留粒子與光源
            if (particleSystems != null)
            {
                for (int i = 0; i < particleSystems.Length; i++)
                {
                    if (particleSystems[i] != null)
                        particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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
            elapsedTime += Time.deltaTime;

            // 平滑衰減點光源亮度
            if (impactLight != null && impactLight.enabled)
            {
                if (elapsedTime < lightDuration)
                {
                    float t = Mathf.Clamp01(elapsedTime / Mathf.Max(0.001f, lightDuration));
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
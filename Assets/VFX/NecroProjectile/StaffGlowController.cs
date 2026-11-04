using UnityEngine;

/// <summary>
/// 法杖頂端施法發光特效控制器：
/// 當在 Animation 視窗透過關鍵幀將此物件 SetActive(true) 時，自動重置粒子並平滑淡入點光源。
/// </summary>
public class StaffGlowController : MonoBehaviour
{
    [Header("=== 點光源呼吸淡入設定 ===")]
    public Light staffLight;
    public float maxLightIntensity = 3.5f;
    public float fadeInSpeed = 8.0f;
    public float pulseFrequency = 6.0f;
    public float pulseAmplitude = 0.6f;

    [Header("=== 選配：同步點亮法杖水晶 Mesh 材質 ===")]
    public Renderer staffCrystalMeshRenderer;
    [ColorUsage(true, true)]
    public Color activeEmissionColor = new Color(1.8f, 0.3f, 3.2f, 1f);

    private ParticleSystem[] _particleSystems;
    private MaterialPropertyBlock _mpb;
    private float _activeTimer;
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        _mpb = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        _activeTimer = 0f;

        // 每次動畫觸發開啟時，重置所有子粒子確保演出一致
        if (_particleSystems != null)
        {
            for (int i = 0; i < _particleSystems.Length; i++)
            {
                _particleSystems[i].Clear(true);
                _particleSystems[i].Play(true);
            }
        }

        if (staffLight != null)
        {
            staffLight.intensity = 0f;
        }
    }

    private void Update()
    {
        _activeTimer += Time.deltaTime;

        if (staffLight != null)
        {
            float baseIntensity = Mathf.MoveTowards(staffLight.intensity, maxLightIntensity, fadeInSpeed * Time.deltaTime);
            float pulse = Mathf.Sin(_activeTimer * pulseFrequency) * pulseAmplitude;
            staffLight.intensity = Mathf.Max(0f, baseIntensity + pulse);
        }

        if (staffCrystalMeshRenderer != null)
        {
            float glowFactor = Mathf.Clamp01(_activeTimer * fadeInSpeed / Mathf.Max(0.1f, maxLightIntensity));
            staffCrystalMeshRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(EmissionColorId, activeEmissionColor * glowFactor);
            _mpb.SetColor(BaseColorId, activeEmissionColor * glowFactor);
            staffCrystalMeshRenderer.SetPropertyBlock(_mpb);
        }
    }

    private void OnDisable()
    {
        // 當動畫結束關閉特效時，自動將法杖水晶本體還原
        if (staffCrystalMeshRenderer != null && _mpb != null)
        {
            staffCrystalMeshRenderer.GetPropertyBlock(_mpb);
            _mpb.Clear();
            staffCrystalMeshRenderer.SetPropertyBlock(_mpb);
        }
    }
}
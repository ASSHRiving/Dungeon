using System.Collections.Generic;
using UnityEngine;

public class RoyalSwordVFXController : MonoBehaviour
{
    [Header("=== 渲染器 (若留空會自動抓取子物件 SkinnedMeshRenderer) ===")]
    [SerializeField] private Renderer swordRenderer;
    [SerializeField] private Light gemPointLight;

    [Header("=== 護手寶石 (Mat_HolyGem) 雙色呼吸設定 ===")]
    [SerializeField] private Color gemDimColor = new Color(0.0f, 0.22f, 0.45f);    // 呼吸低谷：深海藍 (#003873)
    [SerializeField] private Color gemBrightColor = new Color(0.0f, 0.95f, 1.0f);  // 呼吸波峰：青藍聖光 (#00F2FF)
    [SerializeField] private float gemMinEmission = 0.2f;                          // 低谷發光強度 (低於1才會明顯變暗)
    [SerializeField] private float gemMaxEmission = 2.8f;                          // 波峰發光強度

    [Header("=== 劍身符文 (Mat_GoldenRune) 雙色呼吸設定 ===")]
    [SerializeField] private Color runeDimColor = new Color(0.45f, 0.16f, 0.0f);   // 呼吸低谷：深邃暗珀紅金 (#732900)
    [SerializeField] private Color runeBrightColor = new Color(1.0f, 0.88f, 0.15f);// 呼吸波峰：耀眼燦金 (#FFE026)
    [SerializeField] private Color runeSlashColor = new Color(0.5f, 1.0f, 1.0f);   // 揮砍爆發瞬間：轉為青白聖火色
    [SerializeField] private float runeMinEmission = 0.15f;                        // 低谷發光強度
    [SerializeField] private float runeMaxEmission = 3.2f;                         // 波峰發光強度

    [Header("=== 點光源防泛綠保護色 ===")]
    [SerializeField] private Color pointLightColor = new Color(0.65f, 0.92f, 1.00f);// 冰藍偏白 (防止黃金護手泛綠)
    [SerializeField] private float lightMinIntensity = 0.8f;
    [SerializeField] private float lightMaxIntensity = 1.8f;

    [Header("=== 呼吸頻率與相位差 ===")]
    [SerializeField] private float pulseFrequency = 3.2f; // 呼吸速度 (數值越大閃爍越快)
    [SerializeField] private float phaseOffset = 1.2f;    // 寶石與劍身符文的能量傳導時間差

    [Header("=== 攻擊爆發與刀光組件 ===")]
    [SerializeField] private float slashOverdriveMultiplier = 2.5f;
    [SerializeField] private TrailRenderer goldEdgeTrail;
    [SerializeField] private TrailRenderer cyanCoreTrail;
    [SerializeField] private ParticleSystem gemCoreParticles;
    [SerializeField] private ParticleSystem slashSparkParticles;

    [Header("=== 護手寶石動態強化 (免回 Blender) ===")]
    [SerializeField] private Transform floatingGemBone;       // 拖入 Bone_FloatingGem 骨骼
    [SerializeField] private float ExtraBobAmplitude = 0.0001f; // 額外增加 ±1cm 懸浮感
    [SerializeField] private float ExtraBobFrequency = 2.5f;  // 懸浮頻率
    [SerializeField] private float ExtraTiltAngle = 18f;      // 前後左右微傾搖擺角度
    [SerializeField] private float ScalePulseAmount = 0.15f;  // 尺寸呼吸縮放 (±15%)

    // 內部自動配對的材質球實例列表
    private readonly List<Material> _gemMaterials = new List<Material>();
    private readonly List<Material> _runeMaterials = new List<Material>();

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private Vector3 _gemInitialLocalPos;
    private Vector3 _gemInitialLocalScale;
    private bool _gemInitialized = false;

    private float _currentOverdrive = 1.0f;
    private float _targetOverdrive = 1.0f;

    private void Awake()
    {
        // 1. 若未手動拖入 Renderer，自動搜尋子物件中的 SkinnedMeshRenderer 或 MeshRenderer
        if (swordRenderer == null)
        {
            swordRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            if (swordRenderer == null) swordRenderer = GetComponentInChildren<Renderer>();
        }

        // 2. 自動依據材質名稱 ("HolyGem" / "GoldenRune") 抓取正確插槽並開啟 _EMISSION
        if (swordRenderer != null)
        {
            Material[] mats = swordRenderer.materials; // 取得材質實例以支援動態開啟 Keyword
            for (int i = 0; i < mats.Length; i++)
            {
                Material m = mats[i];
                if (m == null) continue;

                string matName = m.name.ToLower();
                if (matName.Contains("holygem") || matName.Contains("gem"))
                {
                    m.EnableKeyword("_EMISSION");
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    _gemMaterials.Add(m);
                }
                else if (matName.Contains("goldenrune") || matName.Contains("rune"))
                {
                    m.EnableKeyword("_EMISSION");
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    _runeMaterials.Add(m);
                }
            }

            Debug.Log($"<color=#00E5FF>[RoyalSword VFX]</color> 成功綁定材質：找到 {_gemMaterials.Count} 個寶石材質、{_runeMaterials.Count} 個符文材質！");
        }
        else
        {
            Debug.LogError("[RoyalSword VFX] 找不到任何 Renderer，請確認腳本掛在武器物件上！");
        }

        ToggleSlashVFX(false);
    }

    private void Update()
    {
        // 平滑過渡揮砍時的聖光爆發倍率
        _currentOverdrive = Mathf.Lerp(_currentOverdrive, _targetOverdrive, Time.deltaTime * 10f);
        float overdrive01 = Mathf.InverseLerp(1.0f, slashOverdriveMultiplier, _currentOverdrive);

        // 將正弦波 (-1 ~ +1) 轉換為 0.0 ~ 1.0 的呼吸比例
        float timePhase = Time.time * pulseFrequency;
        float gemWave01 = Mathf.Sin(timePhase) * 0.5f + 0.5f;
        float runeWave01 = Mathf.Sin(timePhase - phaseOffset) * 0.5f + 0.5f;

        // 1. 計算護手寶石顏色與發光強度 (在深海藍與青藍聖光之間平滑變色)
        Color currentGemBase = Color.Lerp(gemDimColor, gemBrightColor, gemWave01);
        float currentGemEmission = Mathf.Lerp(gemMinEmission, gemMaxEmission, gemWave01) * _currentOverdrive;
        Color finalGemEmission = currentGemBase * currentGemEmission;

        for (int i = 0; i < _gemMaterials.Count; i++)
        {
            Material m = _gemMaterials[i];
            m.SetColor(BaseColorId, currentGemBase);
            m.SetColor(ColorId, currentGemBase);
            m.SetColor(EmissionColorId, finalGemEmission);
        }

        // 2. 計算劍身符文顏色與發光強度 (在暗珀金與耀眼燦金之間變色，揮砍時融入青白聖火)
        Color currentRuneBase = Color.Lerp(runeDimColor, runeBrightColor, runeWave01);
        currentRuneBase = Color.Lerp(currentRuneBase, runeSlashColor, overdrive01 * 0.6f);

        float currentRuneEmission = Mathf.Lerp(runeMinEmission, runeMaxEmission, runeWave01) * _currentOverdrive;
        Color finalRuneEmission = currentRuneBase * currentRuneEmission;

        for (int i = 0; i < _runeMaterials.Count; i++)
        {
            Material m = _runeMaterials[i];
            m.SetColor(BaseColorId, currentRuneBase);
            m.SetColor(ColorId, currentRuneBase);
            m.SetColor(EmissionColorId, finalRuneEmission);
        }
        if (gemPointLight != null)
        {
            gemPointLight.color = pointLightColor;
            gemPointLight.intensity = Mathf.Lerp(lightMinIntensity, lightMaxIntensity, gemWave01);
        }
    }

    private void LateUpdate()
    {
        if (floatingGemBone == null) return;

        if (!_gemInitialized)
        {
            _gemInitialLocalPos = floatingGemBone.localPosition;
            _gemInitialLocalScale = floatingGemBone.localScale;
            _gemInitialized = true;
        }

        float t = Time.time * ExtraBobFrequency;

        // 1. 放大懸浮位移 (使用基準座標加上偏移，防止未開 Animator 時飛走)
        Vector3 bobOffset = new Vector3(0f, 0f, Mathf.Sin(t) * ExtraBobAmplitude);
        floatingGemBone.localPosition = _gemInitialLocalPos + bobOffset;

        // 2. 增加雙軸向星體傾斜翻滾 + 加速自轉
        Quaternion extraTilt = Quaternion.Euler(
            Mathf.Sin(t * 0.8f) * ExtraTiltAngle,
            Time.time * 120f,
            Mathf.Cos(t * 0.8f) * ExtraTiltAngle
        );
        floatingGemBone.localRotation *= extraTilt;

        // 3. 尺寸呼吸縮放
        float scaleMultiplier = 1.0f + Mathf.Sin(t * 1.5f) * ScalePulseAmount;
        floatingGemBone.localScale = _gemInitialLocalScale * scaleMultiplier;
    }

    public void EnableSlashVFX()
    {
        _targetOverdrive = slashOverdriveMultiplier;
        ToggleSlashVFX(true);
        if (gemCoreParticles != null) gemCoreParticles.Emit(15);
    }

    public void DisableSlashVFX()
    {
        _targetOverdrive = 1.0f;
        ToggleSlashVFX(false);
    }

    private void ToggleSlashVFX(bool state)
    {
        if (goldEdgeTrail != null) goldEdgeTrail.emitting = state;
        if (cyanCoreTrail != null) cyanCoreTrail.emitting = state;
        if (slashSparkParticles != null)
        {
            if (state) slashSparkParticles.Play();
            else slashSparkParticles.Stop();
        }
    }
}
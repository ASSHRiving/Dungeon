using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 瞬移黑紫煙霧特效控制器：驅動瞬移瞬間的紫光閃爍衰減與物件池重置
/// </summary>
public class TeleportSmokeVfxController : MonoBehaviour
{
    public Light flashLight;
    public float initialLightIntensity = 4.5f;
    public float lightDecaySpeed = 10.0f;

    private ParticleSystem[] _systems;
    private IObjectPool<TeleportSmokeVfxController> _pool;
    private float _elapsed;

    private void Awake()
    {
        _systems = GetComponentsInChildren<ParticleSystem>(true);
    }

    public void SetPool(IObjectPool<TeleportSmokeVfxController> pool) => _pool = pool;

    private void OnEnable()
    {
        _elapsed = 0f;
        if (flashLight != null)
        {
            flashLight.intensity = initialLightIntensity;
            flashLight.enabled = true;
        }

        if (_systems != null)
        {
            for (int i = 0; i < _systems.Length; i++)
            {
                _systems[i].Clear(true);
                _systems[i].Play(true);
            }
        }
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;

        if (flashLight != null && flashLight.enabled)
        {
            flashLight.intensity = Mathf.MoveTowards(flashLight.intensity, 0f, lightDecaySpeed * Time.deltaTime);
            if (flashLight.intensity <= 0.01f)
            {
                flashLight.enabled = false;
            }
        }

        if (_pool != null && _elapsed >= 1.5f)
        {
            _pool.Release(this);
        }
    }
}
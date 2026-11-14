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

    private ParticleSystem[] systems;
    private IObjectPool<TeleportSmokeVfxController> pool;
    private float elapsed;

    private void Awake()
    {
        systems = GetComponentsInChildren<ParticleSystem>(true);
    }

    public void SetPool(IObjectPool<TeleportSmokeVfxController> pool) => this.pool = pool;

    private void OnEnable()
    {
        elapsed = 0f;
        if (flashLight != null)
        {
            flashLight.intensity = initialLightIntensity;
            flashLight.enabled = true;
        }

        if (systems != null)
        {
            for (int i = 0; i < systems.Length; i++)
            {
                systems[i].Clear(true);
                systems[i].Play(true);
            }
        }
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        if (flashLight != null && flashLight.enabled)
        {
            flashLight.intensity = Mathf.MoveTowards(flashLight.intensity, 0f, lightDecaySpeed * Time.deltaTime);
            if (flashLight.intensity <= 0.01f)
            {
                flashLight.enabled = false;
            }
        }

        if (pool != null && elapsed >= 1.5f)
        {
            pool.Release(this);
        }
    }
}
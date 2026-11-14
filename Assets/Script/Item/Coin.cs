using UnityEngine;
using UnityEngine.Pool;

public class Coin : MonoBehaviour
{
    [SerializeField, Header("靠近多少距離開始吸附")] private float pickupRadius = 4f;
    [SerializeField, Header("吸附飛行的基本速度")] private float flySpeed = 8f;
    [SerializeField, Header("飛行加速度")] private float accel = 15f;
    [SerializeField, Header("拾取音效")] private AudioClip clip;

    private Transform playerTransform;
    private bool isFlying = false;
    private float currentSpeed;

    private float spawnTime;
    private float pickupDelay = 0.6f;

    private Rigidbody rb;
    private Collider col;
    private IObjectPool<GameObject> myPool;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    public void SetPool(IObjectPool<GameObject> pool)
    {
        myPool = pool;
    }

    private void OnEnable()
    {
        isFlying = false;
        currentSpeed = flySpeed;
        spawnTime = Time.time;

        if (rb != null) rb.isKinematic = false;
        if (col != null) col.isTrigger = false;

        if (playerTransform == null)
        {
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null) playerTransform = playerGO.transform;
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;
        if (Time.time < spawnTime + pickupDelay) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (distance <= pickupRadius && !isFlying)
        {
            isFlying = true;
            if (rb != null) rb.isKinematic = true;
            if (col != null) col.isTrigger = true;
        }

        if (isFlying)
        {
            currentSpeed += accel * Time.deltaTime;
            Vector3 targetPosition = playerTransform.position;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, currentSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.3f)
            {
                CollectCoin(playerTransform.gameObject);
            }
        }
    }

    private void CollectCoin(GameObject player)
    {
        AudioSource audioSource = player.GetComponentInChildren<AudioSource>();
        if(audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, 5f);
        }
        if (player.TryGetComponent<PlayerBalanceSystem>(out PlayerBalanceSystem playerBalance))
        {
            playerBalance.AddCoin(1);
        }

        if (myPool != null)
        {
            myPool.Release(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void DropCoin()
    {
        if (transform.TryGetComponent<Rigidbody>(out Rigidbody coinRb))
        {
            Vector3 force = new Vector3(Random.Range(-2f, 2f), 4f, Random.Range(-2f, 2f));
            coinRb.AddForce(force, ForceMode.Impulse);
        }
    }

    /*
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
    */
}
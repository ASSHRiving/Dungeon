using UnityEngine;
using UnityEngine.Pool;

public class Coin : MonoBehaviour
{
    [SerializeField, Header("靠近多少距離開始吸附")] private float pickupRadius = 4f;
    [SerializeField, Header("吸附飛行的基本速度")] private float flySpeed = 8f;
    [SerializeField, Header("飛行加速度")] private float accel = 15f;

    private Transform _playerTransform;
    private bool _isFlying = false;
    private float _currentSpeed;

    private float _spawnTime;
    private float pickupDelay = 0.6f;

    private Rigidbody _rb;
    private Collider _col;
    private IObjectPool<GameObject> _myPool;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();
    }

    public void SetPool(IObjectPool<GameObject> pool)
    {
        _myPool = pool;
    }

    private void OnEnable()
    {
        _isFlying = false;
        _currentSpeed = flySpeed;
        _spawnTime = Time.time;

        if (_rb != null) _rb.isKinematic = false;
        if (_col != null) _col.isTrigger = false;

        if (_playerTransform == null)
        {
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null) _playerTransform = playerGO.transform;
        }
    }

    private void Update()
    {
        if (_playerTransform == null) return;
        if (Time.time < _spawnTime + pickupDelay) return;

        float distance = Vector3.Distance(transform.position, _playerTransform.position);

        if (distance <= pickupRadius && !_isFlying)
        {
            _isFlying = true;
            if (_rb != null) _rb.isKinematic = true;
            if (_col != null) _col.isTrigger = true;
        }

        if (_isFlying)
        {
            _currentSpeed += accel * Time.deltaTime;
            Vector3 targetPosition = _playerTransform.position;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, _currentSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.3f)
            {
                CollectCoin(_playerTransform.gameObject);
            }
        }
    }

    private void CollectCoin(GameObject player)
    {
        if (player.TryGetComponent<PlayerBalanceSystem>(out PlayerBalanceSystem playerBalance))
        {
            playerBalance.AddCoin(1);
        }

        if (_myPool != null)
        {
            _myPool.Release(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
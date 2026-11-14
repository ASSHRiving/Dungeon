using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class Chest : MonoBehaviour, IInteractable
{
    [SerializeField] public string interactableName = "Chest";
    string IInteractable.interactableName => interactableName;
    [SerializeField] private Transform dropPoint;

    [Header("獎勵池設定 (優先使用 ScriptableObject)")]
    [SerializeField] private ChestRewardPoolSO chestPool;

    [Header("舊版手動設定 (未指派 ChestPool 時作為備援)")]
    [SerializeField] private List<GameObject> itemList;
    [SerializeField] private int gold;
    private GameObject itemPrefab;

    [Header("彈射力道")]
    [SerializeField] private float upForce;
    [SerializeField] private float forwardForce;
    private bool isOpen = false;
    private Animator animator;
    [Header("音效設定")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openSound;

    private void Awake()
    {
        if (itemList != null && itemList.Count > 0)
        {
            itemPrefab = itemList[Random.Range(0, itemList.Count)];
        }
        animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    public void Interact(Transform player)
    {
        if (isOpen) return;

        isOpen = true;
        if (animator != null)
        {
            animator.SetBool("IsOpen", true);
        }
        if (audioSource != null && openSound != null)
        {
            audioSource.PlayOneShot(openSound, 3f);
        }
        StartCoroutine(Wait());
        foreach (Transform child in transform)
        {
            child.gameObject.layer = LayerMask.NameToLayer("UnInteractable");
        }
        gameObject.layer = LayerMask.NameToLayer("UnInteractable");
    }

    private void DropItem()
    {
        Transform spawnOrigin = dropPoint != null ? dropPoint : transform;

        if (chestPool != null)
        {
            int currentLevel = GameManager.Instance != null ? GameManager.Instance.currentLevel : 1;
            SpawnCoins(chestPool.RollGold(currentLevel), spawnOrigin);

            List<ChestRewardEntry> drops = chestPool.RollDrops(currentLevel);
            for (int i = 0; i < drops.Count; i++)
            {
                ChestRewardEntry entry = drops[i];
                if (entry == null || entry.itemPrefab == null) continue;

                GameObject itemGO = Instantiate(entry.itemPrefab, spawnOrigin.position, spawnOrigin.rotation);
                if (itemGO.TryGetComponent<Equipment>(out var equipment))
                {
                    int tierToSet = entry.targetTier >= 0
                        ? entry.targetTier
                        : chestPool.RollEquipmentTier(currentLevel);
                    equipment.SetIndex(tierToSet);
                }
                LaunchItem(itemGO, spawnOrigin);
            }
            return;
        }

        SpawnCoins(gold, spawnOrigin);
        if (itemPrefab != null)
        {
            GameObject itemGO = Instantiate(itemPrefab, spawnOrigin.position, spawnOrigin.rotation);
            LaunchItem(itemGO, spawnOrigin);
        }
    }

    private void SpawnCoins(int coinCount, Transform spawnOrigin)
    {
        for (int i = 0; i < coinCount; i++)
        {
            Vector3 randomOffset = new Vector3(Random.Range(-0.5f, 0.5f), 0.5f, Random.Range(-0.5f, 0.5f));
            GameObject coin = ObjectPoolManager.Instance.GetCoin(spawnOrigin.position + randomOffset, Quaternion.identity);

            if (coin != null && coin.TryGetComponent<Rigidbody>(out Rigidbody coinRb))
            {
                coinRb.linearVelocity = Vector3.zero;
                coinRb.angularVelocity = Vector3.zero;
                Vector3 force = new Vector3(Random.Range(-2f, 2f), 4f, Random.Range(-2f, 2f));
                coinRb.AddForce(force, ForceMode.Impulse);
            }
        }
    }

    private void LaunchItem(GameObject itemGO, Transform spawnOrigin)
    {
        if (itemGO == null) return;

        Rigidbody rb = itemGO.GetComponentInChildren<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;

            float randomRightForce = Random.Range(-1.5f, 1.5f);
            Vector3 dir = (spawnOrigin.up * upForce) + (spawnOrigin.forward * forwardForce) + (spawnOrigin.right * randomRightForce);
            rb.AddForce(dir, ForceMode.Impulse);
            float randomTorque = Random.Range(-0.05f, 0.05f);
            rb.AddTorque(new Vector3(randomTorque, randomTorque, randomTorque), ForceMode.Impulse);
        }
    }

    private IEnumerator Wait()
    {
        yield return new WaitForSeconds(0.2f);
        DropItem();
    }
}

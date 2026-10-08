using UnityEngine;

public class PickupSpawner : MonoBehaviour
{
    [Header("生成区域")]
    public Vector2 areaMin = new Vector2(-15f, -1f);
    public Vector2 areaMax = new Vector2(15f, 14f);

    [Header("生成规则")]
    public int initialCount = 3;
    public float spawnInterval = 8f;
    public int maxPickups = 5;
    public int maxAttempts = 30;

    [Header("落地检测")]
    public LayerMask groundLayer;
    public float spawnOffsetY = 0.7f;
    public float checkRadius = 0.25f;
    public float minDistanceToOtherPickup = 2.0f;

    private float spawnTimer;

    private void Start()
    {
        if (PickupPoolManager.Instance == null)
        {
            Debug.LogError("[Spawner] 场景里没有 PickupPoolManager！");
            return;
        }

        for (int i = 0; i < initialCount; i++)
            TrySpawnOne();

        spawnTimer = spawnInterval;
    }

    private void Update()
    {
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying)
            return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f) return;
        spawnTimer = spawnInterval;

        if (FindObjectsOfType<Pickup>().Length < maxPickups)
            TrySpawnOne();
    }

    private void TrySpawnOne()
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            float x = Random.Range(areaMin.x, areaMax.x);
            Vector2 rayStart = new Vector2(x, areaMax.y);
            float rayLength = areaMax.y - areaMin.y;

            RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, rayLength, groundLayer);
            if (hit.collider == null) continue;

            Vector2 spawnPos = hit.point + Vector2.up * spawnOffsetY;

            if (!IsPositionValid(spawnPos)) continue;

            PickupType type = (PickupType)Random.Range(0, 3);
            PickupPoolManager.Instance.Spawn(type, spawnPos);
            return;
        }

        Debug.LogWarning("[Spawner] 30 次尝试都没找到合法位置。");
    }

    private bool IsPositionValid(Vector2 pos)
    {
        float safeRadius = Mathf.Min(checkRadius, spawnOffsetY * 0.9f);
        if (Physics2D.OverlapCircle(pos, safeRadius, groundLayer)) return false;

        foreach (var p in FindObjectsOfType<Pickup>())
            if (Vector2.Distance(p.transform.position, pos) < minDistanceToOtherPickup)
                return false;

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Vector3 c = new Vector3((areaMin.x + areaMax.x) * 0.5f, (areaMin.y + areaMax.y) * 0.5f, 0f);
        Vector3 s = new Vector3(areaMax.x - areaMin.x, areaMax.y - areaMin.y, 0f);
        Gizmos.DrawWireCube(c, s);
    }
}
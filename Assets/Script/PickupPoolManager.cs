using UnityEngine;

/// <summary>
/// 道具池管理器（单例）
/// 挂在场景里一个空物体上（比如叫 PickupPoolManager）
/// </summary>
public class PickupPoolManager : MonoBehaviour
{
    public static PickupPoolManager Instance { get; private set; }

    [Header("道具预制体（拖入你的 3 个）")]
    public GameObject stunPrefab;
    public GameObject doubleJumpPrefab;
    public GameObject shieldPrefab;

    [Header("池初始大小")]
    public int initialPoolSize = 5;

    private SimplePool stunPool;
    private SimplePool doubleJumpPool;
    private SimplePool shieldPool;
    private Transform poolRoot;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        poolRoot = new GameObject("_PickupPools").transform;

        if (stunPrefab != null)
            stunPool = new SimplePool(stunPrefab, initialPoolSize, poolRoot);
        else
            Debug.LogError("[Pool] stunPrefab 没赋值！");

        if (doubleJumpPrefab != null)
            doubleJumpPool = new SimplePool(doubleJumpPrefab, initialPoolSize, poolRoot);
        else
            Debug.LogError("[Pool] doubleJumpPrefab 没赋值！");

        if (shieldPrefab != null)
            shieldPool = new SimplePool(shieldPrefab, initialPoolSize, poolRoot);
        else
            Debug.LogError("[Pool] shieldPrefab 没赋值！");
    }

    public GameObject Spawn(PickupType type, Vector3 position)
    {
        var pool = GetPool(type);
        if (pool == null) return null;
        return pool.Get(position);
    }

    public void Return(PickupType type, GameObject obj)
    {
        var pool = GetPool(type);
        if (pool == null) { Destroy(obj); return; }
        pool.Return(obj);
    }

    private SimplePool GetPool(PickupType type)
    {
        switch (type)
        {
            case PickupType.StunAI: return stunPool;
            case PickupType.DoubleJump: return doubleJumpPool;
            case PickupType.Shield: return shieldPool;
        }
        return null;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
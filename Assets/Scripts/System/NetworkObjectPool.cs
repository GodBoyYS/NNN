using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 网络对象池管理器
/// 核心职责：拦截 NGO 的默认生成逻辑，改为从池中存取
/// </summary>
public class NetworkObjectPool : NetworkBehaviour
{
    // wanna update the pool, not very beautiful now
    public static NetworkObjectPool Instance { get; private set; }

    [System.Serializable]
    struct PoolConfig
    {
        public GameObject Prefab;     // 要池化的预制体
        public int PrewarmCount;      // 初始生成多少个备用
        
    }

    [Header("注册配置")]
    [SerializeField] private List<PoolConfig> _pooledPrefabs;

    // 映射：Prefab -> 对象池
    private Dictionary<GameObject, ObjectPool<NetworkObject>> _pools = new Dictionary<GameObject, ObjectPool<NetworkObject>>();

    // 映射：实例 -> 它的来源Prefab (回收时需要知道它属于哪个池子)
    private Dictionary<NetworkObject, GameObject> _spawnedObjects = new Dictionary<NetworkObject, GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        // 注册所有配置的 Prefab
        foreach (var config in _pooledPrefabs)
        {
            RegisterPrefab(config.Prefab, config.PrewarmCount);
        }
    }

    public override void OnNetworkDespawn()
    {
        // 清理注册，防止重连时报错
        foreach (var prefab in _pools.Keys)
        {
            NetworkManager.Singleton.PrefabHandler.RemoveHandler(prefab);
        }
    }

    /// <summary>
    /// 初始化某个 Prefab 的池子，并注册 Handler
    /// </summary>
    private void RegisterPrefab(GameObject prefab, int count)
    {
        // 1. 创建 Unity 官方的 ObjectPool
        var pool = new ObjectPool<NetworkObject>(
            createFunc: () => CreateFunc(prefab),
            actionOnGet: ActionOnGet,
            actionOnRelease: ActionOnRelease,
            actionOnDestroy: ActionOnDestroy,
            defaultCapacity: count
        );

        _pools.Add(prefab, pool);

        // 2. 预热 (Prewarm)：提前生成一批对象，避免游戏开始时卡顿
        List<NetworkObject> temp = new List<NetworkObject>();
        for (int i = 0; i < count; i++) temp.Add(pool.Get());
        foreach (var obj in temp) pool.Release(obj); // 马上还回去，它们就是未激活状态了

        // 3. 【关键】告诉 NGO：以后这个 Prefab 的生成和销毁，由我(this)负责
        NetworkManager.Singleton.PrefabHandler.AddHandler(prefab, new PooledPrefabInstanceHandler(prefab, this));
    }
    // --- ObjectPool 的回调函数 ---
    private NetworkObject CreateFunc(GameObject prefab)
    {
        // 真正的实例化只发生在这里
        var go = Instantiate(prefab);
        return go.GetComponent<NetworkObject>();
    }
    private void ActionOnGet(NetworkObject netObj) => netObj.gameObject.SetActive(true);
    private void ActionOnRelease(NetworkObject netObj) => netObj.gameObject.SetActive(false);
    private void ActionOnDestroy(NetworkObject netObj) => Destroy(netObj.gameObject);

    // --- 公共 API (给业务逻辑调用的) ---

    /// <summary>
    /// [Server Only] 服务器从池里拿对象，并同步给客户端
    /// 替代 Instantiate + Spawn
    /// </summary>
    public NetworkObject GetNetworkObject(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (!IsServer)
        {
            Debug.LogError("[NetworkObjectPool] 只有服务器能主动生成对象！");
            return null;
        }

        // 1. 服务器本地获取
        NetworkObject netObj = _pools[prefab].Get();

        // 2. 设置位置
        netObj.transform.position = pos;
        netObj.transform.rotation = rot;

        // 3. 记录引用
        if (!_spawnedObjects.ContainsKey(netObj)) _spawnedObjects.Add(netObj, prefab);

        // 4. 网络同步 (这会触发所有客户端的 PrefabHandler)
        if (!netObj.IsSpawned) netObj.Spawn(true);

        return netObj;
    }

    /// <summary>
    /// [Server Only] 服务器回收对象
    /// 替代 Despawn + Destroy
    /// </summary>
    public void ReturnNetworkObject(NetworkObject netObj)
    {
        if (!IsServer) return;

        // 1. 网络解绑
        if (netObj.IsSpawned) netObj.Despawn(false); // false 表示不销毁 GameObject

        // 2. 放回池子
        if (_spawnedObjects.TryGetValue(netObj, out GameObject prefab))
        {
            _pools[prefab].Release(netObj);
        }
    }

    // --- 内部 API (给 Handler 调用的) ---
    // 这些方法是给“客户端”用的，当服务器发指令过来时，客户端本地找对象
    public NetworkObject GetLocal(GameObject prefab) => _pools[prefab].Get();
    public void ReturnLocal(NetworkObject netObj, GameObject prefab) => _pools[prefab].Release(netObj);
}

/// <summary>
/// 拦截器类：实现 INetworkPrefabInstanceHandler
/// </summary>
public class PooledPrefabInstanceHandler : INetworkPrefabInstanceHandler
{
    private GameObject _prefab;
    private NetworkObjectPool _pool;

    public PooledPrefabInstanceHandler(GameObject prefab, NetworkObjectPool pool)
    {
        _prefab = prefab;
        _pool = pool;
    }

    // 当 NGO 想要 Instantiate 时调用
    public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
    {
        var netObj = _pool.GetLocal(_prefab);
        netObj.transform.position = position;
        netObj.transform.rotation = rotation;
        return netObj;
    }

    // 当 NGO 想要 Destroy 时调用
    public void Destroy(NetworkObject networkObject)
    {
        _pool.ReturnLocal(networkObject, _prefab);
    }
}

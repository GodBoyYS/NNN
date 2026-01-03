using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.Collections;
using TMPro.EditorUtilities;
using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerNetworkCore : NetworkBehaviour
{
    [Header("Server LifeCycle")]
    [SerializeField] private float deathDespawnDelay = 3f;

    // 持久状态：server 写，everyone 读
    // =======NV Life
    private readonly NetworkVariable<PlayerNetworkStates.LifeState> _lifeState = new NetworkVariable<PlayerNetworkStates.LifeState>(
        PlayerNetworkStates.LifeState.Alive,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public PlayerNetworkStates.LifeState Life => _lifeState.Value;
    public NetworkVariable<PlayerNetworkStates.LifeState> LifeVar => _lifeState;

    // ======NV Motion
    private readonly NetworkVariable<PlayerNetworkStates.MotionState> _motionState = new NetworkVariable<PlayerNetworkStates.MotionState>(
        PlayerNetworkStates.MotionState.Idle,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public PlayerNetworkStates.MotionState Motion => _motionState.Value;
    public NetworkVariable<PlayerNetworkStates.MotionState> MotionVar => _motionState;
    public bool IsDead => _lifeState.Value == PlayerNetworkStates.LifeState.Dead;

    // ======NV points
    private readonly NetworkVariable<int> _points = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
        );
    public NetworkVariable<int> PointVar => _points;

    // ======NV backbag
    private NetworkList<FixedString32Bytes> _items = new NetworkList<FixedString32Bytes>(
        new List<FixedString32Bytes>(),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
        );
    public NetworkList<FixedString32Bytes> ItemsVar => _items;
    //private readonly NetworkVariable<List<string>> _items = new NetworkVariable<List<string>>(
    //    new List<string>(),
    //    NetworkVariableReadPermission.Everyone,
    //    NetworkVariableWritePermission.Server
    //    );
    //public NetworkVariable<List<string>> ItemsVar => _items;

    #region public events
    public event Action<PlayerNetworkCore> OnPlayerDied;
    #endregion

    private PlayerNetworkHealth _health;
    private PlayerNetworkMovement _movement;
    private PlayerNetworkCombat _combat;

    // server-only
    private bool _deathTimerRunning;
    private float _deathTimer;

    private void Awake()
    {
        _health = GetComponent<PlayerNetworkHealth>();
        _movement = GetComponent<PlayerNetworkMovement>();
        _combat = GetComponent<PlayerNetworkCombat>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        // reset
        _lifeState.Value = PlayerNetworkStates.LifeState.Alive;
        _deathTimerRunning = false;
        _deathTimer = 0f;

        if (_health != null)
        {
            _health.OnDiedServer += OnDiedServer;
        }
        if (GameLifecycleManager.Instance != null)
        {
            //GameLifecycleManager.Instance.RegisterPlayer(this);
        }
        else
        {
            // 如果 Instance 还没准备好（这种情况很少见，因为我们在Manager里修复了主动扫描），
            // 但为了保险，可以用 FindObject 兜底
            var manager = FindFirstObjectByType<GameLifecycleManager>();
            //if (manager != null) manager.RegisterPlayer(this);
        }
        // 可选：通知子模块“复位”
        _movement?.ServerReset();
        //_combat?.ServerReset();
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        if (_health != null)
        {
            _health.OnDiedServer -= OnDiedServer;
        }
        // 【修复】先检查 Instance 是否存在
        if (GameLifecycleManager.Instance != null)
        {
            //GameLifecycleManager.Instance.UnregisterPlayer(this);
        }
        var gameManager = GameLifecycleManager.Instance;
        //gameManager.UnregisterPlayer(this);
    }

    private void Update()
    {
        if (!IsServer) return;
        if (!IsDead) return;

        ProcessDeathDespawn();
    }

    private void OnDiedServer()
    {
        if (!IsServer) return;
        if (IsDead) return;

        _lifeState.Value = PlayerNetworkStates.LifeState.Dead;
        OnPlayerDied?.Invoke(this);
        // 死亡时统一收口：让模块停工（避免残留 chase/attack/move）
        //_movement?.ServerForceStop();
        //_combat?.ServerForceCancelAll();

        _deathTimerRunning = true;
        _deathTimer = 0f;
    }

    private void ProcessDeathDespawn()
    {
        if (!_deathTimerRunning) return;

        _deathTimer += Time.deltaTime;
        if (_deathTimer < deathDespawnDelay) return;

        var nob = GetComponent<NetworkObject>();
        if (nob != null && nob.IsSpawned)
        {
            nob.Despawn();
        }

        _deathTimerRunning = false;
    }

    // ✅ 服务器专用：给 EnemyController 等直接调用（不走 RPC）
    public void ApplyDamageServer(int amount, ulong attackerId)
    {
        if (!IsServer) return;
        if (IsDead) return;
        if (_health == null) return;

        _health.ServerTakeDamage(amount, attackerId);
    }

    public void SetMotionServer(PlayerNetworkStates.MotionState newState)
    {
        if (!IsServer) return;
        if(_motionState.Value == newState) return;
        _motionState.Value = newState;
    }
    public void AddPointsServer(int amount)
    {
        if (!IsServer) return;
        _points.Value += amount;
    }
    public void AddItem(string name)
    {
        _items.Add(name);
        //Debug.Log($"添加了{name}，目前总共有{_items.Count}个物品");
        string allItems = "";
        foreach(var item in _items)
        {
            allItems += item;
        }
        Debug.Log(allItems);
    }
}

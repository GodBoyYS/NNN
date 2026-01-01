using System;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Rendering; // 如果使用的是 Mirror，请改为 using Mirror;

public class PlayerController : NetworkBehaviour
{
    public enum MotionState : byte
    {
        Idle = 0,
        Moving = 1,
        Attack = 2
    }
    private readonly NetworkVariable<MotionState> _motionState =
        new NetworkVariable<MotionState>(
            MotionState.Idle,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
            );

    public MotionState Motion => _motionState.Value;

    [Header("Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 10f;
    [SerializeField] private float attackDistance = 20f;

    [SerializeField] private LayerMask groundLayer; // 务必设置这个层级，防止点到玩家自己
    [SerializeField] private LayerMask interactLayer;   // 该层级用于交互


    private IPlayerState currentState;

    // 服务器权威
    private Vector3 _serverTargetPosition;
    // 获取主相机缓存
    public Camera MainCamera { get; private set; }
    public Rigidbody Rigidbody { get; private set; }
    public CapsuleCollider CapsuleCollider { get; private set; }

    // 供state类访问的属性
    public float MoveSpeed => moveSpeed;
    public float RotationSpeed => rotateSpeed;
    public Vector3 ServerTargetPosition => _serverTargetPosition;

    private PlayerNetworkHealth _healthComponent;

    public LayerMask GroundLayer => groundLayer;
    public LayerMask InteractLayer => interactLayer;
    // 建议改为 Awake 获取引用，确保不论是 Server 还是 Client，组件引用一定存在
    private void Awake()
    {
        MainCamera = Camera.main; // 注意：MainCamera 在非本地玩家身上可能不需要，但在 Awake 获取也没事
        Rigidbody = GetComponent<Rigidbody>();
        CapsuleCollider = GetComponent<CapsuleCollider>();
        _healthComponent = GetComponent<PlayerNetworkHealth>();
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _serverTargetPosition = transform.position;
            _motionState.Value = MotionState.Idle;
            _healthComponent.OnHealthChanged += CheckDeath;
        }
        if (IsOwner)
        {
            BindLocalPlayerUI();
            // 只让 owner 监听并驱动自己的状态机
            _motionState.OnValueChanged += OnMotionStateChanged;
            // 初始化一次：用当前状态驱动本地 FSM
            AppyMotionState(_motionState.Value);
        }
    }
    private void OnMotionStateChanged(MotionState oldState, MotionState newState)
    {
        AppyMotionState(newState);
    }
    private void AppyMotionState(MotionState state)
    {
        if (!IsOwner) return;
        switch (state)
        {
            case MotionState.Idle:
                if (currentState is PlayerStateIdle) return;
                //ChangeState(new PlayerStateIdle(this));
                break;
            case MotionState.Moving:
                if (currentState is PlayerStateMove) return;
                //ChangeState(new PlayerStateMove(this));
                break;
        }
        //意义：
        //你的 FSM 不再被 RPC/ Server / Client 多点驱动，而是被 一个确定的下行状态 驱动。
        //入口从“恶心的多”变成“一个”。
    }
    public override void OnNetworkDespawn()
    {
        if (IsOwner && HealthBarUI.Instance != null)
        {
            _healthComponent.OnHealthChanged -= HealthBarUI.Instance.UpdateViewHealth;
            _motionState.OnValueChanged -= OnMotionStateChanged; 
        }
        if (IsServer)
        {
            _healthComponent.OnHealthChanged -= CheckDeath;
        }
    }
    void Update()
    {
        if (IsOwner)
        {
            currentState?.Update();
        }
        if (IsServer)
        {
            ProcessMovement();
        }
        // 意义：Server 不再执行你的输入状态逻辑，避免“Host 掩盖问题”。
    }
    public void ChangeState(IPlayerState nextState)
    {
        if (currentState != null) currentState.Exit();
        currentState = nextState;
        currentState.Enter();
    }

    private void BindLocalPlayerUI()
    {
        // 1. 获取 View (通过单例或 FindObject)
        var ui = HealthBarUI.Instance;
        // 卫语句：防止场景里没放 UI 报错
        if (ui == null)
        {
            Debug.LogError("Scene creates Player but HealthBarUI is missing!");
            return;
        }

        // 2. 绑定事件 (M -> V)
        _healthComponent.OnHealthChanged += ui.UpdateViewHealth;
        ui.UpdateViewHealth(_healthComponent.MaxHealth, _healthComponent.MaxHealth);
    }

    #region StateRequest methods
    public void RequestMove(Vector3 position)
    {
        if (IsOwner)
        {
            RequestMoveServerRpc(position);
        }
    }
    public void RequestStop()
    {
        if (IsOwner)
        {
            RequestStopServerRpc();
        }
    }
    //public void RequestAttack(PlayerNetworkHealth _target)
    //{
    //    if (IsOwner)
    //    {
    //        //RequestAttackServerRpc(_target);
    //    }
    //}
    [ServerRpc]
    private void RequestMoveServerRpc(Vector3 pos)
    {
        // 验证逻辑（防作弊）
        _serverTargetPosition = new Vector3(pos.x, transform.position.y, pos.z);
        _motionState.Value = MotionState.Moving;
    }
    [ServerRpc]
    private void RequestStopServerRpc()
    {
        ServerStopMove();
        //ServerStopMove();
    }
    //[ServerRpc]
    //private void RequestAttackServerRpc(PlayerNetworkHealth _target)
    //{
    //    _target.RequestTakeDamageServerRpc(10);
    //}
    #endregion

    #region movement methods
    private void ProcessMovement()
    {
        if (_motionState.Value != MotionState.Moving) return;
        float step = moveSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, _serverTargetPosition, step);
        Vector3 direction = _serverTargetPosition - transform.position;
        if (direction != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        }
        if (Vector3.Distance(transform.position, _serverTargetPosition) < 0.01f)
        {
            //if (_motionState.Value != BossMotionState.Idle) return;
            //_motionState.Value = BossMotionState.Idle;
            ServerStopMove();
        }

    }

    private void ServerStopMove()
    {
        if (_motionState.Value == MotionState.Idle) return;
        //if (_motionState.Value != BossMotionState.Idle) return;
        _motionState.Value = MotionState.Idle;
    }
    #endregion

    #region death
    // 检查是否死亡 (Server only)
    private void CheckDeath(int currentHealth, int maxHealth)
    {
        // 如果已经死了，或者正在死，就不处理
        if (currentHealth <= 0 && !(currentState is PlayerStateDie))
        {
            // 广播给所有人：切换到 Die 状态
            BroadcastDeathClientRpc();
        }
    }

    [ClientRpc]
    private void BroadcastDeathClientRpc()  // 也要改成让 networkvar来驱动
    {
        // 所有客户端（包括 host）都会收到这个消息
        //ChangeState(new PlayerStateDie(this));
    }

    // 别忘了在 OnNetworkDespawn 里取消订阅 CheckDeath
    #endregion
    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;
        if(collision != null && collision.gameObject.CompareTag("Player"))
        {
            ServerStopMove();
        }
    }

    public void CheckInteract(RaycastHit hit)
    {
        switch (hit.collider.gameObject.tag)
        {
            case "Player":
                Debug.Log("点击到了玩家");
                break;
            default:
                Debug.Log("点击到了无法交互的物体");
                break;
        }
    }
}

/*
 * 
 * using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Rendering; // 如果使用的是 Mirror，请改为 using Mirror;

public class PlayerController : NetworkBehaviour
{
    public enum BossMotionState : byte
    {
        Idle = 0,
        Moving = 1
    }

    private readonly NetworkVariable<BossMotionState> _motionState =
        new NetworkVariable<BossMotionState>(
            BossMotionState.Idle,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
            );

    public BossMotionState Motion => _motionState.Value;



    [Header("Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 10f;
    [SerializeField] private LayerMask groundLayer; // 务必设置这个层级，防止点到玩家自己
    [SerializeField] private LayerMask interactLayer;   // 该层级用于交互

    private IPlayerState currentState;

    // 服务器权威
    private Vector3 _serverTargetPosition;
    private bool _isMoving = false;
    // 获取主相机缓存
    public Camera MainCamera { get; private set; }
    public Rigidbody Rigidbody { get; private set; }
    public CapsuleCollider CapsuleCollider { get; private set; }

    // 供state类访问的属性
    public float MoveSpeed => moveSpeed;
    public float RotationSpeed => rotateSpeed;
    public bool IsMoving => _isMoving;
    public Vector3 ServerTargetPosition => _serverTargetPosition;

    private PlayerNetworkHealth _healthComponent;

    public LayerMask GroundLayer => groundLayer;
    public LayerMask InteractLayer => interactLayer;
    // 建议改为 Awake 获取引用，确保不论是 Server 还是 Client，组件引用一定存在
    private void Awake()
    {
        MainCamera = Camera.main; // 注意：MainCamera 在非本地玩家身上可能不需要，但在 Awake 获取也没事
        Rigidbody = GetComponent<Rigidbody>();
        CapsuleCollider = GetComponent<CapsuleCollider>();

        _healthComponent = GetComponent<PlayerNetworkHealth>();
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _serverTargetPosition = transform.position;
            _motionState.Value = BossMotionState.Idle;
            _healthComponent.OnHealthChanged += CheckDeath;
        }
        if (IsOwner)
        {
            BindLocalPlayerUI();
            // 只让 owner 监听并驱动自己的状态机
            _motionState.OnValueChanged += OnMotionStateChanged;
            // 初始化一次：用当前状态驱动本地 FSM
            AppyMotionState(_motionState.Value);

        }

    }

    private void OnMotionStateChanged(BossMotionState oldState, BossMotionState newState)
    {
        AppyMotionState(newState);
    }
    private void AppyMotionState(BossMotionState state)
    {
        if (!IsOwner) return;
        switch (state)
        {
            case BossMotionState.Idle:
                if (currentState is PlayerStateIdle) return;
                ChangeState(new PlayerStateIdle(this));
                break;
            case BossMotionState.Moving:
                if (currentState is PlayerStateMove) return;
                ChangeState(new PlayerStateMove(this));
                break;
        }
        //意义：
        //你的 FSM 不再被 RPC/ Server / Client 多点驱动，而是被 一个确定的下行状态 驱动。
        //入口从“恶心的多”变成“一个”。
    }
    public override void OnNetworkDespawn()
    {
        if (IsOwner && HealthBarUI.Instance != null)
        {
            _healthComponent.OnHealthChanged -= HealthBarUI.Instance.UpdateViewHealth;
        }
        if (IsServer)
        {
            _healthComponent.OnHealthChanged -= CheckDeath;
        }
    }


    void Update()
    {
        if (IsOwner)
        {
            currentState?.Update();
        }
        if (IsServer)
        {
            ProcessMovement();
        }
        // 意义：Server 不再执行你的输入状态逻辑，避免“Host 掩盖问题”。
    }

    public void ChangeState(IPlayerState nextState)
    {
        if (currentState != null) currentState.Exit();
        currentState = nextState;
        currentState.Enter();
    }

    private void BindLocalPlayerUI()
    {
        // 1. 获取 View (通过单例或 FindObject)
        var ui = HealthBarUI.Instance;
        // 卫语句：防止场景里没放 UI 报错
        if (ui == null)
        {
            Debug.LogError("Scene creates Player but HealthBarUI is missing!");
            return;
        }

        // 2. 绑定事件 (M -> V)
        _healthComponent.OnHealthChanged += ui.UpdateViewHealth;
        ui.UpdateViewHealth(_healthComponent.MaxHealth, _healthComponent.MaxHealth);
    }

    //[Rpc(SendTo.Everyone)]
    //public void ChangeStateToIdleClientRpc()
    //{
    //    if (!IsServer) // 避免服务器重复切换
    //    {
    //        Debug.Log("客户端收到状态切换指令");
    //        ChangeState(new PlayerStateIdle(this));
    //    }
    //}

    #region movement
    // -- 核心修复：rpc必须在此
    /// <summary>
    /// 供state调用的公共方法，发起移动请求
    /// </summary>
    public void RequestMove(Vector3 position)
    {
        if (IsOwner)
        {
            RequestMoveServerRpc(position);
        }
    }
    [ServerRpc]
    private void RequestMoveServerRpc(Vector3 pos)
    {
        // 验证逻辑（防作弊）
        _serverTargetPosition = new Vector3(pos.x, transform.position.y, pos.z);
        _motionState.Value = BossMotionState.Moving;
        //_isMoving = true;
    }
    //[Rpc(SendTo.ClientsAndHost)]
    //public void SwitchToIdleClientRpc()
    //{
    //    Debug.Log($"[Client]{NetworkObjectId} 收到服务器指令，切换回 Idle 状态");
    //    ChangeState(new PlayerStateIdle(this));
    //}
    //[Rpc(SendTo.ClientsAndHost)]
    //public void SwithToMoveClientRpc(Vector3 pos)
    //{
    //    ChangeState(new PlayerStateMove(this, pos));
    //}
    /// <summary>
    /// 供server在update中调用的实际移动逻辑
    /// </summary>
    private void ProcessMovement()
    {
        if (_motionState.Value != BossMotionState.Moving) return;
        float step = moveSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, _serverTargetPosition, step);
        Vector3 direction = _serverTargetPosition - transform.position;
        if (direction != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        }
        if (Vector3.Distance(transform.position, _serverTargetPosition) < 0.01f)
        {
            ServerStopMove();
            //_isMoving = false;
            //ServerStopMove();
            //ChangeStateToIdleClientRpc();
        }

        //if (!_isMoving) return;

        //float step = moveSpeed * Time.deltaTime;
        //transform.position = Vector3.MoveTowards(transform.position, _serverTargetPosition, step);

        //Vector3 direction = _serverTargetPosition - transform.position;
        //if (direction != Vector3.zero)
        //{
        //    Quaternion targetRot = Quaternion.LookRotation(direction);
        //    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        //}

        //if (Vector3.Distance(transform.position, _serverTargetPosition) < 0.01f)
        //{
        //    _isMoving = false;
        //    ServerStopMove();
        //    //ChangeStateToIdleClientRpc();
        //}
    }
    private void ServerStopMove()
    {
        if (_motionState.Value != BossMotionState.Idle) return;
        _motionState.Value = BossMotionState.Idle;
    }
    public void RequestStop()
    {
        if (IsOwner)
        {
            RequestStopServerRpc();
        }
    }
    [ServerRpc]
    private void RequestStopServerRpc()
    {
        ServerStopMove();
    }

    //private void ServerStopMove()
    //{
    //    if (!_isMoving) return;
    //    _isMoving = false;
    //    // 服务器判定停止后，通知拥有者切回idle
    //    NotifyOwnerEnterIdle();
    //}
    private void NotifyOwnerEnterIdle()
    {
        if(!IsOwner) return;
        ChangeState(new PlayerStateIdle(this));
    }
    public void StopMoving()
    {
        if(IsServer) _isMoving = false; 
    }
    #endregion

    #region death
    // 检查是否死亡 (Server only)
    private void CheckDeath(int currentHealth, int _maxHealth)
    {
        // 如果已经死了，或者正在死，就不处理
        if (currentHealth <= 0 && !(currentState is PlayerStateDie))
        {
            // 广播给所有人：切换到 Die 状态
            BroadcastDeathClientRpc();
        }
    }

    [ClientRpc]
    private void BroadcastDeathClientRpc()
    {
        // 所有客户端（包括 host）都会收到这个消息
        ChangeState(new PlayerStateDie(this));
    }

    // 别忘了在 OnNetworkDespawn 里取消订阅 CheckDeath
    #endregion
    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;
        if(collision != null && collision.gameObject.CompareTag("Player"))
        {
            ServerStopMove();
        }

        //if(collision != null && collision.gameObject.CompareTag("Player"))
        //{
        //    ServerStopMove();
        //    //_isMoving = false;
        //}
    }
}
 * 
 */
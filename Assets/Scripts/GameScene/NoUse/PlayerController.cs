using System;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Rendering; // 濡傛灉浣跨敤鐨勬槸 Mirror锛岃鏀逛负 using Mirror;

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

    [SerializeField] private LayerMask groundLayer; // 鍔″繀璁剧疆杩欎釜灞傜骇锛岄槻姝㈢偣鍒扮帺瀹惰嚜宸?
    [SerializeField] private LayerMask interactLayer;   // 璇ュ眰绾х敤浜庝氦浜?


    private IPlayerState currentState;

    // 鏈嶅姟鍣ㄦ潈濞?
    private Vector3 _serverTargetPosition;
    // 鑾峰彇涓荤浉鏈虹紦瀛?
    public Camera MainCamera { get; private set; }
    public Rigidbody Rigidbody { get; private set; }
    public CapsuleCollider CapsuleCollider { get; private set; }

    // 渚泂tate绫昏闂殑灞炴€?
    public float MoveSpeed => moveSpeed;
    public float RotationSpeed => rotateSpeed;
    public Vector3 ServerTargetPosition => _serverTargetPosition;

    private PlayerNetworkHealth _healthComponent;

    public LayerMask GroundLayer => groundLayer;
    public LayerMask InteractLayer => interactLayer;
    // 寤鸿鏀逛负 Awake 鑾峰彇寮曠敤锛岀‘淇濅笉璁烘槸 Server 杩樻槸 Client锛岀粍浠跺紩鐢ㄤ竴瀹氬瓨鍦?
    private void Awake()
    {
        MainCamera = Camera.main; // 娉ㄦ剰锛歁ainCamera 鍦ㄩ潪鏈湴鐜╁韬笂鍙兘涓嶉渶瑕侊紝浣嗗湪 Awake 鑾峰彇涔熸病浜?
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
            // 鍙 owner 鐩戝惉骞堕┍鍔ㄨ嚜宸辩殑鐘舵€佹満
            _motionState.OnValueChanged += OnMotionStateChanged;
            // 鍒濆鍖栦竴娆★細鐢ㄥ綋鍓嶇姸鎬侀┍鍔ㄦ湰鍦?FSM
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
        //鎰忎箟锛?
        //浣犵殑 FSM 涓嶅啀琚?RPC/ Server / Client 澶氱偣椹卞姩锛岃€屾槸琚?涓€涓‘瀹氱殑涓嬭鐘舵€?椹卞姩銆?
        //鍏ュ彛浠庘€滄伓蹇冪殑澶氣€濆彉鎴愨€滀竴涓€濄€?
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
    }
    public void ChangeState(IPlayerState nextState)
    {
        if (currentState != null) currentState.Exit();
        currentState = nextState;
        currentState.Enter();
    }

    private void BindLocalPlayerUI()
    {
        // 1. 鑾峰彇 View (閫氳繃鍗曚緥鎴?FindObject)
        var ui = HealthBarUI.Instance;
        // 鍗鍙ワ細闃叉鍦烘櫙閲屾病鏀?UI 鎶ラ敊
        if (ui == null)
        {
            Debug.LogError("Scene creates Player but HealthBarUI is missing!");
            return;
        }

        // 2. 缁戝畾浜嬩欢 (M -> V)
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
        // 楠岃瘉閫昏緫锛堥槻浣滃紛锛?
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
    // 妫€鏌ユ槸鍚︽浜?(Server only)
    private void CheckDeath(int currentHealth, int maxHealth)
    {
        // 濡傛灉宸茬粡姝讳簡锛屾垨鑰呮鍦ㄦ锛屽氨涓嶅鐞?
        if (currentHealth <= 0 && !(currentState is PlayerStateDie))
        {
            // 骞挎挱缁欐墍鏈変汉锛氬垏鎹㈠埌 Die 鐘舵€?
            BroadcastDeathClientRpc();
        }
    }

    [ClientRpc]
    private void BroadcastDeathClientRpc()  // 涔熻鏀规垚璁?networkvar鏉ラ┍鍔?
    {
        // 鎵€鏈夊鎴风锛堝寘鎷?host锛夐兘浼氭敹鍒拌繖涓秷鎭?
        //ChangeState(new PlayerStateDie(this));
    }

    // 鍒繕浜嗗湪 OnNetworkDespawn 閲屽彇娑堣闃?CheckDeath
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
                Debug.Log("碰到玩家");
                break;
            default:
                Debug.Log("其他物体");
                break;
        }
    }
}

/*
 * 
 * using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Rendering; // 濡傛灉浣跨敤鐨勬槸 Mirror锛岃鏀逛负 using Mirror;

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
    [SerializeField] private LayerMask groundLayer; // 鍔″繀璁剧疆杩欎釜灞傜骇锛岄槻姝㈢偣鍒扮帺瀹惰嚜宸?
    [SerializeField] private LayerMask interactLayer;   // 璇ュ眰绾х敤浜庝氦浜?

    private IPlayerState currentState;

    // 鏈嶅姟鍣ㄦ潈濞?
    private Vector3 _serverTargetPosition;
    private bool _isMoving = false;
    // 鑾峰彇涓荤浉鏈虹紦瀛?
    public Camera MainCamera { get; private set; }
    public Rigidbody Rigidbody { get; private set; }
    public CapsuleCollider CapsuleCollider { get; private set; }

    // 渚泂tate绫昏闂殑灞炴€?
    public float MoveSpeed => moveSpeed;
    public float RotationSpeed => rotateSpeed;
    public bool IsMoving => _isMoving;
    public Vector3 ServerTargetPosition => _serverTargetPosition;

    private PlayerNetworkHealth _healthComponent;

    public LayerMask GroundLayer => groundLayer;
    public LayerMask InteractLayer => interactLayer;
    // 寤鸿鏀逛负 Awake 鑾峰彇寮曠敤锛岀‘淇濅笉璁烘槸 Server 杩樻槸 Client锛岀粍浠跺紩鐢ㄤ竴瀹氬瓨鍦?
    private void Awake()
    {
        MainCamera = Camera.main; // 娉ㄦ剰锛歁ainCamera 鍦ㄩ潪鏈湴鐜╁韬笂鍙兘涓嶉渶瑕侊紝浣嗗湪 Awake 鑾峰彇涔熸病浜?
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
            // 鍙 owner 鐩戝惉骞堕┍鍔ㄨ嚜宸辩殑鐘舵€佹満
            _motionState.OnValueChanged += OnMotionStateChanged;
            // 鍒濆鍖栦竴娆★細鐢ㄥ綋鍓嶇姸鎬侀┍鍔ㄦ湰鍦?FSM
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
        //鎰忎箟锛?
        //浣犵殑 FSM 涓嶅啀琚?RPC/ Server / Client 澶氱偣椹卞姩锛岃€屾槸琚?涓€涓‘瀹氱殑涓嬭鐘舵€?椹卞姩銆?
        //鍏ュ彛浠庘€滄伓蹇冪殑澶氣€濆彉鎴愨€滀竴涓€濄€?
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
        // 鎰忎箟锛歋erver 涓嶅啀鎵ц浣犵殑杈撳叆鐘舵€侀€昏緫锛岄伩鍏嶁€淗ost 鎺╃洊闂鈥濄€?
    }

    public void ChangeState(IPlayerState nextState)
    {
        if (currentState != null) currentState.Exit();
        currentState = nextState;
        currentState.Enter();
    }

    private void BindLocalPlayerUI()
    {
        // 1. 鑾峰彇 View (閫氳繃鍗曚緥鎴?FindObject)
        var ui = HealthBarUI.Instance;
        // 鍗鍙ワ細闃叉鍦烘櫙閲屾病鏀?UI 鎶ラ敊
        if (ui == null)
        {
            Debug.LogError("Scene creates Player but HealthBarUI is missing!");
            return;
        }

        // 2. 缁戝畾浜嬩欢 (M -> V)
        _healthComponent.OnHealthChanged += ui.UpdateViewHealth;
        ui.UpdateViewHealth(_healthComponent.MaxHealth, _healthComponent.MaxHealth);
    }

    //[Rpc(SendTo.Everyone)]
    //public void ChangeStateToIdleClientRpc()
    //{
    //    if (!IsServer) // 閬垮厤鏈嶅姟鍣ㄩ噸澶嶅垏鎹?
    //    {
    //        Debug.Log("瀹㈡埛绔敹鍒扮姸鎬佸垏鎹㈡寚浠?);
    //        ChangeState(new PlayerStateIdle(this));
    //    }
    //}

    #region movement
    // -- 鏍稿績淇锛歳pc蹇呴』鍦ㄦ
    /// <summary>
    /// 渚泂tate璋冪敤鐨勫叕鍏辨柟娉曪紝鍙戣捣绉诲姩璇锋眰
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
        // 楠岃瘉閫昏緫锛堥槻浣滃紛锛?
        _serverTargetPosition = new Vector3(pos.x, transform.position.y, pos.z);
        _motionState.Value = BossMotionState.Moving;
        //_isMoving = true;
    }
    //[Rpc(SendTo.ClientsAndHost)]
    //public void SwitchToIdleClientRpc()
    //{
    //    Debug.Log($"[Client]{NetworkObjectId} 鏀跺埌鏈嶅姟鍣ㄦ寚浠わ紝鍒囨崲鍥?Idle 鐘舵€?);
    //    ChangeState(new PlayerStateIdle(this));
    //}
    //[Rpc(SendTo.ClientsAndHost)]
    //public void SwithToMoveClientRpc(Vector3 pos)
    //{
    //    ChangeState(new PlayerStateMove(this, pos));
    //}
    /// <summary>
    /// 渚泂erver鍦╱pdate涓皟鐢ㄧ殑瀹為檯绉诲姩閫昏緫
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
    //    // 鏈嶅姟鍣ㄥ垽瀹氬仠姝㈠悗锛岄€氱煡鎷ユ湁鑰呭垏鍥瀒dle
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
    // 妫€鏌ユ槸鍚︽浜?(Server only)
    private void CheckDeath(int currentHealth, int _maxHealth)
    {
        // 濡傛灉宸茬粡姝讳簡锛屾垨鑰呮鍦ㄦ锛屽氨涓嶅鐞?
        if (currentHealth <= 0 && !(currentState is PlayerStateDie))
        {
            // 骞挎挱缁欐墍鏈変汉锛氬垏鎹㈠埌 Die 鐘舵€?
            BroadcastDeathClientRpc();
        }
    }

    [ClientRpc]
    private void BroadcastDeathClientRpc()
    {
        // 鎵€鏈夊鎴风锛堝寘鎷?host锛夐兘浼氭敹鍒拌繖涓秷鎭?
        ChangeState(new PlayerStateDie(this));
    }

    // 鍒繕浜嗗湪 OnNetworkDespawn 閲屽彇娑堣闃?CheckDeath
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

using Unity.Netcode;
using UnityEngine;

public class PlayerMainController : NetworkBehaviour
{
    [Header("Components")]
    [SerializeField] private PlayerNetworkMovement _movement; 
    [SerializeField] private PlayerNetworkCombat _combat; 
    [SerializeField] private PlayerDataContainer _dataContainer; 
    [SerializeField] private PlayerNewInputManager _inputManager; 
    [SerializeField] private Animator _animator;

    // Getters
    public PlayerNetworkMovement Movement => _movement;
    public PlayerNetworkCombat Combat => _combat;
    public PlayerDataContainer DataContainer => _dataContainer;
    public Animator Animator => _animator;

    // 获取当前帧输入 (如果是Owner)
    public FrameInput Input => _inputManager != null ? _inputManager.CurrentInput : new FrameInput();

    private PlayerStateMachine _stateMachine;
    public PlayerStateMachine StateMachine => _stateMachine;

    private void Awake()
    {
        // 自动获取组件
        if (_movement == null) _movement = GetComponent<PlayerNetworkMovement>();
        if (_combat == null) _combat = GetComponent<PlayerNetworkCombat>();
        if (_dataContainer == null) _dataContainer = GetComponent<PlayerDataContainer>();
        if (_inputManager == null) _inputManager = GetComponent<PlayerNewInputManager>();
        if (_animator == null) _animator = GetComponent<Animator>();

        _stateMachine = new PlayerStateMachine(this);

        if (GameCameraManager.Instance != null)
        {
            //Debug.Log("[Client Debug] 找到 GameCameraManager，正在设置跟随目标...");
            GameCameraManager.Instance.SetFollowTarget(transform);
        }
    }
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            GameHUDView.Instance.BindToLocalPlayer(_dataContainer, _combat);
        }
        if (IsServer)
        {
            GameLifecycleManager.Instance.RegisterPlayer(this);
        }
    }
    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            GameLifecycleManager.Instance.UnregisterPlayer(this);
        }
    }

    private void Update()
    {
        // 状态机每帧运行
        _stateMachine.Update();
    }
}

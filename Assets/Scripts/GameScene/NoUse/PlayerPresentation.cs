using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PlayerNetworkCore))]
public class PlayerPresentation : NetworkBehaviour
{
    private PlayerNetworkCore _core;
    private PlayerNetworkCombat _combat;
    private PlayerNetworkHealth _health;
    private IPlayerState _currentState;

    private string _skillAnimationName;
    public string SkillAnimationName => _skillAnimationName;

    public Rigidbody Rigidbody { get; private set; }
    public CapsuleCollider CapsuleCollider { get; private set; }
    public Animator Animator { get; private set; }

    private void Awake()
    {
        _core = GetComponent<PlayerNetworkCore>();
        _combat = GetComponent<PlayerNetworkCombat>();
        _health = GetComponent<PlayerNetworkHealth>();
        Rigidbody = GetComponent<Rigidbody>();
        CapsuleCollider = GetComponent<CapsuleCollider>();
        Animator = GetComponent<Animator>();
    }
    public override void OnNetworkSpawn()
    {
        if (_core != null)
        {
            _core.LifeVar.OnValueChanged += OnLifeChanged;
            _core.MotionVar.OnValueChanged += OnMotionChanged;
        }
        // 【新增】监听技能索引变化
        if (_combat != null)
        {
            //_combat.OnSkillIndexChanged += OnSkillIndexChanged;
        }
        RefreshStateFromNet();
        //if(IsOwner && _health != null)
        //{
        //    BindLocalPlayerUI();
        //}
        if (_health != null)
        {
            _health.OnDamaged += OnDamagedLocal; // everyone都可以播放被打表现
        }
        // 关键：只有 Owner (本地玩家) 才需要连接 UI
        if (IsOwner)
        {
            // 绑定 UI
            if (GameHUDView.Instance != null)
            {
                //GameHUDView.Instance.BindToLocalPlayer(_health, _combat, _core);
            }

            // --- 增加调试日志 ---
            //Debug.Log($"[Client Debug] 本地玩家 {OwnerClientId} 生成。正在寻找摄像机管理器...");

            if (GameCameraManager.Instance != null)
            {
                //Debug.Log("[Client Debug] 找到 GameCameraManager，正在设置跟随目标...");
                GameCameraManager.Instance.SetFollowTarget(transform);
            }
            else
            {
                // 如果此时还没找到，说明执行顺序有问题，我们需要报错
                //Debug.LogError("!!! 致命错误：场景中找不到 GameCameraManager！摄像机无法跟随！");
            }
        }

    }
    public override void OnNetworkDespawn()
    {
        if (_core != null)
        {
            _core.LifeVar.OnValueChanged -= OnLifeChanged;
            _core.MotionVar.OnValueChanged -= OnMotionChanged;
        }
        // 【新增】移除监听
        if (_combat != null)
        {
            _combat.OnSkillIndexChanged -= OnSkillIndexChanged;
        }

        if (IsOwner && _health != null && HealthBarUI.Instance != null)
        {
            _health.OnHealthChanged -= HealthBarUI.Instance.UpdateViewHealth;
            _health.OnDamaged -= OnDamagedLocal;
        }
    }
    // 【新增】当技能索引变化时，如果当前是Skill状态，说明数据更新了，重新刷新一下动画
    private void OnSkillIndexChanged(int newIndex)
    {
        // 只有当 Index 有效(不是复位的-1) 且 当前确实处于 Skill 状态时，才去刷新
        // 这样解决了“Motion先到，Index后到”导致动画没播的问题
        if (_core.Motion == PlayerNetworkStates.MotionState.Skill && newIndex != -1)
        {
            RefreshStateFromNet();
        }
    }
    private void OnDamagedLocal(int damage, ulong attackerId)
    {
        // 这里播放：受击闪白，受击音效，受击动作
        // 如果只想owner才震屏
        if (IsOwner)
        {
            // camera shake
        }
    }
    private void Update()
    {
        _currentState?.Update();
    }
    private void OnMotionChanged(PlayerNetworkStates.MotionState oldState, PlayerNetworkStates.MotionState newState)
    {
        RefreshStateFromNet();
    }
    private void OnLifeChanged(PlayerNetworkStates.LifeState oldState, PlayerNetworkStates.LifeState newState)
    {
        RefreshStateFromNet();
    }
    private void RefreshStateFromNet()
    {
        if (_core == null) return;
        // life优先级最高：dead覆盖motion
        if (_core.Life == PlayerNetworkStates.LifeState.Dead)
        {
            if (_currentState is PlayerStateDie) return;
            ChangeState(new PlayerStateDie(this));
            return;
        }
        switch (_core.Motion)
        {
            case PlayerNetworkStates.MotionState.Moving:
                if (_currentState is PlayerStateMove) return;
                ChangeState(new PlayerStateMove(this));
                break;
            case PlayerNetworkStates.MotionState.Idle:
                if (_currentState is PlayerStateIdle) return;
                ChangeState(new PlayerStateIdle(this));
                break;
            case PlayerNetworkStates.MotionState.Skill:
                //_skillAnimationName = _combat.GetSkillAnimationName();
                ChangeState(new PlayerStateSkill(this));
                break;
            default:
                ChangeState(new PlayerStateIdle(this));
                break;
        }
    }
    public void ChangeState(IPlayerState nextState)
    {
        _currentState?.Exit();
        _currentState = nextState;
        _currentState.Enter();
    }
    private void BindLocalPlayerUI()
    {
        var ui = HealthBarUI.Instance;
        if (ui == null)
        {
            Debug.LogError("场景存在玩家但是没有 血量条");
            return;
        }
        if (_health == null)
        {
            Debug.LogError("玩家没有血量组件");
            return;
        }
        _health.OnHealthChanged += ui.UpdateViewHealth;
        ui.UpdateViewHealth(_health.MaxHealth, _health.MaxHealth);
    }
}

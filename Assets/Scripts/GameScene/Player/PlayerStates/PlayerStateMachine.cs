using UnityEngine;

public class PlayerStateMachine
{
    public PlayerNewStateIdle StateIdle; 
    public PlayerNewStateMove StateMove; 
    public PlayerNewStateCharge StateCharge;
    public PlayerNewStateSkill StateSkill; 
    public PlayerNewStateRecovery StateRecovery;
    public PlayerNewStateDie StateDie;

    // 新增：用于在不同状态间传递“当前是哪个技能”
    public int PendingSkillIndex { get; set; } = -1;

    private float _chargeTime;
    private float _skillTime;
    private float _recoveryTime;
    public float ChargeTime => _chargeTime;
    public float SkillTime => _skillTime;
    public float RecoveryTime => _recoveryTime;

    // 公开属性方便调试
    public PlayerBaseState CurrentState => _currentState;

    private PlayerBaseState _currentState;
    private PlayerMainController _controller;
    // 新增：用于存储技能释放时的目标位置（鼠标点击点）
    public Vector3 PendingAimPosition { get; set; }

    public PlayerStateMachine(PlayerMainController controller)
    {
        _controller = controller;

        // 1. 实例化所有状态
        StateIdle = new PlayerNewStateIdle(_controller);
        StateMove = new PlayerNewStateMove(_controller);
        StateCharge = new PlayerNewStateCharge(_controller);
        StateSkill = new PlayerNewStateSkill(_controller);
        StateRecovery = new PlayerNewStateRecovery(_controller);
        StateDie = new PlayerNewStateDie(_controller);

        // 2. [关键修复] 初始化默认状态！
        // 直接赋值并手动调用 Enter，避免调用 ChangeState 导致空引用异常
        _currentState = StateIdle;
        _currentState.OnEnter();

        Debug.Log($"[StateMachine] 初始化完成，当前状态: {_currentState}");
    }

    public void Update()
    {
        _currentState?.OnUpdate();
    }

    public void ChangeState(PlayerBaseState nextState)
    {
        if (nextState == null) return;

        // [优化] 防止重复进入相同状态
        // if (_currentState == nextState) return; 

        // 3. [安全检查] 只有当前状态不为空时才调用 Exit
        if (_currentState != null)
        {
            _currentState.OnExit();
        }

        Debug.Log($"[StateMachine] 切换状态: {_currentState} -> {nextState}");

        _currentState = nextState;
        _currentState.OnEnter();
    }

    public void SetAnimationTimes(float chargeTime,  float skillTime, float recoveryTime)
    {
        _chargeTime = chargeTime;
        _skillTime = skillTime;
        _recoveryTime = recoveryTime;
    }
}

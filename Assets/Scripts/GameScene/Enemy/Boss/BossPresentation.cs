using UnityEngine;

[RequireComponent(typeof(Animator))]
public class BossPresentation : MonoBehaviour
{
    private BossController _controller;
    private IEnemyState _currentState;
    private Animator _animator;
    private string _skillAnimationName;
    private string _skillChargeAnimationName;

    public Animator Animator => _animator;
    public string SkillAnimationName => _skillAnimationName;
    public string SkillChargeAnimationName => _skillChargeAnimationName;
    // 给 State 类访问
    public BossController Controller => _controller;

    void Start()
    {
        _controller = GetComponent<BossController>();
        _controller.Motion.OnValueChanged += OnMotionStateChanged;
        // 监听技能索引变化，确保连发技能时也能刷新
        _controller.SkillIndexVar.OnValueChanged += OnSkillIndexChanged;
        _animator = GetComponent<Animator>();
    }

    void Update()
    {
        // 【关键】必须每帧调用当前状态的 Update
        // 这样 BossStateMove 才能检测速度并播放动画
        _currentState?.Update();
    }

    // 销毁时取消订阅，防止报错
    private void OnDestroy()
    {
        if (_controller != null)
        {
            _controller.Motion.OnValueChanged -= OnMotionStateChanged;
        }
    }

    private void OnMotionStateChanged(BossController.BossMotionState oldState, BossController.BossMotionState newState)
    {
        RefreshState();
    }
    private void OnSkillIndexChanged(int oldIdx, int newIdx)
    {
        // 如果索引变了，强制刷新当前状态（针对 Charge 或 Skill 状态）
        if (_controller.MotionStateVar == BossController.BossMotionState.Skill ||
            _controller.MotionStateVar == BossController.BossMotionState.Charge)
        {
            RefreshState();
        }
    }

    private void RefreshState()
    {
        if (_controller == null) return;

        switch (_controller.MotionStateVar)
        {
            case BossController.BossMotionState.Idle:
                // 注意：这里要避免重复创建 new 对象，最好缓存 State 对象，但为了简单先这样
                if (_currentState is BossStateIdle) return;
                ChangeState(new BossStateIdle(this));
                break;

            case BossController.BossMotionState.Chase:
                if (_currentState is BossStateMove) return; // 注意类型检查修正
                ChangeState(new BossStateMove(this));
                break;

            case BossController.BossMotionState.Charge: // [新增] 处理蓄力状态
                // 每次都 new，确保 Enter 被触发播放动画
                _skillChargeAnimationName = _controller.GetCurrentChargeAnimationName();
                //ChangeState(new BossStateCharge(this));
                break;

            case BossController.BossMotionState.Skill:
                // 此时 Client 端去拿 AnimationName，因为 NetVar 已经同步了，所以是准确的
                _skillAnimationName = _controller.GetCurrentSkillAnimationName();
                // 强制重新进入 Skill 状态，以便每次放技能都播放动画 (即使是连发)
                ChangeState(new BossStateSkill(this));
                break;

            case BossController.BossMotionState.Die:
                // 你还没写 BossStateDie，这里留空
                break;
        }
    }

    public void ChangeState(IEnemyState newState)
    {
        _currentState?.Exit();
        _currentState = newState;
        _currentState.Enter();
    }
}
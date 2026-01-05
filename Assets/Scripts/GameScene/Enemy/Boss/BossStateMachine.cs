using UnityEngine;

public class BossStateMachine
{
    public BossBaseState CurrentState { get; private set; }

    // 状态实例
    public BossStateIdle StateIdle;
    public BossStateMove StateMove;
    public BossStateCharge StateCharge;
    public BossStateSkill StateSkill;
    public BossStateRecovery StateRecovery; // 新增 Recovery
    public BossStateDie StateDie;

    // 数据上下文 (类似于 PlayerStateMachine 的设计)
    public int PendingSkillIndex { get; set; } = -1;

    public BossStateMachine(BossController controller)
    {
        StateIdle = new BossStateIdle(controller, this);
        StateMove = new BossStateMove(controller, this);
        StateCharge = new BossStateCharge(controller, this);
        StateSkill = new BossStateSkill(controller, this);
        StateRecovery = new BossStateRecovery(controller, this);
        StateDie = new BossStateDie(controller, this);
    }

    public void Update()
    {
        CurrentState?.OnUpdate();
    }

    public void ChangeState(BossBaseState newState)
    {
        if (newState == null || newState == CurrentState) return;

        // Debug.Log($"[BossFSM] {CurrentState?.GetType().Name} -> {newState.GetType().Name}");

        CurrentState?.OnExit();
        CurrentState = newState;
        CurrentState.OnEnter();
    }
}

using UnityEngine;

public class PlayerNewStateIdle : PlayerBaseState
{
    public PlayerNewStateIdle(PlayerMainController controller) : base(controller) { }

    public override void OnEnter()
    {
        _controller.Animator.CrossFade("Idle_Battle_SwordAndShield", 0.05f);
        if (_controller.IsOwner)
        {
            _controller.Movement.RequestStop();
        }
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        if (_controller.IsOwner)
        {
            StateLogic();
        }
    }

    protected override void StateLogic()
    {
        if (ChangeStateToCharge()) return;
        if (ChangeStateToMove()) return;
    }

    #region Checks

    private bool ChangeStateToMove()
    {
        if (_currentInput.InteractDown && _currentInput.HasMouseTarget)
        {
            Debug.Log("在 idle状态检测到移动输入，且有点击的目标，准备切换到 移动状态");
            _controller.StateMachine.ChangeState(_controller.StateMachine.StateMove);
            return true;
        }
        return false;
    }

    private bool ChangeStateToCharge()
    {
        int skillIdx = -1;

        // 优先检测按键，并获取对应的技能索引
        if (_currentInput.AttackDown) skillIdx = 0;
        else if (_currentInput.SkillQDown) skillIdx = 1;
        else if (_currentInput.SkillWDown) skillIdx = 2;
        else if (_currentInput.SkillEDown) skillIdx = 3;

        if (skillIdx != -1)
        {
            // 关键修改：在切换状态前检查 Client 端的 CD 是否就绪
            if (_controller.Combat.IsSkillReadyClient(skillIdx))
            {
                Debug.Log($"[Idle] 释放技能 {skillIdx}，CD就绪，切换状态");
                //_controller.StateMachine.ChangeState(_controller.StateMachine.StateCharge);
                // [修复] 记录技能索引和鼠标目标位置
                _controller.StateMachine.PendingSkillIndex = skillIdx;
                _controller.StateMachine.PendingAimPosition = _currentInput.MouseWorldPos;
                _controller.StateMachine.ChangeState(_controller.StateMachine.StateCharge);
                return true;
            }
        }

        return false;
    }

    #endregion
}

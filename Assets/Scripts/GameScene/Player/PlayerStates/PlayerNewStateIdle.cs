using UnityEngine;

public class PlayerNewStateIdle : PlayerBaseState
{
    public PlayerNewStateIdle(PlayerMainController controller) : base(controller) { }

    public override void OnEnter()
    {
        // 播放待机动画
        _controller.Animator.Play("Idle_Battle_SwordAndShield");
        // 确保停止移动
        if (_controller.IsOwner)
        {
            _controller.Movement.RequestStop();
        }
    }

    public override void OnUpdate()
    {
        base.OnUpdate(); // 检查死亡 & 更新Input

        if (_controller.IsOwner)
        {
            StateLogic();
        }
    }

    protected override void StateLogic()
    {
        // 结构清晰的判断链
        if (ChangeStateToSkill()) return;
        if (ChangeStateToMove()) return;
    }

    #region Checks
    private bool ChangeStateToMove()
    {
        // 玩家右键点击地面 -> 切换到移动状态
        if (_currentInput.InteractDown && _currentInput.HasMouseTarget)
        {
            Debug.Log("在idle状态按下了移动输入，并且有点击目标，请求切换到移动状态");
            _controller.StateMachine.ChangeState(_controller.StateMachine.StateMove);
            return true;
        }
        return false;
    }

    private bool ChangeStateToSkill()
    {
        // 任何技能键按下 -> 切换到技能状态
        if (_currentInput.AttackDown || _currentInput.SkillQDown || _currentInput.SkillWDown || _currentInput.SkillEDown)
        {
            Debug.Log("按下了技能键");
            _controller.StateMachine.ChangeState(_controller.StateMachine.StateSkill);
            return true;
        }
        return false;
    }
    #endregion
}
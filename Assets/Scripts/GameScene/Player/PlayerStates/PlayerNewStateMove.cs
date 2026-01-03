using UnityEngine;

public class PlayerNewStateMove : PlayerBaseState
{
    private Vector3 _targetPos; 
    private const float StopDistance = 0.2f;

    public PlayerNewStateMove(PlayerMainController controller) : base(controller) { }

    public override void OnEnter()
    {
        _controller.Animator.Play("MoveFWD_Normal_InPlace_SwordAndShield");

        // [关键修复1] 立即响应移动
        // 直接访问 Controller 的实时 Input，因为状态切换当帧 _currentInput 可能未更新
        var input = _controller.Input;

        if (input.HasMouseTarget)
        {
            _targetPos = input.MouseWorldPos;
            _controller.Movement.RequestMove(_targetPos);
        }
        else
        {
            // 如果是以奇怪的方式进入移动状态且没有目标，原地停止
            _targetPos = _controller.transform.position;
        }
    }

    public override void OnUpdate()
    {
        base.OnUpdate();

        if (_controller.IsOwner)
        {
            StateLogic();

            // 持续更新移动逻辑（处理按住鼠标的情况）
            if (_controller.StateMachine.CurrentState == this)
            {
                PerformMove();
            }
        }
    }

    protected override void StateLogic()
    {
        if (ChangeStateToSkill()) return;
        if (ChangeStateToIdle()) return;
    }

    private void PerformMove()
    {
        // 如果玩家持续按住右键，更新目标点
        if (_currentInput.InteractDown && _currentInput.HasMouseTarget)
        {
            _targetPos = _currentInput.MouseWorldPos;
            _controller.Movement.RequestMove(_targetPos);
        }
    }

    #region Checks

    private bool ChangeStateToIdle()
    {
        // 1. 按S强制停止
        if (_currentInput.StopDown)
        {
            _controller.Movement.RequestStop(); // 确保组件层也停止
            _controller.StateMachine.ChangeState(_controller.StateMachine.StateIdle);
            return true;
        }

        // 2. [关键修复2] 到达目的地检测
        // 计算平面距离（忽略Y轴差异）
        Vector3 playerPos = _controller.transform.position;
        playerPos.y = 0;
        Vector3 targetPos = _targetPos;
        targetPos.y = 0;

        if (Vector3.Distance(playerPos, targetPos) <= StopDistance)
        {
            _controller.StateMachine.ChangeState(_controller.StateMachine.StateIdle);
            return true;
        }

        return false;
    }

    private bool ChangeStateToSkill()
    {
        if (_currentInput.AttackDown || _currentInput.SkillQDown || _currentInput.SkillWDown || _currentInput.SkillEDown)
        {
            _controller.StateMachine.ChangeState(_controller.StateMachine.StateSkill);
            return true;
        }
        return false;
    }

    #endregion
}
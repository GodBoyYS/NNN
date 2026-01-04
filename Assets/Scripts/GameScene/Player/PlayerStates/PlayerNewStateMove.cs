using UnityEngine;

public class PlayerNewStateMove : PlayerBaseState
{
    private Vector3 _targetPos; 
    private const float StopDistance = 0.2f;

    public PlayerNewStateMove(PlayerMainController controller) : base(controller)
    {
    }

    public override void OnEnter()
    {
        _controller.Animator.Play("MoveFWD_Normal_InPlace_SwordAndShield");

        var input = _controller.Input;
        if (input.HasMouseTarget)
        {
            _targetPos = input.MouseWorldPos;
            _controller.Movement.RequestMove(_targetPos);
        }
        else
        {
            _targetPos = _controller.transform.position;
        }
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        if (_controller.IsOwner)
        {
            StateLogic();

            // 只有在当前状态仍为Move时才执行
            if (_controller.StateMachine.CurrentState == this)
            {
                PerformMove();
            }
        }
    }

    protected override void StateLogic()
    {
        if (ChangeStateToCharge()) return;
        if (ChangeStateToIdle()) return;
    }

    private void PerformMove()
    {
        if (_currentInput.InteractDown && _currentInput.HasMouseTarget)
        {
            _targetPos = _currentInput.MouseWorldPos;
            _controller.Movement.RequestMove(_targetPos);
        }
    }

    #region Checks

    private bool ChangeStateToIdle()
    {
        if (_currentInput.StopDown)
        {
            _controller.Movement.RequestStop();
            _controller.StateMachine.ChangeState(_controller.StateMachine.StateIdle);
            return true;
        }

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

    private bool ChangeStateToCharge()
    {
        int skillIdx = -1;

        if (_currentInput.AttackDown) skillIdx = 0;
        else if (_currentInput.SkillQDown) skillIdx = 1;
        else if (_currentInput.SkillWDown) skillIdx = 2;
        else if (_currentInput.SkillEDown) skillIdx = 3;

        if (skillIdx != -1)
        {
            // 关键修改：检查CD
            if (_controller.Combat.IsSkillReadyClient(skillIdx))
            {
                //_controller.StateMachine.ChangeState(_controller.StateMachine.StateCharge);
                _controller.StateMachine.ChangeState(_controller.StateMachine.StateCharge);
                return true;
            }
        }
        return false;
    }

    #endregion
}
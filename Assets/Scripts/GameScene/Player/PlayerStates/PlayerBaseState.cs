using UnityEngine;

public abstract class PlayerBaseState
{
    protected PlayerMainController _controller; 
    protected FrameInput _currentInput; // 提升为字段

    public PlayerBaseState(PlayerMainController controller)
    {
        _controller = controller;
    }

    public virtual void OnEnter() { }

    public virtual void OnUpdate()
    {
        // 1. 全局死亡检查 (优先级最高)
        // 无论是不是Owner，如果数据层说死了，就必须进死亡状态
        if (_controller.DataContainer.IsDead)
        {
            if (!(_controller.StateMachine.CurrentState is PlayerNewStateDie))
            {
                _controller.StateMachine.ChangeState(_controller.StateMachine.StateDie);
            }
            return;
        }

        // 2. 更新输入 (仅本地玩家有效)
        if (_controller.IsOwner)
        {
            _currentInput = _controller.Input;
        }
    }

    public virtual void OnExit() { }

    // 强制子类实现的核心逻辑流
    protected abstract void StateLogic();
}

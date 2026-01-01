using UnityEngine;

public abstract class StateMachine
{
}
public class BossStateMachine
{
    public BossBaseState _currentState;
    public BossStateIdle StateIdle;
    public BossStateMove StateMove;
    public BossStateCharge StateCharge;
    public BossStateActive StateActive;
    public BossStateRecovery StateRecovery;
    public BossStateDie StateDie;

    private BossController _bossController;
    private BossPresentation _view;
    //private BossStateSkill _skill; // 启用->改为使用stateactive
    public BossStateMachine(BossController controller, BossPresentation view)
    {
        _bossController = controller;
        _view = view;
        StateIdle = new BossStateIdle(_view);
        StateMove = new BossStateMove(_view);
        StateCharge = new BossStateCharge(_bossController, _view);
        StateActive = new BossStateActive();
        StateRecovery = new BossStateRecovery();
        StateDie = new BossStateDie(_view);

    }
    public void Update()
    {
        _currentState?.Update();
    }
    public void ChangeState(BossBaseState newState) 
    {
        if(newState == _currentState) return;
        _currentState = newState;
        _currentState?.Enter();
    }
}
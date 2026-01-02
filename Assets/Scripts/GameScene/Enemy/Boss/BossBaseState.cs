using UnityEngine;

public abstract class BossBaseState
{
    protected BossController _controller;
    protected BossPresentation _view;
    protected BossStateMachine _stateMachine;

    public BossBaseState(BossController controller, BossStateMachine stateMachine)
    {
        _controller = controller;
        _view = controller.View;
        _stateMachine = stateMachine;
    }

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void Exit() { }
}

//using UnityEngine;

//public class BossBaseState 
//{
//    protected BossController _bossController;
//    protected BossPresentation _view;
//    public BossBaseState(BossController bossController, BossPresentation view)
//    {
//        _bossController = bossController;
//        _view = view;
//    }

//    public virtual void Enter() { }
//    public virtual void Update() { }
//    public virtual void Exit() { }
//}


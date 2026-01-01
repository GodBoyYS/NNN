using UnityEngine;

public class BossBaseState 
{
    protected BossController _bossController;
    protected BossPresentation _view;
    public BossBaseState(BossController bossController, BossPresentation view)
    {
        _bossController = bossController;
        _view = view;
    }

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void Exit() { }
}


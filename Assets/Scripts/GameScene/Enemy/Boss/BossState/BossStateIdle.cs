using UnityEngine;

public class BossStateIdle : IEnemyState
{
    private BossPresentation _view;
    public BossStateIdle(BossPresentation view)
    {
        _view = view;
    }
    public void Enter()
    {
        _view.Animator.Play("Idle");
    }

    public void Exit()
    {

    }

    public void Update()
    {

    }
}

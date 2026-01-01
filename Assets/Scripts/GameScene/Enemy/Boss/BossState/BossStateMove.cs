using UnityEngine;
using UnityEngine.AI;

public class BossStateMove : IEnemyState
{
    private BossPresentation _view;
    private NavMeshAgent _agent;

    public BossStateMove(BossPresentation view)
    {
        _view = view;
        _agent = view.GetComponent<NavMeshAgent>();
    }

    public void Enter()
    {
        _view.Animator.Play("Walk");
    }

    public void Exit() { }

    public void Update()
    {
        // 简单的防滑步检测
        if (_agent != null)
        {
            // 如果速度很慢，可能是被挡住或者停止了，临时播 Idle
            if (_agent.velocity.sqrMagnitude < 0.1f)
            {
                // _view.Animator.Play("Idle"); 
            }
            else
            {
                // 确保动画状态机在 Walk
                // _view.Animator.Play("Walk");
            }
        }
    }
}
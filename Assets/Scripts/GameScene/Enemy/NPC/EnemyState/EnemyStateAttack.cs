using Unity.Netcode;
using UnityEngine;

public class EnemyStateAttack : IEnemyState
{
    private EnemyPresentation _view;
    public EnemyStateAttack(EnemyPresentation view)
    {
        _view = view;
    }

    public void Enter()
    {
        Debug.Log("¹ÖÎï½øÈë¹¥»÷×´Ì¬");
        _view.Animator.Play("Attack01");
    }

    public void Exit()
    {
    }

    public void Update()
    {
    }
}
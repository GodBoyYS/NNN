
using Unity.Netcode;
using UnityEngine;

public class EnemyStateMove : IEnemyState
{
    private EnemyPresentation _view;

    public EnemyStateMove(EnemyPresentation view)
    {
        _view = view;
    }

    public void Enter()
    {
        Debug.Log("怪物进入追逐状态");
        _view.Animator.Play("WalkFWD");
    }

    public void Exit()
    {
    }

    public void Update()
    {
    }
}

/*
 * 
using Unity.Netcode;
using UnityEngine;

public class EnemyStateMove : IEnemyState
{
    ////private EnemyController _enemy;
    ////private NetworkObject _target;
    //// 优化：计时器，避免每帧重复计算路径
    //private float _repathTimer = 0f;
    //private float _repathInterval = 0.2f;

    private EnemyPresentation _view;

    public EnemyStateMove(EnemyPresentation view)
    {
        _view = view;
    }

    public void Enter()
    {
        Debug.Log("怪物进入追逐状态");
        //// 进入状态立即进行一次移动
        //if (_target != null && _enemy.NavMeshAgent.isOnNavMesh)
        //{
        //    _enemy.NavMeshAgent.isStopped = false;  // 确保agent是开启移动的
        //    _enemy.NavMeshAgent.SetDestination(_target.transform.position);
        //}
    }

    public void Exit()
    {
        // 离开追逐（比如进入攻击或者死亡）时，停止移动
        //if (_enemy.NavMeshAgent.isOnNavMesh)
        //{
        //    _enemy.NavMeshAgent.isStopped = true;
        //    _enemy.NavMeshAgent.ResetPath();
        //}
    }

    public void Update()
    {
        ////if (ChangeStateToSkill()) return;
        //if (ChangeStateToIdle()) return;
        //ChasePlayer();

    }
    //private void ChasePlayer()
    //{
    //    // 持续追逐敌人
    //    // todo
    //    // 追逐player：思路-->获取玩家位置，navmeshagent.settarget()，networktransform更新位置
    //    if (_target == null) return;
    //    // 简单的计时逻辑
    //    _repathTimer += Time.deltaTime;
    //    if (_repathTimer > _repathInterval)
    //    {
    //        _repathTimer = 0f;
    //        // 【核心移动代码】
    //        // 只需要这一行，NavMeshAgent 就会推动 GameObject 移动
    //        // NetworkTransform 会自动把这个移动同步给客户端
    //        if (_enemy.NavMeshAgent.isOnNavMesh)    // 加上校验防止报错
    //        {
    //            _enemy.NavMeshAgent.SetDestination(_target.transform.position);
    //        }
    //    }
    //}
    //// chase -> idle / attack
    //private bool ChangeStateToSkill()
    //{   // 1.玩家进入攻击范围，切换到攻击状态
    //    return false;
    //}
    //private bool ChangeStateToIdle()
    //{   // 2.玩家逃脱追逐范围，切换到idle状态
    //    if (_target == null || Vector3.Distance(_target.transform.position, _enemy.transform.position) > _enemy.ChaseRange)
    //    {
    //        _enemy.ChangeState(new EnemyStateIdle(_enemy));
    //        return true;
    //    }
    //    return false;
    //}
}
*/

using Unity.Netcode;
using UnityEngine;

public class EnemyStateIdle : IEnemyState
{
    private EnemyPresentation _view;
    private NetworkObject _target;

    public EnemyStateIdle(EnemyPresentation view)
    {
        _view = view;
    }

    public void Enter()
    {
        // 播放动画
        _view.Animator.Play("IdleNormal");
        Debug.Log("怪物进入 Idle 状态");
        _target = null;
    }

    public void Exit() { }

    public void Update()
    {
    }
}

//using Unity.Netcode;
//using UnityEngine;

//public class EnemyStateIdle : IEnemyState
//{
//    private EnemyPresentation _view;
//    private NetworkObject _target;

//    public EnemyStateIdle(EnemyPresentation view)
//    {
//        _view = view;
//    }

//    public void Enter()
//    {
//        // 播放动画
//        // _view.Animator.SetBool(...);
//        Debug.Log("怪物进入 Idle 状态");
//        _target = null;
//    }

//    public void Exit() { }

//    public void Update()
//    {
//        //DetectPlayer();

//        //// 尝试切换到追逐
//        //if (ChangeStateToChasing()) return;

//        //// 【关键修复 1】: 如果有目标，但是 ChangeStateToChasing 失败了（说明距离不够或者条件不满足）
//        //// 必须把目标置空！否则下一帧 DetectPlayer 会因为 _target != null 而直接跳过检测
//        //// 导致怪物卡死在“有一个太远的目标”的状态里
//        //if (_target != null)
//        //{
//        //    _target = null;
//        //}

//    }

//    //// Idle -> Chase
//    //private bool ChangeStateToChasing()
//    //{
//    //    if (_target == null) return false;

//    //    // 【关键修复 2】: 给予一点宽容度 (Hysteresis)
//    //    // OverlapSphere 检测的是碰撞体，Distance 检测的是中心点
//    //    // 如果 DetectPlayer 已经基于碰撞体检测到了，我们应该放宽一点距离判断
//    //    // 比如乘以 1.2 或者直接信任 DetectPlayer 的结果（这里我加了 1.2倍 容错）
//    //    float checkDistance = _enemy.ChaseRange * 1.2f;

//    //    if (Vector3.Distance(_target.transform.position, _enemy.transform.position) > checkDistance)
//    //        return false;

//    //    _enemy.ChangeState(new EnemyStateMove(_enemy, _target));
//    //    return true;
//    //}

//    //private void DetectPlayer()
//    //{
//    //    if (_target != null) return;    // 防止重复检测

//    //    // 1. 获取范围内物体
//    //    var colliderInfos = Physics.OverlapSphere(_enemy.transform.position, _enemy.ChaseRange, _enemy.ChaseLayer);

//    //    if (colliderInfos.Length <= 0) return;

//    //    // 【关键修复 3】: 绝对不要用 colliderInfos[0] !!!
//    //    // OverlapSphere 返回顺序是不确定的。
//    //    // 如果索引 [0] 是怪物自己（如果层级设置重叠），或者是一个死掉的玩家，逻辑就会出错。
//    //    // 必须遍历寻找“最近的、有效的”玩家。

//    //    NetworkObject bestTarget = null;
//    //    float minDistance = float.MaxValue;

//    //    foreach (Collider collider in colliderInfos)
//    //    {
//    //        // 排除自己
//    //        if (collider.gameObject == _enemy.gameObject) continue;

//    //        // 尝试获取 NetworkObject
//    //        if (!collider.TryGetComponent<NetworkObject>(out NetworkObject netObj)) continue;

//    //        // 简单的距离比对，找最近的
//    //        float d = Vector3.Distance(collider.transform.position, _enemy.transform.position);
//    //        if (d < minDistance)
//    //        {
//    //            minDistance = d;
//    //            bestTarget = netObj;
//    //        }
//    //    }

//    //    if (bestTarget != null)
//    //    {
//    //        Debug.Log($"锁定目标: {bestTarget.name}");
//    //        _target = bestTarget;
//    //    }
//    //}

//    //// Idle -> Attack (你需要保留这个逻辑)
//    //private bool ChangeStateToSkill()
//    //{
//    //    // ... 保持你原有的攻击检测逻辑 ...
//    //    return false;
//    //}
//}


//public class EnemyStateIdle : IEnemyState
//{
//    // 不断检测玩家，直到->1.玩家出现在自己的检测范围内，大于攻击范围-->切换到追逐状态；2.玩家出现在攻击范围内->切换到攻击状态；
//    private EnemyController _enemy;
//    private NetworkObject _target;
//    public EnemyStateIdle(EnemyController enemy)
//    {
//        _enemy = enemy;
//    }
//    public void Enter()
//    {
//        Debug.Log("怪物进入idle状态");
//        _target = null;
//    }

//    public void Exit()
//    {
//    }

//    public void Update()
//    {
//        DetectPlayer();// 检测玩家
//        //if (ChangeStateToSkill()) return;
//        if (ChangeStateToChasing()) return;
//    }
//    // idle -> chase / attack
//    private bool ChangeStateToChasing()
//    {   // 1.玩家出现在自己的检测范围内，大于攻击范围-- > 切换到追逐状态；2.玩家出现在攻击范围内->切换到攻击状态；
//        if (_target == null) return false;
//        if(Vector3.Distance(_target.transform.position, _enemy.transform.position) > _enemy.ChaseRange ) return false;

//        _enemy.ChangeState(new EnemyStateMove(_enemy, _target));
//        return true;
//    }
//    private bool ChangeStateToSkill()
//    {
//        // 2.玩家出现在攻击范围内->切换到攻击状态；
//        if (_target == null) return false;
//        if (Vector3.Distance(_target.transform.position, _enemy.transform.position) > _enemy.AttackRange ) return false;

//        _enemy.ChangeState(new EnemyStateAttack(_enemy, _target));
//        return true;
//    }
//    private void DetectPlayer()
//    {
//        if (_target != null) return;    // 防止重复检测
//        // 不断检测玩家
//        var colliderInfos = Physics.OverlapSphere(position: _enemy.transform.position, radius: _enemy.ChaseRange , layerMask: _enemy.ChaseLayer);
//        Debug.Log($"检测出了{colliderInfos.Length}个物体");
//        if(colliderInfos.Length <= 0 ) return;
//        foreach (Collider collider in colliderInfos)
//        {
//            Debug.Log($"collider name -> [{collider.name}], collider networkid -> [{collider.gameObject.GetComponent<NetworkObject>().NetworkObjectId}]");

//        }
//        // 选择第一个为目标
//        colliderInfos[0].TryGetComponent<NetworkObject>(out NetworkObject target);
//        Debug.Log($"target.name = {target.name}"); 
//        _target = target;
//    }

//}
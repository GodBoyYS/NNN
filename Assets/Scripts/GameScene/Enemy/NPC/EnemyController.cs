using System;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;

[RequireComponent(typeof(NavMeshAgent)), RequireComponent(typeof(NetworkObject))]
public class EnemyController : NetworkBehaviour
{
    public enum NPCMotionState
    {
        Idle = 0,
        Chase = 1,
        Attack = 2,
        Die = 3
    }
    [SerializeField] private float _chaseRange = 10f;
    [SerializeField] private float _attackRange = 5f;
    [SerializeField] private LayerMask _chaseLayer;
    // 优化：计时器，避免每帧重复计算路径
    private float _repathTimer = 0f;
    private float _repathInterval = 0.2f;
    private float _attackTimer = 0f;
    private float _attackInterval = 0.833f;
    private bool _isAttacking = false;
    public LayerMask ChaseLayer => _chaseLayer;
    private NavMeshAgent _agent;
    private NetworkObject _target;
    


    #region public property
    public NavMeshAgent Agent => _agent;
    public float AttackRange => _attackRange;
    public float ChaseRange => _chaseRange;
    #endregion

    #region network variable
    private NetworkVariable<NPCMotionState> _currentEnmeyState = new NetworkVariable<NPCMotionState>(
        NPCMotionState.Idle,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
        );
    public NetworkVariable<NPCMotionState> Motion => _currentEnmeyState;
    public NPCMotionState MotionStateVar => _currentEnmeyState.Value;
    #endregion
    public override void OnNetworkSpawn()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (IsServer)
        {
            // 服务器：启用寻路，初始化状态机
            _target = null;
            _agent.enabled = true;
            _currentEnmeyState.Value = NPCMotionState.Idle;
            //_currentBossState.Value = BossMotionState.Idle;
        }
        else
        {
            // 客户端，必须关闭自动寻路，防止于networktransform冲突
            _agent.enabled = false;
            // 客户端如果不需要跑状态机逻辑（纯表现），可以不初始化 _currenState
            // 或者初始化一个只负责播放特效的 dummy state
        }
    }
    private void Update()
    {
        if (!IsServer) return;
        // 【优化】不要每帧都先 Detect 再 Chase
        // 而是根据当前状态决定行为
        switch (_currentEnmeyState.Value)
        {
            case NPCMotionState.Idle:
                LogicIdle();
                break;
            case NPCMotionState.Chase:
                LogicChase();
                break;
            case NPCMotionState.Attack:
                LogicAttack();
                break;
        }
    }
    // --- 各状态的具体逻辑 ---

    private void LogicIdle()
    {
        DetectPlayer(); // 只有 Idle 时才需要不断寻找目标
                        // 如果找到了目标，DetectPlayer 内部已经赋值了 _target
        if (_target != null)
        {
            // 找到了目标，根据距离决定直接攻击还是追逐
            float distance = Vector3.Distance(transform.position, _target.transform.position);

            if (distance <= _attackRange)
            {
                ChangeStateToAttack();
            }
            else
            {
                ChangeStateToChase();
            }
        }
    }

    private void LogicChase()
    {
        // 1. 丢失目标检测
        if (_target == null)
        {
            ChangeStateToIdle();
            return;
        }
        // 2. 距离检测
        float distance = Vector3.Distance(transform.position, _target.transform.position);
        // 如果超出追逐范围 -> 变回 Idle
        if (distance > _chaseRange * 1.2f) // 1.2倍容错
        {
            ChangeStateToIdle();
            return;
        }
        // 如果进入攻击范围 -> 变为 Attack
        if (distance <= _attackRange)
        {
            ChangeStateToAttack();
            return;
        }
        // 3. 执行移动 (只有在 Chase 状态才移动)
        ChasePlayerMovement();
    }

    private void LogicAttack()
    {
        if (_target == null)
        {
            ChangeStateToIdle();
            return;
        }
        // 1. 距离检测：如果玩家跑远了，变回追逐
        // 给一点容错，比如攻击范围是2米，玩家跑到2.5米再切换回追逐，防止在临界点鬼畜切换
        float distance = Vector3.Distance(transform.position, _target.transform.position);
        if (distance > _attackRange * 1.1f)
        {
            ChangeStateToChase();
            return;
        }
        // 2. 执行攻击逻辑
        AttackPlayerBehavior();
    }

    // --- 状态切换辅助方法 (把状态赋值和副作用分开) ---
    private void ChangeStateToIdle()
    {
        _currentEnmeyState.Value = NPCMotionState.Idle;
        _target = null;
        if (_agent.isOnNavMesh) _agent.ResetPath(); // 停止寻路
    }

    private void ChangeStateToChase()
    {
        _currentEnmeyState.Value = NPCMotionState.Chase;
        if (_agent.isOnNavMesh) _agent.isStopped = false; // 确保Agent开启
    }

    private void ChangeStateToAttack()
    {
        _currentEnmeyState.Value = NPCMotionState.Attack;
        if (_agent.isOnNavMesh) _agent.ResetPath(); // 【重要】攻击时必须停下脚步！
        //_isAttacking = false; // 重置攻击计时器状态
        _attackTimer = _attackInterval; // 可选：让它进入状态后立即可以攻击一次
    }
    // --- 具体的行为方法 (把Update里原来的代码搬过来) ---

    private void ChasePlayerMovement()
    {
        // 原来的 ChasePlayer 代码，去掉 target 为空的判断（外层判断过了）
        _repathTimer += Time.deltaTime;
        if (_repathTimer > _repathInterval)
        {
            _repathTimer = 0f;
            if (_agent.isOnNavMesh)
            {
                _agent.SetDestination(_target.transform.position);
            }
        }
    }

    private void AttackPlayerBehavior()
    {
        // 处理攻击CD
        _attackTimer += Time.deltaTime;
        if (_attackTimer > _attackInterval)
        {
            _attackTimer = 0f;
            Debug.Log($"对 {_target.name} 发起攻击！");
            // 这里执行具体的扣血逻辑
            if(_target.TryGetComponent<PlayerNetworkCore>(out PlayerNetworkCore targetAuth))
            {
                // 请求服务器对玩家造成伤害
                targetAuth.ApplyDamageServer(10, NetworkObjectId);
            }
        }
    }

    // ========== 状态处理方法 =========
    // 检测玩家
    private void DetectPlayer()
    {
        if (_target != null) return;    // 防止重复检测
        // 1. 获取范围内物体
        var colliderInfos = Physics.OverlapSphere(transform.position, _chaseRange, _chaseLayer);
        if (colliderInfos.Length <= 0) return;
        Debug.Log($"检测到{colliderInfos.Length}个对象");
        // 【关键修复 3】: 绝对不要用 colliderInfos[0] !!!
        // OverlapSphere 返回顺序是不确定的。
        // 如果索引 [0] 是怪物自己（如果层级设置重叠），或者是一个死掉的玩家，逻辑就会出错。
        // 必须遍历寻找“最近的、有效的”玩家。
        NetworkObject bestTarget = null;
        float minDistance = float.MaxValue;
        foreach (Collider collider in colliderInfos)
        {
            // 排除自己
            if (collider.gameObject == gameObject) continue;
            // 尝试获取 NetworkObject
            if (!collider.TryGetComponent<NetworkObject>(out NetworkObject netObj)) continue;
            Debug.Log($"netobject.id -> [{netObj.NetworkObjectId}]");
            // 简单的距离比对，找最近的
            float d = Vector3.Distance(collider.transform.position, transform.position);
            if (d < minDistance)
            {
                minDistance = d;
                bestTarget = netObj;
            }
        }
        if (bestTarget != null)
        {
            Debug.Log($"锁定目标: {bestTarget.name}");
            _target = bestTarget;
        }
    }
 
}


/*
     * 怪物状态明确：（暂时不加入巡逻状态）
     * 怪物最初在idle状态：不断检测玩家，直到->1.玩家出现在自己的检测范围内，大于攻击范围-->切换到追逐状态；2.玩家出现在攻击范围内->切换到攻击状态；
     * 怪物进入追逐状态：持续追逐敌人，直到-->1.玩家进入攻击范围，切换到攻击状态；2.玩家逃脱追逐范围，切换到idle状态
     * 怪物进入攻击状态：持续攻击敌人，直到-->1.玩家死亡，切换到idle状态；2.玩家跑出攻击范围，切换到追逐状态；
     */


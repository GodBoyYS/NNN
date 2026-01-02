using System;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;

[RequireComponent(typeof(NavMeshAgent)), RequireComponent(typeof(NetworkObject))]
public class EnemyController : NetworkBehaviour,
    IDamageable
{
    public enum NPCMotionState
    {
        Idle = 0,
        Chase = 1,
        Attack = 2,
        Die = 3
    }
    [SerializeField] private float _chaseRange = 10f;
    //[SerializeField] private float _attackRange = 5f;
    [SerializeField] private LayerMask _chaseLayer;
    // 优化：计时器，避免每帧重复计算路径
    private float _repathTimer = 0f;
    private float _repathInterval = 0.2f;
    private float _attackTimer = 0f;
    private float _attackInterval = 0.833f;
    public LayerMask ChaseLayer => _chaseLayer;
    private NavMeshAgent _agent;
    private NetworkObject _target;

    [Header("Skill Settings")]
    [SerializeField] private SkillDataSO _skillData; // 核心：配置技能数据
    [SerializeField] private float _triggerAttackRange = 5f;


    #region public property
    public NavMeshAgent Agent => _agent;
    //public float AttackRange => _attackRange;
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
    private NetworkVariable<int> _currentHealth = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
        );
    // 同理
    public int MaxHealth => _maxHealth;
    public int CurrentHealth => _currentHealth.Value;
    [SerializeField] private int _maxHealth = 100;
    #endregion
    
    private float _skillTimer = 0f; // 技能冷却计时器
    public string GetSkillAnimationName()
    {
        return _skillData.skillActiveAnimationName;
    }

    #region public events
    public event Action<int, int> OnHealthChanged; // 事件
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
            _currentHealth.Value = _maxHealth;

            // 初始化 CD，避免怪物一出生就秒放技能，可以给一点随机延迟
            _skillTimer = 1.0f;
        }
        else
        {
            // 客户端，必须关闭自动寻路，防止于networktransform冲突
            _agent.enabled = false;
            // 客户端如果不需要跑状态机逻辑（纯表现），可以不初始化 _currenState
            // 或者初始化一个只负责播放特效的 dummy state
        }
        _currentHealth.OnValueChanged += (prev, curr) => OnHealthChanged?.Invoke(curr, _maxHealth);
        OnHealthChanged?.Invoke(_currentHealth.Value, _maxHealth);
    }
    private void Update()
    {
        if (!IsServer) return;
        // 更新技能冷却
        if (_skillTimer > 0) _skillTimer -= Time.deltaTime;
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
        DetectPlayer();
        if (_target != null)
        {
            float distance = Vector3.Distance(transform.position, _target.transform.position);
            // 使用配置的触发距离
            if (distance <= _triggerAttackRange)
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
        if (_target == null)
        {
            ChangeStateToIdle();
            return;
        }

        float distance = Vector3.Distance(transform.position, _target.transform.position);

        // 这里的脱战距离可以稍微大一点
        if (distance > _chaseRange * 1.5f)
        {
            ChangeStateToIdle();
            return;
        }

        // 进入攻击范围
        if (distance <= _triggerAttackRange)
        {
            ChangeStateToAttack();
            return;
        }

        ChasePlayerMovement();
    }

    private void LogicAttack()
    {
        if (_target == null)
        {
            ChangeStateToIdle();
            return;
        }

        float distance = Vector3.Distance(transform.position, _target.transform.position);

        // 如果玩家跑远了，切换回追击
        if (distance > _triggerAttackRange * 1.2f)
        {
            ChangeStateToChase();
            return;
        }

        // 停止移动
        if (_agent.isOnNavMesh) _agent.ResetPath();

        // 1. 始终面向目标 (这一步很重要，特别是远程怪)
        Vector3 direction = (_target.transform.position - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 10f * Time.deltaTime);
        }

        // 2. 尝试释放技能
        if (_skillTimer <= 0 && _skillData != null)
        {
            // 重置 CD
            _skillTimer = _skillData.coolDown;

            // 调用技能系统的 Cast
            // 注意：Cast 会在本地启动 Coroutine 处理 DelayEffect 等
            // 如果技能是 FlyingBullet，它会生成子弹 NetworkObject
            // 如果技能是 DamageEffect，它会直接造成伤害
            _skillData.Cast(gameObject, _target.gameObject, _target.transform.position);

            Debug.Log($"Enemy Cast Skill: {_skillData.name}");
        }
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

    public void TakeDamage(int amount, ulong attackerId)
    {
        if (!IsServer) return;
        int newHealth = _currentHealth.Value - amount;
        if (newHealth < 0) newHealth = 0;
        _currentHealth.Value = newHealth;
        if (newHealth <= 0)
        {
            // Die logic
        }
    }
}
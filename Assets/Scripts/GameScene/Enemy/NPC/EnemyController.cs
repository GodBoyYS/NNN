using System;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent)), RequireComponent(typeof(NetworkObject))]
public class EnemyController : NetworkBehaviour, 
    IDamageable
{
    public enum NPCMotionState { Idle = 0, Chase = 1, Attack = 2, Die = 3 }

    [Header("Detection Settings")]
    [SerializeField] private float _chaseRange = 10f; // 仅保留追逐（索敌）范围
    [SerializeField] private LayerMask _chaseLayer;

    [Header("Skill Settings")]
    [SerializeField] private SkillDataSO _skillData;
    // _triggerAttackRange 已移除，现在使用 _skillData.castRadius

    private float _repathTimer = 0f;
    private float _repathInterval = 0.2f;

    // 攻击间隔计时器（防止无CD技能连续播放动画太快）
    private float _attackTimer = 0f;
    private float _attackInterval = 0.833f;

    public LayerMask ChaseLayer => _chaseLayer;
    private NavMeshAgent _agent;
    private NetworkObject _target;

    #region public property
    public NavMeshAgent Agent => _agent;
    public float ChaseRange => _chaseRange;
    public SkillDataSO SkillData => _skillData;
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
    public int MaxHealth => _maxHealth;
    public int CurrentHealth => _currentHealth.Value;
    [SerializeField] private int _maxHealth = 100;
    #endregion

    private float _skillTimer = 0f;

    public string GetSkillAnimationName()
    {
        return _skillData != null ? _skillData.activeAnimationName : "Attack";
    }

    #region public events
    public event Action<int, int> OnHealthChanged;
    public event Action<NetworkObject> OnDied;
    #endregion

    public override void OnNetworkSpawn()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (IsServer)
        {
            _target = null;
            _agent.enabled = true;
            _currentEnmeyState.Value = NPCMotionState.Idle;
            _currentHealth.Value = _maxHealth;
            _skillTimer = 0f; // 初始可以直接释放技能

            // 动态设置 NavMeshAgent 的停止距离，防止远程怪一定要走到脸上才停
            if (_skillData != null)
            {
                // 稍微设置得比技能半径小一点点，确保能进入判定范围
                _agent.stoppingDistance = Mathf.Max(1.0f, _skillData.castRadius - 0.5f);
            }
        }
        else
        {
            _agent.enabled = false;
        }

        _currentHealth.OnValueChanged += (prev, curr) => OnHealthChanged?.Invoke(curr, _maxHealth);
        OnHealthChanged?.Invoke(_currentHealth.Value, _maxHealth);
    }

    private void Update()
    {
        if (!IsServer) return;

        // 冷却时间更新
        if (_skillTimer > 0) _skillTimer -= Time.deltaTime;

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

    private void LogicIdle()
    {
        DetectPlayer();

        if (_target != null)
        {
            float distance = Vector3.Distance(transform.position, _target.transform.position);
            float requiredRange = GetRequiredAttackRange();

            if (distance <= requiredRange)
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

        // 1. 如果超出最大追击距离（仇恨范围），放弃
        if (distance > _chaseRange * 1.5f)
        {
            ChangeStateToIdle();
            return;
        }

        // 2. 核心修改：判断是否进入技能释放半径
        float requiredRange = GetRequiredAttackRange();

        if (distance <= requiredRange)
        {
            ChangeStateToAttack();
            return;
        }

        // 3. 否则继续追击
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
        float requiredRange = GetRequiredAttackRange();

        // 1. 距离判定：给予一点缓冲空间 (Hysteresis)，防止在临界点反复抽搐
        // 如果玩家跑出了 (技能半径 * 1.1)，则重新开始追
        if (distance > requiredRange * 1.1f)
        {
            ChangeStateToChase();
            return;
        }

        // 2. 攻击时确保停止移动
        if (_agent.isOnNavMesh && !_agent.isStopped)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }

        // 3. 始终朝向目标
        Vector3 direction = (_target.transform.position - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 10f * Time.deltaTime);
        }

        // 4. 尝试释放技能
        if (_skillTimer <= 0 && _skillData != null)
        {
            // 重置冷却
            _skillTimer = _skillData.coolDown;

            // 释放技能
            _skillData.Cast(gameObject, _target.gameObject, _target.transform.position);
            Debug.Log($"Enemy Cast Skill: {_skillData.name}");
        }
    }

    // 辅助方法：获取当前配置的攻击距离，如果没有配置则给个默认值
    private float GetRequiredAttackRange()
    {
        if (_skillData != null)
        {
            return _skillData.castRadius;
        }
        return 2.0f; // 默认近战距离
    }

    private void ChangeStateToIdle()
    {
        if (_currentEnmeyState.Value == NPCMotionState.Idle) return;
        _currentEnmeyState.Value = NPCMotionState.Idle;
        _target = null;
        if (_agent.isOnNavMesh) _agent.ResetPath();
    }

    private void ChangeStateToChase()
    {
        if (_currentEnmeyState.Value == NPCMotionState.Chase) return;
        _currentEnmeyState.Value = NPCMotionState.Chase;

        // 确保 NavMeshAgent 参数匹配当前的技能距离
        if (_agent.isOnNavMesh && _skillData != null)
        {
            // 停止距离设置为技能半径的一半或者稍微短一点，保证能走进射程
            // 如果是远程怪(castRadius=10)，它会在距离玩家8-9米处停下
            float stopDist = Mathf.Max(0.5f, _skillData.castRadius * 0.8f);
            _agent.stoppingDistance = stopDist;
            _agent.isStopped = false;
        }
    }

    private void ChangeStateToAttack()
    {
        if (_currentEnmeyState.Value == NPCMotionState.Attack) return;
        _currentEnmeyState.Value = NPCMotionState.Attack;
        if (_agent.isOnNavMesh) _agent.ResetPath();

        // 进入攻击状态时不立即重置计时器，而是依赖 Update 中的冷却判断
        // 这样可以支持进入攻击范围后稍微有一点反应时间，或者立即攻击（取决于之前的冷却）
    }

    private void ChasePlayerMovement()
    {
        _repathTimer += Time.deltaTime;
        if (_repathTimer > _repathInterval)
        {
            _repathTimer = 0f;
            if (_agent.isOnNavMesh && _target != null)
            {
                _agent.SetDestination(_target.transform.position);
            }
        }
    }

    private void DetectPlayer()
    {
        if (_target != null) return;

        var colliderInfos = Physics.OverlapSphere(transform.position, _chaseRange, _chaseLayer);
        if (colliderInfos.Length <= 0) return;

        NetworkObject bestTarget = null;
        float minDistance = float.MaxValue;

        foreach (Collider collider in colliderInfos)
        {
            if (collider.gameObject == gameObject) continue;
            if (!collider.TryGetComponent<PlayerDataContainer>(out PlayerDataContainer playerData)) continue;

            // 简单的可见性检查（防止隔墙吸仇恨，可选）
            // if (Physics.Linecast(transform.position + Vector3.up, collider.transform.position + Vector3.up, GroundLayer)) continue;

            float d = Vector3.Distance(collider.transform.position, transform.position);
            if (d < minDistance)
            {
                minDistance = d;
                bestTarget = playerData.GetComponent<NetworkObject>();
            }
        }

        if (bestTarget != null)
        {
            _target = bestTarget;
        }
    }

    public void TakeDamage(int amount, ulong attackerId)
    {
        if (!IsServer) return;
        int newHealth = _currentHealth.Value - amount;
        if (newHealth < 0) newHealth = 0;
        _currentHealth.Value = newHealth;

        // 简单的反击逻辑：如果在挨打且没有目标，将攻击者设为目标
        if (_target == null && NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(attackerId, out var attackerObj))
        {
            _target = attackerObj;
            ChangeStateToChase();
        }

        if (newHealth <= 0)
        {
            // Die logic here
            // _currentEnmeyState.Value = NPCMotionState.Die;
            OnDied?.Invoke(NetworkObject);
            GetComponent<NetworkObject>().Despawn();
        }
    }
}
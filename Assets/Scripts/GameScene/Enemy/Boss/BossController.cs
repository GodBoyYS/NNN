using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

[RequireComponent(typeof(BossPresentation))]
[RequireComponent(typeof(NetworkObject))]
public class BossController : NetworkBehaviour, IDamageable
{
    public enum BossMotionState
    {
        Idle = 0,
        Chase = 1,
        Charge = 2,
        Skill = 3,
        Die = 4,
    }

    [Header("References")]
    [SerializeField] private BossPresentation _view;
    [SerializeField] private SkillDataSO[] _skills;

    [Header("Settings")]
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] public float ChaseRange = 50f;      // Public for State Access
    [SerializeField] public float BasicAttackRange = 3.5f; // Public for State Access
    [SerializeField] public LayerMask ChaseLayer;        // Public for State Access
    [SerializeField] private float _specialSkillInterval = 15f;

    // --- State Data ---
    public NavMeshAgent Agent => _view.Agent;
    public BossPresentation View => _view;
    public NetworkObject Target { get; private set; }
    public SkillDataSO[] Skills => _skills;

    private float[] _skillCDs;
    private float _specialSkillTimer = 0f;
    private bool _hasTriggered50Ult = false;
    private bool _hasTriggered10Ult = false;

    // --- Network Variables ---
    private NetworkVariable<BossMotionState> _currentBossState = new NetworkVariable<BossMotionState>(
        BossMotionState.Idle, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );
    private NetworkVariable<int> _currentHealth = new NetworkVariable<int>(
        100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );
    private NetworkVariable<int> _currentSkillIndex = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    public BossMotionState MotionState => _currentBossState.Value;
    public int CurrentSkillIdx => _currentSkillIndex.Value;
    public SkillDataSO CurrentSkillData => (_skills != null && CurrentSkillIdx >= 0 && CurrentSkillIdx < _skills.Length) ? _skills[CurrentSkillIdx] : null;

    public event Action OnBossDied;
    private BossStateMachine _stateMachine;

    private void Awake()
    {
        if (_view == null) _view = GetComponent<BossPresentation>();
        _stateMachine = new BossStateMachine(this);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _currentHealth.Value = _maxHealth;
            _currentBossState.Value = BossMotionState.Idle;
            _currentSkillIndex.Value = 0;
            Agent.enabled = true;
            Target = null;
            if (_skills != null) _skillCDs = new float[_skills.Length];
        }
        else
        {
            Agent.enabled = false;
        }

        // 核心：监听网络变量变化，驱动本地状态机
        _currentBossState.OnValueChanged += OnStateNetworkValueChanged;

        // 初始化初始状态
        SyncStateFromNetwork(_currentBossState.Value);
    }

    public override void OnNetworkDespawn()
    {
        _currentBossState.OnValueChanged -= OnStateNetworkValueChanged;
    }

    private void Update()
    {
        // 1. 服务端处理冷却时间
        if (IsServer)
        {
            UpdateTimers();
        }

        // 2. 状态机驱动 (Server运行逻辑, Server+Client运行表现)
        _stateMachine.Update();
    }

    private void UpdateTimers()
    {
        if (_currentBossState.Value == BossMotionState.Die) return;

        if (_skillCDs != null)
        {
            for (int i = 0; i < _skillCDs.Length; i++)
                if (_skillCDs[i] > 0) _skillCDs[i] -= Time.deltaTime;
        }
        if (Target != null) _specialSkillTimer += Time.deltaTime;
    }

    // --- 状态同步逻辑 ---

    private void OnStateNetworkValueChanged(BossMotionState oldState, BossMotionState newState)
    {
        SyncStateFromNetwork(newState);
    }

    private void SyncStateFromNetwork(BossMotionState state)
    {
        switch (state)
        {
            case BossMotionState.Idle:
                _stateMachine.ChangeState(_stateMachine.StateIdle);
                break;
            case BossMotionState.Chase:
                _stateMachine.ChangeState(_stateMachine.StateMove);
                break;
            case BossMotionState.Charge:
                _stateMachine.ChangeState(_stateMachine.StateCharge);
                break;
            case BossMotionState.Skill:
                _stateMachine.ChangeState(_stateMachine.StateSkill);
                break;
            case BossMotionState.Die:
                _stateMachine.ChangeState(_stateMachine.StateDie);
                break;
        }
    }

    // --- 公共接口供 State 调用 (仅 Server) ---

    public void SetState(BossMotionState newState)
    {
        if (!IsServer) return;
        if (_currentBossState.Value != newState)
        {
            _currentBossState.Value = newState;
            // Server端本地也会收到 OnValueChanged 回调从而切换状态
        }
    }

    public void SetTarget(NetworkObject target)
    {
        Target = target;
    }

    public void RotateTowardsTarget()
    {
        if (Target == null) return;
        Vector3 dir = (Target.transform.position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
    }

    /// <summary>
    /// 尝试选择一个技能并发起攻击
    /// </summary>
    public bool TrySelectAndStartAttack()
    {
        int skillIndex = SelectSkillToCast();
        if (skillIndex != -1)
        {
            _currentSkillIndex.Value = skillIndex;
            // 扣除CD
            if (_skills != null && skillIndex < _skills.Length)
                _skillCDs[skillIndex] = _skills[skillIndex].coolDown;

            // 进入蓄力状态 (这会自动同步给Client)
            SetState(BossMotionState.Charge);
            return true;
        }
        return false;
    }

    private int SelectSkillToCast()
    {
        // 保持原有的AI决策逻辑
        float hpPercent = (float)_currentHealth.Value / _maxHealth;
        int ultIndex = 3;
        if (_skills.Length > ultIndex)
        {
            if (hpPercent <= 0.1f && !_hasTriggered10Ult)
            {
                _hasTriggered10Ult = true;
                _skillCDs[ultIndex] = 0;
                return ultIndex;
            }
            if (hpPercent <= 0.5f && !_hasTriggered50Ult)
            {
                _hasTriggered50Ult = true;
                _skillCDs[ultIndex] = 0;
                return ultIndex;
            }
        }

        if (_specialSkillTimer >= _specialSkillInterval)
        {
            List<int> readySpecials = new List<int>();
            if (_skills.Length > 1 && _skillCDs[1] <= 0) readySpecials.Add(1);
            if (_skills.Length > 2 && _skillCDs[2] <= 0) readySpecials.Add(2);
            if (readySpecials.Count > 0)
            {
                _specialSkillTimer = 0f;
                return readySpecials[Random.Range(0, readySpecials.Count)];
            }
        }

        if (_skills.Length > 0 && _skillCDs[0] <= 0) return 0;
        return -1;
    }

    public void TakeDamage(int amount, ulong attackerId)
    {
        if (!IsServer) return;
        if (_currentBossState.Value == BossMotionState.Die) return;

        int newHealth = Mathf.Max(_currentHealth.Value - amount, 0);
        _currentHealth.Value = newHealth;

        if (newHealth <= 0)
        {
            SetState(BossMotionState.Die);
            OnBossDied?.Invoke();
            return;
        }

        // 仇恨机制：如果没目标，谁打我我打谁
        if (Target == null && NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(attackerId, out var attacker))
        {
            Target = attacker;
        }
    }
    public void TageDamage(int amount, ulong attackerId) => TakeDamage(amount, attackerId);

    // =========================================================
    // 新增：视觉特效相关的 RPC 接口
    // =========================================================

    /// <summary>
    /// 供 State 调用，触发全客户端的蓄力特效
    /// </summary>
    public void TriggerChargeVisuals(Vector3 pos, float duration)
    {
        // 获取当前技能索引，通过 RPC 发送给客户端
        int skillIndex = _currentSkillIndex.Value;
        SpawnChargeVisualsClientRpc(skillIndex, pos, duration);
    }

    [ClientRpc]
    private void SpawnChargeVisualsClientRpc(int skillIndex, Vector3 pos, float duration)
    {
        // 1. 安全检查
        if (_skills == null || skillIndex < 0 || skillIndex >= _skills.Length) return;

        var skillData = _skills[skillIndex];
        var prefabs = skillData.chargeVisualPrefabs;

        if (prefabs == null || prefabs.Count == 0) return;

        // 2. 遍历生成所有配置的特效
        foreach (var prefab in prefabs)
        {
            if (prefab == null) continue;

            // 实例化特效（纯本地对象，不需要 NetworkSpawn）
            GameObject instance = Instantiate(prefab, pos, Quaternion.identity);

            // 3. 设置特效的生命周期和成长参数
            // 假设你有一个脚本专门控制特效缩放/消失
            if (instance.TryGetComponent<ChargeGrowingVisual>(out var visualScript))
            {
                visualScript.SetDuration(duration);
                //visualScript.Initialize(duration);
            }
            else
            {
                // 如果没有专用脚本，至少保证它会销毁
                Destroy(instance, duration + 0.5f);
            }
        }
    }
}



//using System;
//using System.Collections;
//using System.Collections.Generic;
//using Unity.Netcode;
//using UnityEngine;
//using UnityEngine.AI;
//using Random = UnityEngine.Random;

//[RequireComponent(typeof(NavMeshAgent)), RequireComponent(typeof(NetworkObject))]
//public class BossController : NetworkBehaviour, IDamageable
//{
//    public enum NewBossMotionState
//    {
//        Idle = 0,
//        Chase = 1,
//        SkillCharge = 2,
//        SkillActive = 3,
//        SkillRecovery = 4,
//        Die = 5,
//    }
//    public enum BossMotionState
//    {
//        Idle = 0,
//        Chase = 1,
//        Charge = 2,
//        Skill = 3,
//        Die = 4,
//    }

//    [Header("AI 基础设置")]
//    [SerializeField] private int _maxHealth = 100;
//    [SerializeField] private float _chaseRange = 50f;
//    [SerializeField] private float _basicAttackRange = 3.5f;
//    [SerializeField] private LayerMask _chaseLayer;

//    [Header("技能配置")]
//    [SerializeField] private SkillDataSO[] _skills;

//    [Header("AI 战斗节奏")]
//    [SerializeField] private float _specialSkillInterval = 15f;

//    // --- 运行时数据 ---
//    private NavMeshAgent _agent;
//    private NetworkObject _target;
//    private float[] _skillCDs;

//    // 状态控制
//    private bool _isCasting = false;
//    private float _specialSkillTimer = 0f;
//    private bool _hasTriggered50Ult = false;
//    private bool _hasTriggered10Ult = false;

//    // 寻路优化
//    private float _repathTimer = 0f;
//    private float _repathInterval = 0.2f;

//    #region Network Variables
//    private NetworkVariable<BossMotionState> _currentBossState = new NetworkVariable<BossMotionState>(
//        BossMotionState.Idle, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
//    );

//    private NetworkVariable<int> _currentHealth = new NetworkVariable<int>(
//        100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
//    );

//    private NetworkVariable<int> _currentSkillIndex = new NetworkVariable<int>(
//        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
//    );

//    public BossMotionState MotionStateVar => _currentBossState.Value;
//    public NetworkVariable<BossMotionState> Motion => _currentBossState;
//    public NetworkVariable<int> SkillIndexVar => _currentSkillIndex;
//    #endregion

//    public event Action OnBossDied;


//    private BossStateMachine _stateMachine;
//    private BossPresentation _view;
//    public void Awake()
//    {
//        _view = GetComponent<BossPresentation>();
//        _stateMachine = new BossStateMachine(this, _view);
//    }

//    public override void OnNetworkSpawn()
//    {
//        _agent = GetComponent<NavMeshAgent>();
//        if (_skills != null) _skillCDs = new float[_skills.Length];

//        if (IsServer)
//        {
//            _currentHealth.Value = _maxHealth;
//            _currentBossState.Value = BossMotionState.Idle;
//            _currentSkillIndex.Value = 0;
//            _agent.enabled = true;
//            _target = null;
//        }
//        else
//        {
//            _agent.enabled = false;
//        }
//    }

//    private void Update()
//    {
//        _stateMachine.Update();


//        if (!IsServer) return;
//        if (_currentBossState.Value == BossMotionState.Die) return;

//        UpdateTimers();

//        switch (_currentBossState.Value)
//        {
//            case BossMotionState.Idle: LogicIdle(); break;
//            case BossMotionState.Chase: LogicChase(); break;
//            case BossMotionState.Skill: LogicSkill(); break;
//        }
//    }

//    private void UpdateTimers()
//    {
//        if (_skillCDs != null)
//        {
//            for (int i = 0; i < _skillCDs.Length; i++)
//                if (_skillCDs[i] > 0) _skillCDs[i] -= Time.deltaTime;
//        }
//        if (_target != null) _specialSkillTimer += Time.deltaTime;
//    }

//    public void TakeDamage(int amount, ulong attackerId)
//    {
//        if (!IsServer) return;
//        if (_currentBossState.Value == BossMotionState.Die) return;

//        int newHealth = Mathf.Max(_currentHealth.Value - amount, 0);
//        _currentHealth.Value = newHealth;

//        if (newHealth <= 0)
//        {
//            ChangeState(BossMotionState.Die);
//            OnBossDied?.Invoke();
//            return;
//        }

//        if (_target == null && NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(attackerId, out var attacker))
//        {
//            _target = attacker;
//        }
//    }
//    public void TageDamage(int amount, ulong attackerId) => TakeDamage(amount, attackerId); // 兼容接口拼写错误

//    // --- 核心逻辑修改：尝试发动攻击 ---
//    // 这个方法负责：先选技能 -> 设置索引 -> 再切换状态
//    private bool TryStartAttack()
//    {
//        int skillIndex = SelectSkillToCast();

//        if (skillIndex != -1)
//        {
//            // 1. 先设置索引！确保客户端收到 State=Skill 时，索引已经是新的了
//            _currentSkillIndex.Value = skillIndex;

//            // 2. 再切换状态
//            ChangeState(BossMotionState.Skill);

//            // 3. 开始服务器端的技能逻辑协程
//            StartCoroutine(PerformSkillRoutine(skillIndex));
//            return true;
//        }
//        return false;
//    }

//    // [新增] 蓄力阶段的逻辑
//    private void LogicCharge()
//    {
//        // 在蓄力时，BOSS 通常会继续盯着玩家（转头），直到技能释放前一刻
//        RotateTowardsTarget();
//    }

//    private void LogicIdle()
//    {
//        DetectPlayer();
//        if (_target != null)
//        {
//            float dist = Vector3.Distance(transform.position, _target.transform.position);
//            if (dist <= _basicAttackRange)
//            {
//                // 尝试攻击，如果所有技能CD都在转，则保持Idle或者切换Chase
//                if (!TryStartAttack())
//                {
//                    // 如果没技能可放（都在CD），保持Idle发呆一会
//                }
//            }
//            else
//            {
//                ChangeState(BossMotionState.Chase);
//            }
//        }
//    }

//    private void LogicChase()
//    {
//        if (_target == null) { ChangeState(BossMotionState.Idle); return; }

//        float dist = Vector3.Distance(transform.position, _target.transform.position);
//        if (dist > _chaseRange * 1.5f) { ChangeState(BossMotionState.Idle); return; }

//        if (dist <= _basicAttackRange)
//        {
//            // 尝试攻击，成功则切状态，失败（都在CD）则不切，继续保持贴脸
//            if (TryStartAttack()) return;
//        }

//        // 移动逻辑
//        _repathTimer += Time.deltaTime;
//        if (_repathTimer > _repathInterval)
//        {
//            _repathTimer = 0f;
//            if (_agent.isOnNavMesh) _agent.SetDestination(_target.transform.position);
//        }
//    }

//    private void LogicSkill()
//    {
//        // 这里只需要处理施法中需要持续更新的逻辑（比如旋转），不需要再选技能了
//        if (!_isCasting)
//        {
//            // 技能放完了，协程结束将 _isCasting 设为 false
//            // 此时应该切回 Idle 或 Chase
//            ChangeState(BossMotionState.Idle);
//            return;
//        }

//        RotateTowardsTarget();
//    }

//    private int SelectSkillToCast()
//    {
//        float hpPercent = (float)_currentHealth.Value / _maxHealth;
//        int ultIndex = 3;

//        // 大招判定
//        if (_skills.Length > ultIndex)
//        {
//            if (hpPercent <= 0.1f && !_hasTriggered10Ult)
//            {
//                _hasTriggered10Ult = true;
//                _skillCDs[ultIndex] = 0;
//                return ultIndex;
//            }
//            if (hpPercent <= 0.5f && !_hasTriggered50Ult)
//            {
//                _hasTriggered50Ult = true;
//                _skillCDs[ultIndex] = 0;
//                return ultIndex;
//            }
//        }

//        // 特殊技能判定
//        if (_specialSkillTimer >= _specialSkillInterval)
//        {
//            List<int> readySpecials = new List<int>();
//            if (_skills.Length > 1 && _skillCDs[1] <= 0) readySpecials.Add(1);
//            if (_skills.Length > 2 && _skillCDs[2] <= 0) readySpecials.Add(2);

//            if (readySpecials.Count > 0)
//            {
//                _specialSkillTimer = 0f;
//                return readySpecials[Random.Range(0, readySpecials.Count)];
//            }
//        }

//        // 普攻判定
//        if (_skills.Length > 0 && _skillCDs[0] <= 0)
//        {
//            return 0;
//        }

//        return -1;
//    }

//    // [核心修改] 技能执行协程：Charge -> Skill
//    // --- 修改 PerformSkillRoutine 协程 ---
//    private IEnumerator PerformSkillRoutine(int index)
//    {
//        _isCasting = true;
//        var skillData = _skills[index];
//        _skillCDs[index] = skillData.coolDown;

//        // 1. 确定位置 (快照)
//        Vector3 finalCastPos = transform.position;
//        bool isSelfCentered = index == 3 || skillData.isSelfCentered;
//        if (!isSelfCentered && _target != null)
//            finalCastPos = _target.transform.position;

//        // 2. 进入蓄力状态 (Charge Phase)
//        ChangeState(BossMotionState.Charge);

//        float chargeTime = skillData.chargeDuration;

//        if (chargeTime > 0)
//        {
//            // A. 处理原有的红圈预警 (Warning Prefab)
//            if (skillData.warningPrefab != null)
//            {
//                float diameter = (index == 3) ? 10.0f : 4.0f; // 建议后续也放入 SO
//                ShowWarningClientRpc(index, finalCastPos, chargeTime, diameter);
//            }

//            // B. [新增] 处理技能专属的蓄力特效 (Charge Visual Prefabs)
//            // 比如：Nuke 的变大球体
//            if (skillData.chargeVisualPrefabs != null && skillData.chargeVisualPrefabs.Count > 0)
//            {
//                SpawnChargeVisualsClientRpc(index, finalCastPos, chargeTime);
//            }

//            Debug.Log($"[Server] 蓄力中... {chargeTime}s");
//            yield return new WaitForSeconds(chargeTime);
//        }

//        // 3. 进入释放状态 (Skill Phase)
//        ChangeState(BossMotionState.Skill);

//        Debug.Log($"[Server] 释放技能逻辑");
//        skillData.Cast(gameObject, _target != null ? _target.gameObject : null, finalCastPos);

//        // 后摇
//        float recoveryTime = (index == 0) ? 1.0f : 2.0f;
//        yield return new WaitForSeconds(recoveryTime);

//        _isCasting = false;
//        ChangeState(BossMotionState.Idle);
//    }
//    // [新增] 专门用于生成纯视觉蓄力特效的 RPC
//    [ClientRpc]
//    private void SpawnChargeVisualsClientRpc(int skillIndex, Vector3 pos, float duration)
//    {
//        // 安全检查
//        if (skillIndex < 0 || skillIndex >= _skills.Length) return;
//        var prefabs = _skills[skillIndex].chargeVisualPrefabs;
//        if (prefabs == null || prefabs.Count == 0) return;

//        foreach (var prefab in prefabs)
//        {
//            if (prefab == null) continue;

//            // 生成特效 (不需要 NetworkObject，因为纯客户端视觉)
//            // 这里的旋转默认 Identity，如果需要朝向 Boss 可以传参，目前 Nuke 球体不需要旋转
//            GameObject instance = Instantiate(prefab, pos, Quaternion.identity);

//            // 查找并初始化 "生长" 脚本
//            if (instance.TryGetComponent<ChargeGrowingVisual>(out var growingVisual))
//            {
//                growingVisual.SetDuration(duration);
//            }
//            // 如果你还有其他类型的视觉脚本，也可以在这里 switch 或 tryGetComponent
//            // else if (instance.TryGetComponent<ParticleSystem>(out var ps)) { ... }
//        }
//    }

//    [ClientRpc]
//    private void ShowWarningClientRpc(int skillIndex, Vector3 pos, float duration, float diameter)
//    {
//        if (skillIndex < 0 || skillIndex >= _skills.Length) return;
//        GameObject prefab = _skills[skillIndex].warningPrefab;
//        if (prefab == null) return;
//        GameObject instance = Instantiate(prefab, pos + Vector3.up * 0.1f, Quaternion.Euler(90, 0, 0));
//        if (instance.TryGetComponent<SkillIndicator>(out var indicator))
//            indicator.Initialize(duration, diameter);
//    }

//    private void RotateTowardsTarget()
//    {
//        if (_target == null) return;
//        Vector3 dir = (_target.transform.position - transform.position).normalized;
//        dir.y = 0;
//        if (dir != Vector3.zero)
//            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
//    }

//    private void ChangeState(BossMotionState newState)
//    {
//        if (_currentBossState.Value == newState) return;
//        _currentBossState.Value = newState;

//        if (newState == BossMotionState.Idle || newState == BossMotionState.Skill)
//        {
//            if (_agent.isOnNavMesh) _agent.ResetPath();
//        }
//        else if (newState == BossMotionState.Chase)
//        {
//            if (_agent.isOnNavMesh) _agent.isStopped = false;
//        }
//    }

//    private void DetectPlayer()
//    {
//        if (_target != null) return;
//        var hits = Physics.OverlapSphere(transform.position, _chaseRange, _chaseLayer);
//        foreach (var hit in hits)
//        {
//            if (hit.TryGetComponent<PlayerNetworkCore>(out var playerCore) && !playerCore.IsDead)
//            {
//                _target = hit.GetComponent<NetworkObject>();
//                return;
//            }
//        }
//    }

//    public string GetCurrentSkillAnimationName()
//    {
//        int idx = _currentSkillIndex.Value;
//        if (_skills != null && idx >= 0 && idx < _skills.Length)
//        {
//            return _skills[idx].animationName;
//        }
//        return "Idle";
//    }
//    // [新增] 获取蓄力动画名称
//    public string GetCurrentChargeAnimationName()
//    {
//        int idx = _currentSkillIndex.Value;
//        if (_skills != null && idx >= 0 && idx < _skills.Length)
//        {
//            // 如果没填，默认返回 Idle，避免报错
//            if (string.IsNullOrEmpty(_skills[idx].chargeAnimationName)) return "Idle";
//            return _skills[idx].chargeAnimationName;
//        }
//        return "Idle";
//    }
//}
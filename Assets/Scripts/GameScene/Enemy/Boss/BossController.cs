using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NavMeshAgent))]
public class BossController : NetworkBehaviour, 
    IDamageable
{
    public enum BossMotionState
    {
        Idle = 0,
        Chase = 1,
        Charge = 2,
        Skill = 3,
        Recovery = 4, // 新增枚举
        Die = 5,
    }

    [Header("References")]
    // [SerializeField] private BossPresentation _view; // Deprecated
    [SerializeField] private SkillDataSO[] _skills;

    [Header("Settings")]
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] public float ChaseRange = 50f;
    [SerializeField] public float BasicAttackRange = 3.5f;
    [SerializeField] public LayerMask ChaseLayer;
    [SerializeField] private float _specialSkillInterval = 15f;

    // 直接获取组件
    public NavMeshAgent Agent { get; private set; }
    public Animator Animator { get; private set; }

    // 兼容旧代码的 View 属性，但建议直接用上面的 Animator/Agent
    public BossPresentation View => GetComponent<BossPresentation>();

    public NetworkObject Target { get; private set; }
    public SkillDataSO[] Skills => _skills;

    private float[] _skillCDs;
    private float _specialSkillTimer = 0f;
    private bool _hasTriggered50Ult = false;
    private bool _hasTriggered10Ult = false;

    // Network Variables
    private NetworkVariable<BossMotionState> _currentBossState = new NetworkVariable<BossMotionState>(
        BossMotionState.Idle, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );
    private NetworkVariable<int> _currentHealth = new NetworkVariable<int>(
        100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    // 公开属性
    public int MaxHealth => _maxHealth;
    public int CurrentHealth => _currentHealth.Value;
    public BossMotionState MotionState => _currentBossState.Value;

    public event Action OnBossDied;
    public event Action<int, int> OnHealthChanged;

    private BossStateMachine _stateMachine;
    public BossStateMachine StateMachine => _stateMachine;

    private void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        Animator = GetComponent<Animator>();
        _stateMachine = new BossStateMachine(this);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _currentHealth.Value = _maxHealth;
            _currentBossState.Value = BossMotionState.Idle;
            Agent.enabled = true;
            Target = null;
            if (_skills != null) _skillCDs = new float[_skills.Length];
            foreach (var skill in _skills) skill.SetDurations();
        }
        else
        {
            Agent.enabled = false;
        }

        _currentBossState.OnValueChanged += OnStateNetworkValueChanged;
        SyncStateFromNetwork(_currentBossState.Value);

        _currentHealth.OnValueChanged += OnHealthNetworkChanged;
        OnHealthChanged?.Invoke(_currentHealth.Value, _maxHealth);
    }

    public override void OnNetworkDespawn()
    {
        _currentBossState.OnValueChanged -= OnStateNetworkValueChanged;
        _currentHealth.OnValueChanged -= OnHealthNetworkChanged;
    }

    private void Update()
    {
        if (IsServer)
        {
            UpdateTimers();
        }
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

    #region State Sync
    private void OnStateNetworkValueChanged(BossMotionState oldState, BossMotionState newState)
    {
        SyncStateFromNetwork(newState);
    }

    private void SyncStateFromNetwork(BossMotionState state)
    {
        switch (state)
        {
            case BossMotionState.Idle: _stateMachine.ChangeState(_stateMachine.StateIdle); break;
            case BossMotionState.Chase: _stateMachine.ChangeState(_stateMachine.StateMove); break;
            case BossMotionState.Charge: _stateMachine.ChangeState(_stateMachine.StateCharge); break;
            case BossMotionState.Skill: _stateMachine.ChangeState(_stateMachine.StateSkill); break;
            case BossMotionState.Recovery: _stateMachine.ChangeState(_stateMachine.StateRecovery); break;
            case BossMotionState.Die: _stateMachine.ChangeState(_stateMachine.StateDie); break;
        }
    }

    public void SetState(BossMotionState newState)
    {
        if (!IsServer) return;
        if (_currentBossState.Value != newState)
        {
            _currentBossState.Value = newState;
        }
    }
    #endregion

    #region Logic
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

    public bool TrySelectAndStartAttack()
    {
        int skillIndex = SelectSkillToCast();
        if (skillIndex != -1)
        {
            // 将选择的技能索引存入状态机上下文
            _stateMachine.PendingSkillIndex = skillIndex;

            if (_skills != null && skillIndex < _skills.Length)
                _skillCDs[skillIndex] = _skills[skillIndex].coolDown;

            SetState(BossMotionState.Charge);
            return true;
        }
        return false;
    }

    private int SelectSkillToCast()
    {
        float hpPercent = (float)_currentHealth.Value / _maxHealth;
        int ultIndex = 3;

        // 大招检测
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

        // 特殊技能检测
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

        // 普攻检测 (Index 0)
        if (_skills.Length > 0 && _skillCDs[0] <= 0) return 0;

        return -1;
    }

    public void TriggerChargeVisuals(Vector3 pos, float duration)
    {
        int skillIndex = _stateMachine.PendingSkillIndex;
        SpawnChargeVisualsClientRpc(skillIndex, pos, duration);
    }

    [ClientRpc]
    private void SpawnChargeVisualsClientRpc(int skillIndex, Vector3 pos, float duration)
    {
        if (_skills == null || skillIndex < 0 || skillIndex >= _skills.Length) return;
        var skillData = _skills[skillIndex];
        var prefabs = skillData.chargeVisualPrefabs;

        if (prefabs == null || prefabs.Count == 0) return;

        foreach (var prefab in prefabs)
        {
            if (prefab == null) continue;
            GameObject instance = Instantiate(prefab, pos, Quaternion.identity);
            if (instance.TryGetComponent<ChargeGrowingVisual>(out var visualScript))
            {
                visualScript.SetDuration(duration);
            }
            else
            {
                Destroy(instance, duration + 0.5f);
            }
        }
    }
    #endregion

    #region IDamageable
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

        if (Target == null && NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(attackerId, out var attacker))
        {
            Target = attacker;
            // 如果在 Idle 被打，可以尝试切到 Chase
            if (_currentBossState.Value == BossMotionState.Idle)
                SetState(BossMotionState.Chase);
        }
    }
    #endregion

    private void OnHealthNetworkChanged(int prev, int curr)
    {
        OnHealthChanged?.Invoke(curr, _maxHealth);
    }
}

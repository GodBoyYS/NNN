using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerNetworkCombat : NetworkBehaviour
{
    [Header("技能配置")]
    [Tooltip("Element 0 = 普攻, Element 1 = Q, Element 2 = W, Element 3 = E")]
    [SerializeField] private List<SkillDataSO> _skillSlots;

    private PlayerNetworkCore _core;
    private PlayerNetworkMovement _movement;

    #region net var
    // 当前正在释放的技能索引（-1 代表没有释放）
    private readonly NetworkVariable<int> _currentSkillIndex = new NetworkVariable<int>(
        -1, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );
    private readonly NetworkVariable<bool> _qSkillActive = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
        );
    private readonly NetworkVariable<bool> _wSkillActive = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
        );
    private readonly NetworkVariable<bool> _eSkillActive = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
        );

    public NetworkVariable<bool> QSkillActiveVar => _qSkillActive;
    public NetworkVariable<bool> WSkillActiveVar => _wSkillActive;
    public NetworkVariable<bool> ESkillActiveVar => _eSkillActive;

    #endregion

    // 冷却计时器数组 (对应 _skillSlots 的索引)
    private float[] _cooldownTimers;

    //public string GetSkillAnimationName() => _skillSlots[_currentSkillIndex.Value].animationName;
    public string GetSkillAnimationName() => _skillSlots[_currentSkillIndex.Value].skillActiveAnimationName;
    // 对外只读属性
    public int CurrentSkillIndex => _currentSkillIndex.Value;
    public int SkillCount => _skillSlots.Count;

    // 【新增】暴露事件给表现层
    public event Action<int> OnSkillIndexChanged;

    // 以前的“追逐”逻辑变量保留，但逻辑需要适配技能
    private bool _isChasing;

    private void Awake()
    {
        _core = GetComponent<PlayerNetworkCore>();
        _movement = GetComponent<PlayerNetworkMovement>();

        // 初始化冷却数组
        _cooldownTimers = new float[_skillSlots.Count];
        for(int i = 0; i < _skillSlots.Count; i++)
        {
            _cooldownTimers[i] = _skillSlots[i].coolDown;
        }
    }

    public override void OnNetworkSpawn()
    {
        // 无论Client还是Server，都需要监听数值变化并抛出事件
        _currentSkillIndex.OnValueChanged += HandleSkillIndexChanged;

        if (!IsServer) return;

        // 监听移动事件，打断施法或追逐（可选）
        // _movement.ServerOwnerIssuedMoveCommand += OnMoveCommandReceived;
    }
    public override void OnNetworkDespawn()
    {
        _currentSkillIndex.OnValueChanged -= HandleSkillIndexChanged;
    }
    // 【新增】中转事件
    private void HandleSkillIndexChanged(int prev, int curr)
    {
        OnSkillIndexChanged?.Invoke(curr);
    }

    private void Update()
    {
        if (!IsServer) return;
        if (_core.IsDead) return;

        // 1. 处理所有技能的冷却
        ProcessCooldowns();

        // 2. 处理追逐逻辑 (暂时保留空位)
        if (_isChasing) PerformChaseLogic();
    }

    private void ProcessCooldowns()
    {
        for (int i = 0; i < _cooldownTimers.Length; i++)
        {
            if (_cooldownTimers[i] > 0)
            {
                _cooldownTimers[i] -= Time.deltaTime;
            }
            else
            {
                // 冷却结束，恢复状态
                // 只有当它是 false 的时候，我们才去设为 true（减少网络带宽消耗，虽然 NV 只有变了才发）
                if (i == 1 && !_qSkillActive.Value) _qSkillActive.Value = true;
                if (i == 2 && !_wSkillActive.Value) _wSkillActive.Value = true;
                if (i == 3 && !_eSkillActive.Value) _eSkillActive.Value = true;

                // 绝对不要在这里写 return！
            }
        }
    }

    /// <summary>
    /// 获取当前的技能数据，供表现层(Presentation)查询动画名
    /// </summary>
    public SkillDataSO GetCurrentSkillData()
    {
        int index = _currentSkillIndex.Value;
        if (index < 0 || index >= _skillSlots.Count) return null;
        return _skillSlots[index];
    }

    // ==========================================
    //  Public API (客户端调用)
    // ==========================================

    /// <summary>
    /// 统一请求入口：普攻传 0，Q传 1...
    /// </summary>
    public void RequestCastSkill(int slotIndex, Vector3 aimPosition)
    {
        if (!IsOwner) return;
        // 可以在这里做客户端侧的 CD 预判，避免发无用包
        RequestCastSkillServerRpc(slotIndex, aimPosition);
    }

    [ServerRpc]
    private void RequestCastSkillServerRpc(int index, Vector3 aimPosition)
    {
        // 1. 基础校验
        if (_core.IsDead) return;
        if (index < 0 || index >= _skillSlots.Count) return;

        // 2. 冷却校验
        if (_cooldownTimers[index] > 0) return;

        // 3. 状态校验 (如果正在放其他技能，是否允许打断？通常普攻可以被技能打断，技能不能被普攻打断)
        // 这里简化处理：只要不是 Dead 就可以放 (或者你可以判断 Motion == Idle/Moving)
        // if (_core.Motion == PlayerNetworkStates.BossMotionState.Skill) return; 

        ExecuteSkill(index, aimPosition);
    }

    // ==========================================
    //  Server Logic
    // ==========================================

    private void ExecuteSkill(int index, Vector3 aimPosition)
    {
        switch (index)
        {
            case 1:
                _qSkillActive.Value = false;
                break;
            case 2:
                _wSkillActive.Value = false;
                break;
            case 3:
                _eSkillActive.Value = false;
                break;
            default:
                break;
        }
        SkillDataSO skillData = _skillSlots[index];

        // 1. 停止移动 (看设计需求，有的技能允许移动施法)
        _movement.ServerForceStop();
        _isChasing = false;

        // 2. 设置状态和索引
        _currentSkillIndex.Value = index;
        _core.SetMotionServer(PlayerNetworkStates.MotionState.Skill);

        // 3. 设置冷却
        _cooldownTimers[index] = skillData.coolDown;

        // 4. 执行核心效果 (积木系统)
        skillData.Cast(gameObject, null, aimPosition);

        // 5. 复位逻辑 (Back To Idle)
        // 注意：这里用简单的延迟复位。理想情况下应该根据 SkillDataSO 里的 duration 字段，或者动画事件。
        // 为了防止协程冲突，先取消之前的复位任务
        CancelInvoke(nameof(ResetToIdle));
        // 假设每个技能都有个 duration，或者统一给个 1.0f，普攻给 0.5f
        // 这里建议在 SkillDataSO 加一个 castDuration 字段
        float recoveryTime = 0.8f; // 默认值
        Invoke(nameof(ResetToIdle), recoveryTime);
    }

    private void ResetToIdle()
    {
        if (_core.IsDead) return;
        _core.SetMotionServer(PlayerNetworkStates.MotionState.Idle);
        _currentSkillIndex.Value = -1;
    }

    private void PerformChaseLogic()
    {
        // 你的追逐逻辑...
    }

    public float GetCooldownRemaining(int v)
    {
        return _cooldownTimers[v];
    }
}
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerNetworkCombat : NetworkBehaviour
{
    [Header("Skills Configuration")]
    [Tooltip("Index 0 = Attack, 1 = Q, 2 = W, 3 = E")]
    [SerializeField] private List<SkillDataSO> _skillSlots;

    #region Network Variables
    private readonly NetworkVariable<bool> _qSkillReady = new NetworkVariable<bool>(true);
    private readonly NetworkVariable<bool> _wSkillReady = new NetworkVariable<bool>(true);
    private readonly NetworkVariable<bool> _eSkillReady = new NetworkVariable<bool>(true);

    // 0 = Attack, no netvar needed usually, but for consistency we track index
    private readonly NetworkVariable<int> _currentSkillIndex = new NetworkVariable<int>(0);

    public NetworkVariable<bool> QSkillActiveVar => _qSkillReady;
    public NetworkVariable<bool> WSkillActiveVar => _wSkillReady;
    public NetworkVariable<bool> ESkillActiveVar => _eSkillReady;

    public event Action<int> OnSkillIndexChanged;
    #endregion

    private float[] _cooldownTimers;
    private PlayerNetworkMovement _movement;

    private void Awake()
    {
        _movement = GetComponent<PlayerNetworkMovement>();
        _cooldownTimers = new float[_skillSlots.Count];
        for (int i = 0; i < _skillSlots.Count; i++)
        {
            if (_skillSlots[i] != null)
                _cooldownTimers[i] = _skillSlots[i].coolDown;
        }
    }

    private void Update()
    {
        if (!IsServer) return;
        ProcessCooldowns();
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
                if (i == 1 && !_qSkillReady.Value) _qSkillReady.Value = true;
                if (i == 2 && !_wSkillReady.Value) _wSkillReady.Value = true;
                if (i == 3 && !_eSkillReady.Value) _eSkillReady.Value = true;
            }
        }
    }

    #region Public API

    public SkillDataSO GetSkillDataByIndex(int index)
    {
        if (index >= 0 && index < _skillSlots.Count) return _skillSlots[index];
        return null;
    }

    public string GetSkillAnimationName(int index)
    {
        var data = GetSkillDataByIndex(index);
        return data != null ? data.skillActiveAnimationName : "Attack";
    }

    /// <summary>
    /// 客户端检查技能是否可用
    /// </summary>
    public bool IsSkillReadyClient(int index)
    {
        // 基础攻击 (Index 0) 默认总是可用，或者可以添加本地计时器。
        // 为了简化，假设普攻总是可用的。
        if (index == 0) return true;

        if (index == 1) return _qSkillReady.Value;
        if (index == 2) return _wSkillReady.Value;
        if (index == 3) return _eSkillReady.Value;

        return false;
    }

    public void RequestCastSkill(int index, Vector3 aimPosition)
    {
        if (IsOwner)
        {
            RequestCastSkillServerRpc(index, aimPosition);
        }
    }

    #endregion

    [ServerRpc]
    private void RequestCastSkillServerRpc(int index, Vector3 aimPosition)
    {
        if (index < 0 || index >= _skillSlots.Count) return;

        // 服务器再次校验CD
        if (_cooldownTimers[index] > 0) return;

        // 1. 停止移动 (解决滑步问题)
        if (_movement != null)
        {
            _movement.ServerStopMove();
            // 2. 强制朝向施法点 (解决朝向问题)
            _movement.ServerLookAt(aimPosition);
        }

        // 设置CD
        _cooldownTimers[index] = _skillSlots[index].coolDown;
        if (index == 1) _qSkillReady.Value = false;
        if (index == 2) _wSkillReady.Value = false;
        if (index == 3) _eSkillReady.Value = false;

        _currentSkillIndex.Value = index;

        ExecuteSkillLogic(index, aimPosition);
    }

    private void ExecuteSkillLogic(int index, Vector3 aimPosition)
    {
        SkillDataSO skillData = _skillSlots[index];
        if (skillData != null)
        {
            Debug.Log($"[Combat] Server executing skill: {skillData.name}");
            skillData.Cast(gameObject, null, aimPosition);
        }
    }
}
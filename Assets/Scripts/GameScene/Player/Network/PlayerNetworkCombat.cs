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
    private readonly NetworkVariable<int> _currentSkillIndex = new NetworkVariable<int>(0); // 只要用于同步给其他客户端播放动画

    public NetworkVariable<bool> QSkillActiveVar => _qSkillReady;
    public NetworkVariable<bool> WSkillActiveVar => _wSkillReady;
    public NetworkVariable<bool> ESkillActiveVar => _eSkillReady;

    // 这是一个事件，用于通知表现层（如果有的话）
    public event Action<int> OnSkillIndexChanged;
    #endregion

    private float[] _cooldownTimers;

    private void Awake()
    {
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

    // [关键修复] 返回正确的动画名称供 StateMachine 使用
    public string GetSkillAnimationName(int index)
    {
        var data = GetSkillDataByIndex(index);
        return data != null ? data.skillActiveAnimationName : "Attack";
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

        // CD 检查
        if (_cooldownTimers[index] > 0) return;

        // 消耗 CD
        _cooldownTimers[index] = _skillSlots[index].coolDown;
        if (index == 1) _qSkillReady.Value = false;
        if (index == 2) _wSkillReady.Value = false;
        if (index == 3) _eSkillReady.Value = false;

        // 记录当前技能 Index，如果需要网络同步动画状态
        _currentSkillIndex.Value = index;

        // 执行技能逻辑
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
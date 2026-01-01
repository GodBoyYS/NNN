using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetworkHealth : NetworkBehaviour, IDamageable
{
    [Header("设置")]
    [SerializeField] private int _maxHealth = 100;
    // 1. 权限必须是 Server。
    // 只有服务器能改，客户端只能看。这是状态同步的铁律。
    private readonly NetworkVariable<int> _currentHealth = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public int CurrentHealth => _currentHealth.Value;
    public NetworkVariable<int> CurrentHealthVar => _currentHealth;
    public int MaxHealth => _maxHealth;
    public bool IsDead => _currentHealth.Value < 0;

    // 持久状态变化（适合UI/持续表现）
    public event Action<int, int> OnHealthChanged;
    // 瞬时事件（适合受击特效/音效，不适合UI初始值）
    public event Action<int, ulong> OnDamaged; // 伤害量，伤害来源
    public event Action<ulong> OnDied;  // 伤害来源网络对象 id
    public event Action OnDiedServer;
    public override void OnNetworkSpawn()
    {
        _currentHealth.OnValueChanged += HandleHealthChanged;
        if (IsServer)
        {
            // 初始化只能由服务器做（保证权威）
            _currentHealth.Value = _maxHealth;
        }
        // 这里推动一次，避免脚本订阅时序导致UI不刷新
        // 即便重复调用，对UI也只是重复赋值，不会产生“补播特效”问题
        _currentHealth.Value = _maxHealth;
        OnHealthChanged?.Invoke(_currentHealth.Value, _maxHealth);
    }
    public override void OnNetworkDespawn()
    {
        _currentHealth.OnValueChanged -= HandleHealthChanged;
        OnHealthChanged = null;
        OnDamaged = null;
        OnDied = null;
        OnDiedServer = null;
        //OnHealthChanged = null;
    }
    private void HandleHealthChanged(int prev, int curr)
    {
        OnHealthChanged?.Invoke(curr, _maxHealth);
    }
    public void RequestTakeDamage(int damage, ulong attackerCliendId = 0)
    {
        ServerTakeDamage(damage, attackerCliendId);
    }
    // ===================
    // serveronly api （唯一合法的扣血入口）
    // ===================
    public void ServerTakeDamage(int damage, ulong attackerCliendId = 0)
    {
        if (!IsServer) return;
        if (damage < 0) return;
        if (IsDead) return;
        int prev = _currentHealth.Value;
        int next = Mathf.Clamp(prev - damage, 0, MaxHealth);
        if (next == prev) return;
        _currentHealth.Value = next;
        // 瞬时事件：让表现层播放特效
        DamagedClientRpc(damage, attackerCliendId);
        ShowDamagePopupClientRpc(damage, transform.position);
        // 死亡瞬时事件（只触发一次）
        if(next <= 0)
        {
            DiedClientRpc(attackerCliendId);
            OnDiedServer?.Invoke();
        }
    }
    public void ServerHeal(int amount)
    {
        if (!IsServer) return;
        if(amount <= 0) return;
        if (IsDead) return;
        int prev = _currentHealth.Value;
        int next = Mathf.Clamp(prev + amount, 0, MaxHealth);
        if(next == prev) return;
        _currentHealth.Value = next;
    }

    [ClientRpc]
    private void DamagedClientRpc(int damage, ulong attackerCliendId)
    {
        OnDamaged?.Invoke(damage, attackerCliendId);
    }
    [ClientRpc]
    private void DiedClientRpc(ulong attackerCliendId)
    {
        OnDied?.Invoke(attackerCliendId);
    }
    [ClientRpc]
    private void ShowDamagePopupClientRpc(int amount, Vector3 targetPos)
    {
        // 客户端收到指令，调用UI管理器
        // 做一个防空判断，防止场景切换时UI没加载报错
        if(DamageTextManager.Instance != null)
        {
            // 为了增加一点打击感，可以给位置加一点随机偏移
            Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 1.5f, 0);
            DamageTextManager.Instance.ShowDamage(amount, targetPos + randomOffset);
        }
    }

    // interface implement
    public void TakeDamage(int amount, ulong attackerId)
    {
        RequestTakeDamage(amount, attackerId);
    }
}
using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerDataContainer : NetworkBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] private int _maxHealth = 100;

    private readonly NetworkVariable<int> _currentHealth = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // UI 显示用的事件
    public event Action<int, int> OnHealthChanged;
    // 受伤特效事件
    public event Action<int, ulong> OnDamaged;
    // 死亡事件 (供 StateMachine 监听)
    public event Action OnDied;

    public int CurrentHealth => _currentHealth.Value;
    public int MaxHealth => _maxHealth;
    public bool IsDead => _currentHealth.Value <= 0;

    public override void OnNetworkSpawn()
    {
        _currentHealth.OnValueChanged += HandleHealthChanged;

        if (IsServer)
        {
            _currentHealth.Value = _maxHealth;
        }

        // 初始化UI
        OnHealthChanged?.Invoke(_currentHealth.Value, _maxHealth);
    }

    public override void OnNetworkDespawn()
    {
        _currentHealth.OnValueChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(int prev, int curr)
    {
        OnHealthChanged?.Invoke(curr, _maxHealth);
        if (curr <= 0 && prev > 0)
        {
            OnDied?.Invoke();
        }
    }

    #region IDamageable Interface & Server Logic

    public void TakeDamage(int amount, ulong attackerId)
    {
        if (!IsServer) return;
        if (IsDead) return;

        int newHealth = Mathf.Clamp(_currentHealth.Value - amount, 0, _maxHealth);
        _currentHealth.Value = newHealth;

        // 触发客户端的表现 (漂字, 闪烁)
        DamagedClientRpc(amount, attackerId);
    }

    public void Heal(int amount)
    {
        if (!IsServer) return;
        if (IsDead) return;

        int newHealth = Mathf.Clamp(_currentHealth.Value + amount, 0, _maxHealth);
        _currentHealth.Value = newHealth;
    }

    [ClientRpc]
    private void DamagedClientRpc(int damage, ulong attackerId)
    {
        OnDamaged?.Invoke(damage, attackerId);

        // 调用之前的特效管理器
        if (DamageTextManager.Instance != null)
        {
            Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 1.5f, 0);
            DamageTextManager.Instance.ShowDamage(damage, transform.position + randomOffset);
        }
        if (HitStopManager.Instance != null)
        {
            HitStopManager.Instance.TriggerHitStop();
        }
        // 闪烁效果可以单独获取组件调用，或者在这里调用
        if (TryGetComponent<DamageFlash>(out var flash))
        {
            flash.TriggerFlash();
        }
    }

    #endregion
}
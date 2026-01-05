using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerDataContainer : NetworkBehaviour, 
    IDamageable,
    IDamageMitigator
{
    [Header("Stats")]
    [SerializeField] private int _maxHealth = 100;

    #region netvar defination
    private readonly NetworkVariable<int> _currentHealth = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public NetworkVariable<int> CurrentHealthVar => _currentHealth;
    public int CurrentHealth => _currentHealth.Value;
    // =============================================
    private readonly NetworkVariable<int> _points = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
        );
    public NetworkVariable<int> PointVar => _points;
    // =============================================
    private NetworkList<FixedString32Bytes> _items = new NetworkList<FixedString32Bytes>(
        new List<FixedString32Bytes>(),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
        );
    public NetworkList<FixedString32Bytes> ItemsVar => _items;
    #endregion

    // UI 显示用的事件
    public event Action<int, int> OnHealthChanged;
    // 受伤特效事件
    public event Action<int, ulong> OnDamaged;
    // 死亡事件 (供 StateMachine 监听)
    public event Action<PlayerDataContainer> OnDied;

    private float _damageReductionPercentage = 0f;
    private Coroutine _reductionCoroutine;

    #region public properties
    public int MaxHealth => _maxHealth;
    public bool IsDead => _currentHealth.Value <= 0;
    #endregion

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
            OnDied?.Invoke(this);
            OnDiedServer();
        }
    }

    #region IDamageable Interface & Server Logic

    public void TakeDamage(int amount, ulong attackerId)
    {
        if (!IsServer) return;
        if (IsDead) return;

        // 计算减伤
        float reducedAmountFloat = amount * (1.0f - _damageReductionPercentage);
        int finalDamage = Mathf.RoundToInt(reducedAmountFloat);

        // 保证最小伤害为1 (除非是无敌或者原伤害就是0)
        if (amount > 0 && finalDamage <= 0 && _damageReductionPercentage < 1.0f) finalDamage = 1;

        int newHealth = Mathf.Clamp(_currentHealth.Value - finalDamage, 0, _maxHealth);
        _currentHealth.Value = newHealth;

        DamagedClientRpc(finalDamage, attackerId);
    }

    public void Heal(int amount)
    {
        if (!IsServer) return;
        if (IsDead) return;

        int newHealth = Mathf.Clamp(_currentHealth.Value + amount, 0, _maxHealth);
        _currentHealth.Value = newHealth;
    }
    public void AddPointsServer(int amount)
    {
        if (!IsServer) return;
        _points.Value += amount;
    }
    public void AddItemServer(string name)
    {
        _items.Add(name);
        //Debug.Log($"添加了{name}，目前总共有{_items.Count}个物品");
        string allItems = "";
        foreach (var item in _items)
        {
            allItems += item;
        }
        Debug.Log(allItems);
    }

    private void OnDiedServer()
    {
        if (!IsServer) return;
        NetworkObject.Despawn();
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

    /// <summary>
    /// 接口实现：应用减伤
    /// </summary>
    public void ApplyDamageReduction(float percentage, float duration)
    {
        if (!IsServer) return; // 只有服务器管理数值

        // 如果已经有减伤在运行，先停止旧的（或者你可以设计为取最大值，这里采用覆盖逻辑）
        if (_reductionCoroutine != null)
        {
            StopCoroutine(_reductionCoroutine);
        }

        _reductionCoroutine = StartCoroutine(DamageReductionRoutine(percentage, duration));
    }

    private System.Collections.IEnumerator DamageReductionRoutine(float percentage, float time)
    {
        _damageReductionPercentage = Mathf.Clamp01(percentage);
        Debug.Log($"[Buff] 减伤开启: {_damageReductionPercentage:P0}, 持续 {time}秒");

        yield return new WaitForSeconds(time);

        _damageReductionPercentage = 0f;
        _reductionCoroutine = null;
        Debug.Log("[Buff] 减伤结束");
    }

    #endregion
}

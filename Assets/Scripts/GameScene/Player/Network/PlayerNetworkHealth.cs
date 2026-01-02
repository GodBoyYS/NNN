using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetworkHealth : NetworkBehaviour, IDamageable
{
    [Header("设置")]
    [SerializeField] private int _maxHealth = 100;
    [Header("Feedback Settings")]
    [SerializeField] private float knockbackForce = 15f; // 默认受击击退力度
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

    // 组件引用
    private PlayerNetworkMovement _movement;
    private DamageFlash _damageFlash;

    // 持久状态变化（适合UI/持续表现）
    public event Action<int, int> OnHealthChanged;
    // 瞬时事件（适合受击特效/音效，不适合UI初始值）
    public event Action<int, ulong> OnDamaged; // 伤害量，伤害来源
    public event Action<ulong> OnDied;  // 伤害来源网络对象 id
    public event Action OnDiedServer;

    private void Awake()
    {
        _movement = GetComponent<PlayerNetworkMovement>();
        _damageFlash = GetComponent<DamageFlash>();
    }

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
    // ================= 核心修改：服务器处理伤害与击退 =================
    public void ServerTakeDamage(int damage, ulong attackerClientId = 0)
    {
        if (!IsServer) return;
        if (damage < 0) return;
        if (IsDead) return;

        int prev = _currentHealth.Value;
        int next = Mathf.Clamp(prev - damage, 0, MaxHealth);

        // 1. 处理数值
        if (next != prev)
        {
            _currentHealth.Value = next;
        }

        // 2. 触发客户端视觉反馈 (顿帧、闪白、飘字)
        DamagedClientRpc(damage, attackerClientId);

        // 3. 处理物理击退 (仅 Server)
        // 获取攻击者位置来计算击退方向
        //if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(attackerClientId, out var attackerObj))
        //{
        //    Vector3 direction = (transform.position - attackerObj.transform.position).normalized;
        //    direction.y = 0; // 保证水平击退

        //    // 如果之前有 PlayerNetworkMovement 脚本，调用它的击退逻辑
        //    if (_movement != null)
        //    {
        //        // 注意：这里使用你在上一轮修改好的 ApplyKnockbackServer
        //        _movement.ApplyKnockbackServer(direction, knockbackForce);
        //    }
        //}

        // 4. 处理死亡
        if (next <= 0)
        {
            DiedClientRpc(attackerClientId);
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
    private void DamagedClientRpc(int damage, ulong attackerClientId)
    {
        OnDamaged?.Invoke(damage, attackerClientId);

        // A. 飘字 (保留原有逻辑)
        if (DamageTextManager.Instance != null)
        {
            Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 1.5f, 0);
            DamageTextManager.Instance.ShowDamage(damage, transform.position + randomOffset);
        }

        // B. 闪白 (新功能)
        if (_damageFlash != null)
        {
            _damageFlash.TriggerFlash();
        }

        // C. 顿帧 (新功能 - 仅本地玩家受击，或者攻击者是本地玩家时触发？)
        // 策略：为了打击感，如果是"我被打"或者"我打人"，都应该顿一下。
        // 但简单起见，只要这个 Rpc 触发，就顿帧，意味着全场任何人被打，所有人屏幕都会微卡一下（类似鬼泣联机）。
        // 如果觉得太乱，可以加判断 if (IsOwner || NetworkManager.Singleton.LocalClientId == attackerClientId)
        if (HitStopManager.Instance != null)
        {
            HitStopManager.Instance.TriggerHitStop(0.05f, 0.0f); // 0.0f 意味着完全静止一瞬间，力度更强
        }
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
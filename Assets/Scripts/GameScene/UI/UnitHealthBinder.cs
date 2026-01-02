// [FILE START: Assets\Scripts\GameScene\UI\UnitHealthBinder.cs]
using Unity.Netcode;
using UnityEngine;

public class UnitHealthBinder : NetworkBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private BufferedHealthBar _healthBar;

    [Header("Unit References")]
    // 依然保留这两个引用，用于自动获取或手动指定
    [SerializeField] private BossController _bossController;
    [SerializeField] private EnemyController _enemyController;

    // 不再使用 Start()
    // Start() 运行时，NetworkVariable 可能还没同步完成

    public override void OnNetworkSpawn()
    {
        // 1. 获取组件 (如果 Inspector 没拖)
        if (_bossController == null) _bossController = GetComponentInParent<BossController>();
        if (_enemyController == null) _enemyController = GetComponentInParent<EnemyController>();

        // 2. 绑定事件并强制初始化 UI
        // 注意：这里需要确保 Controller 公开了 CurrentHealth 和 MaxHealth 的读取权限

        if (_bossController != null)
        {
            _bossController.OnHealthChanged += HandleHealthChanged;

            // 【关键修正】: 主动拉取一次当前值。
            // 因为 OnNetworkSpawn 发生时，NetworkVariable 已经有了最新值。
            // 假设 BossController 暴露了 CurrentHealth 属性（见下文补充）
            HandleHealthChanged(_bossController.CurrentHealth, _bossController.MaxHealth);
        }

        if (_enemyController != null)
        {
            _enemyController.OnHealthChanged += HandleHealthChanged;
            // 同理，初始化 Enemy 血条
            HandleHealthChanged(_enemyController.CurrentHealth, _enemyController.MaxHealth);
        }
    }

    public override void OnNetworkDespawn()
    {
        // 3. 安全清理
        // 当对象被放回对象池或销毁时触发，比 OnDestroy 更适合网络对象
        if (_bossController != null) _bossController.OnHealthChanged -= HandleHealthChanged;
        if (_enemyController != null) _enemyController.OnHealthChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(int current, int max)
    {
        if (_healthBar != null)
        {
            _healthBar.UpdateHealth(current, max);
        }
    }
}
// [FILE END]
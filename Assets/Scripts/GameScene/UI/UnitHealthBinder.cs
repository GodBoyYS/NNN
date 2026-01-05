using Unity.Netcode;
using UnityEngine;

// 【重要修改】不再继承 NetworkBehaviour，而是 MonoBehaviour
// 这样它就不需要 NetworkObject，也就不会报错 Nested NetworkObjects 了
public class UnitHealthBinder : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private BufferedHealthBar _healthBar;

    [Header("Unit References")]
    [SerializeField] private BossController _bossController;
    [SerializeField] private EnemyController _enemyController;

    private void Awake()
    {
        // 尝试自动获取引用
        if (_bossController == null) _bossController = GetComponentInParent<BossController>();
        if (_enemyController == null) _enemyController = GetComponentInParent<EnemyController>();
    }

    // 当对象池取出怪物时（SetActive(true)），这个方法会执行
    private void OnEnable()
    {
        // 1. 强制重置 UI 为满血状态（修复复活时血条残缺/不显示的问题）
        if (_healthBar != null)
        {
            // 假设默认最大血量是 100，或者你可以从 controller 获取
            // 这里我们先让它显示满，稍后绑定事件时会同步真实数据
            _healthBar.ForceReset(100);
        }

        // 2. 绑定事件
        BindEvents();
    }

    // 当对象池回收怪物时（SetActive(false)），这个方法会执行
    private void OnDisable()
    {
        UnbindEvents();
    }

    private void BindEvents()
    {
        if (_bossController != null)
        {
            _bossController.OnHealthChanged += HandleHealthChanged;
            // 初始化同步一次当前真实数值
            HandleHealthChanged(_bossController.CurrentHealth, _bossController.MaxHealth);
        }

        if (_enemyController != null)
        {
            _enemyController.OnHealthChanged += HandleHealthChanged;
            HandleHealthChanged(_enemyController.CurrentHealth, _enemyController.MaxHealth);
        }
    }

    private void UnbindEvents()
    {
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

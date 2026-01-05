// [FILE START: Assets\Scripts\GameScene\Skills\SkillObjects\DamageAuraController.cs]
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 挂载在伤害光环预制体上
/// 职责：跟随主人，并在服务器端持续对周围敌人造成伤害
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class DamageAuraController : NetworkBehaviour
{
    private NetworkObject _owner;
    private int _damage;
    private float _radius;
    private float _interval;
    private float _duration;

    private float _timerDuration = 0f;
    private float _timerInterval = 0f;
    private bool _isInitialized = false;

    // 缓存上一次造成伤害的时间，防止同一帧重复判定（可选，视具体需求而定）
    private Dictionary<ulong, float> _hitHistory = new Dictionary<ulong, float>();

    /// <summary>
    /// 初始化光环 (由技能释放者在生成时调用)
    /// </summary>
    public void Initialize(NetworkObject owner, int damage, float radius, float interval, float duration)
    {
        _owner = owner;
        _damage = damage;
        _radius = radius;
        _interval = interval;
        _duration = duration;
        _isInitialized = true;
    }

    private void Update()
    {
        // 只有服务器负责处理逻辑（伤害、移动、销毁）
        if (!IsServer || !_isInitialized) return;

        // 1. 持续检测并更新位置 (跟随主人)
        if (_owner != null && _owner.IsSpawned)
        {
            transform.position = _owner.transform.position;
        }
        else
        {
            // 如果主人消失了（掉线/死亡销毁），光环也应该销毁
            DespawnAura();
            return;
        }

        // 2. 处理持续时间
        _timerDuration += Time.deltaTime;
        if (_timerDuration >= _duration)
        {
            DespawnAura();
            return;
        }

        // 3. 处理伤害间隔
        _timerInterval += Time.deltaTime;
        if (_timerInterval >= _interval)
        {
            _timerInterval = 0f;
            DealDamage();
        }
    }

    private void DealDamage()
    {
        // 获取范围内所有碰撞体
        Collider[] hits = Physics.OverlapSphere(transform.position, _radius);

        foreach (var hit in hits)
        {
            // 排除主人
            if (hit.gameObject == _owner.gameObject) continue;

            // 排除自己（防止光环有碰撞体打到自己）
            if (hit.gameObject == gameObject) continue;

            if (hit.TryGetComponent<IDamageable>(out var damageable))
            {
                // 造成伤害，attackerId 填主人的ID，这样击杀算主人的
                damageable.TakeDamage(_damage, _owner.NetworkObjectId);
            }
        }
    }

    private void DespawnAura()
    {
        if (NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
    }

    // 可以在编辑器里画出范围，方便调试
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawSphere(transform.position, _radius > 0 ? _radius : 3f);
    }
}
// [FILE END]

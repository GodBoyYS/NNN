using System;
using Unity.Netcode;
using UnityEngine;
/// <summary>
/// 挂载在投射物预制体上，负责飞行、碰撞和销毁
/// </summary>
public class ProjectileController : NetworkBehaviour
{
    private float _speed;
    private float _maxDistance;
    private int _damage;
    private float _radius;
    private ulong _attackerId;
    private GameObject _caster; // 用于避免击中施法者自己

    private Vector3 _startPos;
    private Vector3 _direction;
    private float _traveledDistance;
    private bool _isInitialized = false;

    // 服务器端初始化方法
    public void Initialize(Vector3 direction, float speed, float maxDistance, int damage, float radius, ulong attackerId, GameObject caster)
    {
        _direction = direction;
        _speed = speed;
        _maxDistance = maxDistance;
        _damage = damage;
        _radius = radius;
        _attackerId = attackerId;
        _caster = caster;

        _startPos = transform.position;
        _traveledDistance = 0f;
        _isInitialized = true;
    }

    private void Update()
    {
        // 只有服务器负责移动和伤害判定
        if (!IsServer || !_isInitialized) return;

        ProcessMovement();
    }

    private void ProcessMovement()
    {
        float step = _speed * Time.deltaTime;
        transform.position += _direction * step;
        _traveledDistance += step;

        // 1. 距离检测
        if (_traveledDistance >= _maxDistance)
        {
            DespawnObject();
            return;
        }

        // 2. 碰撞检测
        // 使用 CheckSphere 稍微优化性能，只有探测到东西时才使用 OverlapSphere 获取详情
        if (Physics.CheckSphere(transform.position, _radius, LayerMask.GetMask("Player", "Enemy")))
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, _radius);
            bool hitValidTarget = false;

            foreach (var hit in hits)
            {
                // 忽略施法者自己
                if (hit.gameObject == _caster) continue;
                // 忽略投射物自己
                if (hit.gameObject == gameObject) continue;

                if (hit.TryGetComponent<IDamageable>(out var damageCmp))
                {
                    damageCmp.TakeDamage(_damage, _attackerId);
                    Debug.Log($"[Projectile] Hit {hit.name}, caused {_damage}");
                    hitValidTarget = true;
                }
            }

            // 如果击中了有效的可攻击目标，销毁投射物
            if (hitValidTarget)
            {
                DespawnObject();
            }
        }
    }

    private void DespawnObject()
    {
        if (!IsServer) return;
        // 检查是否有池子在运行
        if (NetworkObjectPool.Instance != null)
        {
            // 通过池子回收
            NetworkObjectPool.Instance.ReturnNetworkObject(NetworkObject);
        }
        else
        {
            // 如果没有池子（比如测试场景忘记放了），还是走老路
            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn();
            }
        }
    }
}
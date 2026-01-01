// 技能积木，生成物体
using System;
using Unity.Netcode;
using UnityEngine;
using System.Collections;

[Serializable]
public class FlyingBullet : SkillEffect
{
    [Header("投掷物参数")]
    [SerializeField] private NetworkObject _bulletPrefab; // 必须是 NetworkObject
    public float speed = 10f;
    public float maxDistance = 20f;
    public int damage = 15;
    public float radius = 1f; // 碰撞半径

    public override void Execute(GameObject caster, GameObject target, Vector3 position)
    {
        // 1. 计算方向
        Vector3 spawnPos = caster.transform.position + Vector3.up * 1.5f; // 稍微抬高一点
        Vector3 direction = (position - caster.transform.position).normalized;
        direction.y = 0; // 水平发射

        // 2. 生成物体
        var bulletInstance = GameObject.Instantiate(_bulletPrefab, spawnPos, Quaternion.LookRotation(direction));
        bulletInstance.Spawn(); // 网络生成

        // 3. 驱动飞行逻辑 (完全在 Effect 代码中控制，无需给 Prefab 挂脚本)
        // 借用 Caster 跑协程
        caster.GetComponent<NetworkBehaviour>().StartCoroutine(
            BulletLogic(caster, bulletInstance.gameObject, direction, spawnPos)
        );
    }

    // 所有的飞行、碰撞、伤害逻辑都封装在这个函数里
    private IEnumerator BulletLogic(GameObject caster, GameObject bullet, Vector3 direction, Vector3 startPos)
    {
        float traveled = 0f;
        ulong attackerId = caster.GetComponent<NetworkObject>().OwnerClientId;

        while (traveled < maxDistance && bullet != null)
        {
            float step = speed * Time.deltaTime;

            // 1. 移动
            bullet.transform.position += direction * step;
            traveled += step;

            // 2. 手动射线/球形检测 (代替 OnTriggerEnter，控制权更强)
            if (Physics.CheckSphere(bullet.transform.position, radius, LayerMask.GetMask("Player", "Enemy")))
            {
                Collider[] hits = Physics.OverlapSphere(bullet.transform.position, radius);
                bool hitValidTarget = false;

                foreach (var hit in hits)
                {
                    if (hit.gameObject == caster) continue; // 忽略自己
                    if (hit.gameObject == bullet) continue;

                    if (hit.TryGetComponent<IDamageable>(out var damageCmp))
                    {
                        damageCmp.TakeDamage(damage, attackerId);
                        //health.RequestTakeDamage(damage, attackerId);
                        hitValidTarget = true;
                    }
                }

                if (hitValidTarget)
                {
                    // 撞到了就销毁，跳出循环
                    break;
                }
            }

            yield return null; // 等待下一帧
        }

        // 销毁子弹
        if (bullet != null && bullet.GetComponent<NetworkObject>().IsSpawned)
        {
            bullet.GetComponent<NetworkObject>().Despawn();
        }
    }
}
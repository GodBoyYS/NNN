// 技能积木：范围伤害
using System;
using Unity.Netcode;
using UnityEngine;
using System.Collections;

[Serializable]
public class AreaEffect : SkillEffect
{
    [Header("AOE 设置")]
    public float radius = 3f;
    public float delay = 1.0f;
    public int damage = 10;
    public GameObject vfxPrefab; // 比如一个地上的红圈特效(NetworkObject)

    public override void Execute(GameObject caster, GameObject target, Vector3 position)
    {
        // 1. 在施法位置生成一个特效 (纯表现，不需要网络逻辑，或者 Spawn 一个不带逻辑的 NetworkObject)
        if (vfxPrefab != null)
        {
            var vfx = GameObject.Instantiate(vfxPrefab, position, Quaternion.identity);
            vfx.GetComponent<NetworkObject>().Spawn();
            // 设置几秒后自动销毁特效
            GameObject.Destroy(vfx, delay + 0.5f);
        }

        // 2. 借用 Caster 的 Mono 开启协程，处理延迟逻辑
        // 注意：这里利用了闭包，协程可以直接访问 execute 传入的参数
        caster.GetComponent<NetworkBehaviour>().StartCoroutine(ExplodeRoutine(caster, position));
    }

    private IEnumerator ExplodeRoutine(GameObject caster, Vector3 centerPos)
    {
        // 等待延迟
        yield return new WaitForSeconds(delay);

        // 范围检测
        Collider[] hits = Physics.OverlapSphere(centerPos, radius);
        foreach (var hit in hits)
        {
            // 排除自己
            if (hit.gameObject == caster) continue;

            // 造成伤害
            if (hit.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(damage, caster.GetComponent<NetworkObject>().NetworkObjectId);
                //// 获取 attackerId
                //ulong attackerId = caster.GetComponent<NetworkObject>().OwnerClientId;
                //health.RequestTakeDamage(damage, attackerId);
                Debug.Log($"[Effect] AOE 炸到了 {hit.name}");
            }
        }
    }
}

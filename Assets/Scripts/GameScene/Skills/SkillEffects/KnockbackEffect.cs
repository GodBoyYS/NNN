using System;
using UnityEngine;
using System.Collections;

[Serializable]
public class KnockbackEffect : SkillEffect
{
    public float radius = 5f;
    public float force = 10f;
    public float stunTime = 0.5f;

    public override void Execute(GameObject caster, GameObject target, Vector3 position)
    {
        // 寻找范围内的倒霉蛋
        Collider[] hits = Physics.OverlapSphere(caster.transform.position, radius);
        foreach (var hit in hits)
        {
            if (hit.gameObject == caster) continue;
            if (hit.TryGetComponent<IKnockBackable>(out var knockCmpnt))
            {
                Vector3 dir = (hit.transform.position - caster.transform.position).normalized;
                dir.y = 0.5f; // 给更高的 Y 值，确保能跳起来

                knockCmpnt.ApplyKnockbackServer(dir, force);
                Debug.Log($"击飞了 {hit.name}");
            }
        }
    }
}
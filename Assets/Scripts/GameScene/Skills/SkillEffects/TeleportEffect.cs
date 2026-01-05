using System;
using UnityEngine;

[Serializable]
public class TeleportEffect : SkillEffect
{
    [Header("位移配置")] 
    public float distance = 5f; 
    public bool isDash = true; // 此参数可用于扩展区分瞬移还是冲锋，目前均作为瞬移处理或需要movement支持dash public float duration = 0.2f;

    public override void Execute(GameObject caster, GameObject target, Vector3 position)
    {
        // 尝试获取 ITeleportable 接口
        if (!caster.TryGetComponent<ITeleportable>(out var teleportable))
        {
            Debug.LogWarning($"[Effect] {caster.name} does not implement ITeleportable!");
            return;
        }

        Vector3 direction = (position - caster.transform.position).normalized;
        direction.y = 0;

        // 如果点击的是脚下，默认向前
        if (direction == Vector3.zero) direction = caster.transform.forward;

        Vector3 targetPos = caster.transform.position + direction * distance;

        // 使用接口方法进行瞬移
        teleportable.TeleportServer(targetPos);

        Debug.Log($"[Effect] Teleport: {caster.name} to {targetPos}");
    }
}

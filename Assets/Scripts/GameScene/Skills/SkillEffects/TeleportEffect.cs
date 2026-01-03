// 积木B，瞬移
using System;
using UnityEngine;

[Serializable]
public class TeleportEffect : SkillEffect
{
    [Header("位移设置")]
    public float distance = 5f;
    public bool isDash = true; // true=冲刺(带碰撞检测), false=瞬移(穿墙)
    public float duration = 0.2f; // 如果是冲刺，持续多久

    public override void Execute(GameObject caster, GameObject target, Vector3 position)
    {
        if (!caster.TryGetComponent<PlayerNetworkMovement>(out var movement)) return;

        // 1. 计算方向
        // position 是鼠标点击的世界坐标
        Vector3 direction = (position - caster.transform.position).normalized;

        // 忽略Y轴差异，防止冲进地里
        direction.y = 0;
        direction.Normalize();

        // 2. 目标点计算
        Vector3 targetPos = caster.transform.position + direction * distance;

        // 3. 执行
        // 这里直接调用我们在 Movement 中写好的 ServerForceDash 
        // 或者是瞬移逻辑
        Debug.Log($"[Effect] 位移: {caster.name} 向 {direction} 移动 {distance}米");

        //movement.ServerForceDash(direction, distance, duration);
    }
}

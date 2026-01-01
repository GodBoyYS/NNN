using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public abstract class SkillEffect
{
    [Header("基础设置")]
    public string effectName;
    // 核心方法：所有的效果积木都必须实现这个逻辑
    // caster：施法者，target：目标（可选），postion（目标位置）
    public abstract void Execute(GameObject caster, GameObject target, Vector3 position);
}

[Serializable]
public class DelayEffect : SkillEffect
{
    public float duration = 1.0f;

    public override void Execute(GameObject caster, GameObject target, Vector3 position)
    {
        // 这里什么都不用做，逻辑在 SkillDataSO 的循环里被处理了
        // 只是作为一个数据容器告诉循环要停多久
    }
}




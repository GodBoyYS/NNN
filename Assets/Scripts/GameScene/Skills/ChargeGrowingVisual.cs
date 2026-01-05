using UnityEngine;
using System.Collections;

public class ChargeGrowingVisual : MonoBehaviour
{
    [Header("视觉参数")]
    [Tooltip("生长最终的目标大小 (Local Scale)")]
    [SerializeField] private float _targetScale = 20.0f; // 对应直径 (半径10 * 2)

    [Tooltip("生长曲线，让变大过程更自然")]
    [SerializeField] private AnimationCurve _growthCurve = AnimationCurve.Linear(0, 0, 1, 1);

    /// <summary>
    /// API: 设置持续时间并开始播放
    /// </summary>
    /// <param name="duration">蓄力时间</param>
    public void SetDuration(float duration)
    {
        // 初始设为0
        transform.localScale = Vector3.zero;
        StartCoroutine(GrowthRoutine(duration));
    }

    private IEnumerator GrowthRoutine(float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / duration);

            // 使用曲线计算当前比例
            float curveValue = _growthCurve.Evaluate(progress);

            transform.localScale = Vector3.one * (_targetScale * curveValue);

            yield return null;
        }

        // 确保最终大小一致
        transform.localScale = Vector3.one * _targetScale;

        // 蓄力结束，特效自我销毁 (或者播放爆炸粒子后销毁)
        Destroy(gameObject);
    }
}

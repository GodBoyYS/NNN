using System.Collections;
using UnityEngine;

public class HitStopManager : MonoBehaviour
{
    public static HitStopManager Instance { get; private set; }

    private bool _isWaiting = false;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// 触发顿帧
    /// </summary>
    /// <param name="duration">持续时间 (秒)</param>
    /// <param name="timeScale">时间缩放比例 (0.0 ~ 1.0)</param>
    public void TriggerHitStop(float duration = 0.05f, float timeScale = 0.1f)
    {
        if (_isWaiting) return; // 如果正在顿帧，忽略新的请求（防止鬼畜）
        StartCoroutine(HitStopRoutine(duration, timeScale));
    }

    private IEnumerator HitStopRoutine(float duration, float targetScale)
    {
        _isWaiting = true;

        // 记录原始 TimeScale (通常是 1)
        float original = Time.timeScale;

        // 瞬间减速
        Time.timeScale = targetScale;

        // 等待真实时间 (不受 timeScale 影响)
        yield return new WaitForSecondsRealtime(duration);

        // 恢复
        Time.timeScale = original;
        _isWaiting = false;
    }
}
using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class NukeZoneVisual : NetworkBehaviour
{
    // 目标缩放大小
    private float _targetScale;
    // 膨胀持续时间
    private float _duration;
    // 是否开始运行
    private bool _isRunning = false;
    private float _timer = 0f;

    // 客户端调用：开始播放膨胀动画
    [ClientRpc]
    public void StartExpansionClientRpc(float targetRadius, float duration)
    {
        // 直径 = 半径 * 2
        _targetScale = targetRadius * 2.0f;
        _duration = duration;
        _timer = 0f;
        _isRunning = true;

        // 初始大小设为接近0
        transform.localScale = Vector3.zero;
    }

    private void Update()
    {
        if (!_isRunning) return;

        _timer += Time.deltaTime;
        float progress = _timer / _duration;

        if (progress >= 1.0f)
        {
            transform.localScale = Vector3.one * _targetScale;
            _isRunning = false;
        }
        else
        {
            // 使用平滑插值，甚至可以用 AnimationCurve 让它先慢后快
            transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * _targetScale, progress);
        }
    }
}

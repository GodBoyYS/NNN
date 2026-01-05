using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(LineRenderer))]
public class LaserVisual : NetworkBehaviour
{
    private LineRenderer _line;
    private float _growthDuration;
    private float _startLen;
    private float _maxLen;
    private bool _isGrowing = false;
    private float _timer = 0f;

    private void Awake()
    {
        _line = GetComponent<LineRenderer>();
        // 确保使用本地坐标，这样我们移动物体，线也会跟着动；
        // 如果想让激光固定在世界坐标不随BOSS转动，可以在生成后不设Parent
        _line.useWorldSpace = false;
    }

    [ClientRpc]
    public void InitializeLaserClientRpc(float startLen, float maxLen, float growthDuration, float width)
    {
        _startLen = startLen;
        _maxLen = maxLen;
        _growthDuration = growthDuration;

        _line.startWidth = width;
        _line.endWidth = width;

        // 初始化长度
        _line.SetPosition(0, Vector3.zero);
        _line.SetPosition(1, Vector3.forward * startLen);

        _timer = 0f;
        _isGrowing = true;
    }

    private void Update()
    {
        if (!_isGrowing) return;

        _timer += Time.deltaTime;
        float progress = Mathf.Clamp01(_timer / _growthDuration);

        // 计算当前长度
        float currentLen = Mathf.Lerp(_startLen, _maxLen, progress);

        // 更新 LineRenderer 终点 (在本地 Z 轴延伸)
        _line.SetPosition(1, Vector3.forward * currentLen);

        if (progress >= 1.0f)
        {
            _isGrowing = false;
        }
    }
}

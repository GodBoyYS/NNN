// [FILE START: Assets\Scripts\GameScene\UI\BufferedHealthBar.cs]
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BufferedHealthBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider _mainSlider;   // 前景血条（红/绿）
    [SerializeField] private Slider _bufferSlider; // 背景缓冲条（黄/白）

    [Header("Settings")]
    [SerializeField] private float _bufferDropSpeed = 0.5f; // 黄条减少的速度
    [SerializeField] private float _bufferDelay = 0.5f;     // 扣血后黄条停顿多久才开始掉

    private float _targetFillAmount;
    private float _delayTimer;

    private void Awake()
    {
        // 初始化
        if (_mainSlider == null) _mainSlider = GetComponent<Slider>();
    }

    /// <summary>
    /// 供外部调用的更新方法
    /// </summary>
    /// <param name="current">当前血量</param>
    /// <param name="max">最大血量</param>
    public void UpdateHealth(int current, int max)
    {
        float fillAmount = (float)current / max;
        _targetFillAmount = fillAmount;

        // 1. 设置主血条（瞬间变化）
        _mainSlider.value = _targetFillAmount;

        // 2. 如果是加血，黄条直接跟上；如果是扣血，重置延迟计时器
        if (_bufferSlider != null)
        {
            if (_mainSlider.value > _bufferSlider.value)
            {
                _bufferSlider.value = _mainSlider.value; // 加血瞬间同步
            }
            else
            {
                _delayTimer = _bufferDelay; // 扣血，重置延迟
            }
        }
    }

    private void Update()
    {
        if (_bufferSlider == null) return;

        // 如果缓冲条比主条长，说明需要减少
        if (_bufferSlider.value > _mainSlider.value)
        {
            // 延迟处理
            if (_delayTimer > 0)
            {
                _delayTimer -= Time.deltaTime;
                return;
            }

            // 平滑减少
            _bufferSlider.value -= _bufferDropSpeed * Time.deltaTime;
        }
    }
}
// [FILE END]
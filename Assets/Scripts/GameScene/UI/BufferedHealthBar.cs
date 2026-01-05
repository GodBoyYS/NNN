using UnityEngine;
using UnityEngine.UI;

public class BufferedHealthBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider _mainSlider;   // 红色/绿色条
    [SerializeField] private Slider _bufferSlider; // 黄色缓冲条
    [SerializeField] private CanvasGroup _canvasGroup; // 【新增】用于控制整体显隐

    [Header("Settings")]
    [SerializeField] private float _bufferDropSpeed = 0.5f;
    [SerializeField] private float _bufferDelay = 0.5f;

    private float _delayTimer;

    private void Awake()
    {
        if (_mainSlider == null) _mainSlider = GetComponent<Slider>();
        if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
    }

    /// <summary>
    /// 普通更新：带缓冲动画
    /// </summary>
    public void UpdateHealth(int current, int max)
    {
        // 1. 如果血量<=0，隐藏血条（解决血条不消失bug）
        if (current <= 0)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);

        float fillAmount = (float)current / max;

        // 设置主血条
        _mainSlider.value = fillAmount;

        // 处理缓冲条逻辑
        if (_bufferSlider != null)
        {
            if (_mainSlider.value > _bufferSlider.value)
            {
                // 加血：黄条瞬间跟上
                _bufferSlider.value = _mainSlider.value;
            }
            else
            {
                // 扣血：延迟
                _delayTimer = _bufferDelay;
            }
        }
    }

    /// <summary>
    /// 【新增】强制重置：用于对象池复活时，瞬间回满，无动画
    /// </summary>
    public void ForceReset(int max)
    {
        SetVisible(true);
        _mainSlider.value = 1.0f;
        if (_bufferSlider != null) _bufferSlider.value = 1.0f;
        _delayTimer = 0f;
    }

    private void Update()
    {
        if (_bufferSlider == null) return;

        if (_bufferSlider.value > _mainSlider.value)
        {
            if (_delayTimer > 0)
            {
                _delayTimer -= Time.deltaTime;
                return;
            }
            _bufferSlider.value -= _bufferDropSpeed * Time.deltaTime;
        }
    }

    private void SetVisible(bool isVisible)
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = isVisible ? 1 : 0;
        }
        else
        {
            // 如果没有CanvasGroup，就暴力的控制子物体开关，但建议用CanvasGroup
            foreach (Transform child in transform) child.gameObject.SetActive(isVisible);
        }
    }
}

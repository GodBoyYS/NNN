using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 挂载在预警 Prefab (Canvas) 上，负责控制预警圈的动画
/// </summary>
public class SkillIndicator : MonoBehaviour
{
    [Header("UI 组件引用")]
    [SerializeField] private Image _fillImage; // 必须设置 Image Type 为 "Filled"

    /// <summary>
    /// 初始化并开始播放预警动画
    /// </summary>
    /// <param name="duration">预警持续时间（秒）</param>
    /// <param name="diameter">预警圈直径（米）</param>
    public void Initialize(float duration, float diameter)
    {
        // 设置 Canvas 的大小，匹配技能范围
        // 对于 World Space Canvas，Scale 1 = 1米，所以直接设置 LocalScale 即可
        transform.localScale = Vector3.one * diameter;

        StartCoroutine(PlayFillAnimationRoutine(duration));
    }

    private IEnumerator PlayFillAnimationRoutine(float duration)
    {
        float timer = 0f;
        _fillImage.fillAmount = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            // 核心逻辑：根据时间比例更新填充度 (0 -> 1)
            _fillImage.fillAmount = timer / duration;

            yield return null;
        }

        // 确保填满
        _fillImage.fillAmount = 1f;

        // 稍微延迟销毁，防止视觉上消失得太突兀
        yield return new WaitForSeconds(0.1f);

        Destroy(gameObject);
    }
}

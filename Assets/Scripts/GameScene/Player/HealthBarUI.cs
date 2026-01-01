using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    // 如果是全局唯一的血条（比如屏幕左上角），做个单例最方便查找
    public static HealthBarUI Instance { get; private set; }

    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image QSkill;

    private void Awake()
    {
        Instance = this;
    }

    public void UpdateViewHealth(int current, int max)
    {
        if (healthSlider == null) return;
        healthSlider.value = (float)current / max;
    }
    public void UpdateViewQskill(bool activeBefore, bool activeCurrent)
    {
        if(QSkill == null) return;
        // 根据是否可用设置颜色灰度值
        QSkill.color = activeCurrent ? Color.yellow : Color.red;
    }
}
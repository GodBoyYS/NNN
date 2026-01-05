using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MouseHoverHighlight : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private float emissionIntensity = 2.0f;

    private Renderer[] _renderers;
    private MaterialPropertyBlock _propBlock;
    private bool _isHovered = false;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        _propBlock = new MaterialPropertyBlock();
    }

    private void OnMouseEnter()
    {
        if (_isHovered) return;
        _isHovered = true;
        SetHighlight(true);
    }

    private void OnMouseExit()
    {
        if (!_isHovered) return;
        _isHovered = false;
        SetHighlight(false);
    }

    private void SetHighlight(bool active)
    {
        foreach (var r in _renderers)
        {
            // 获取当前的 PropertyBlock，以防被 DamageFlash 等其他组件修改过
            r.GetPropertyBlock(_propBlock);

            if (active)
            {
                // 开启高亮：设置 Emission Color
                // 注意：你的 Shader 需要支持 _EmissionColor 属性，且 Enable Emission
                _propBlock.SetColor("_EmissionColor", highlightColor * emissionIntensity);
            }
            else
            {
                // 关闭高亮：恢复黑色（无自发光）
                _propBlock.SetColor("_EmissionColor", Color.black);
            }

            r.SetPropertyBlock(_propBlock);
        }
    }
}

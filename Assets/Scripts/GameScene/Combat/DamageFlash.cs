using System.Collections;
using UnityEngine;

public class DamageFlash : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float duration = 0.1f;

    private SkinnedMeshRenderer[] _renderers;
    private MaterialPropertyBlock _propBlock;
    private Coroutine _flashRoutine;

    private void Awake()
    {
        // 获取所有的 Mesh Renderer (适用于角色模型)
        _renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        _propBlock = new MaterialPropertyBlock();
    }

    public void TriggerFlash()
    {
        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        // 1. 设置为闪光颜色 (利用 Emission 自发光最容易实现“全白”)
        foreach (var r in _renderers)
        {
            r.GetPropertyBlock(_propBlock);
            // 如果是 URP 或 Standard，通常可以通过 _EmissionColor 让它变亮变白
            // 如果你的 Shader 没有 Emission，可以用 _BaseColor 或 _Color，但效果可能只是变色而不是变亮
            _propBlock.SetColor("_EmissionColor", flashColor * 5f); // *5 为了更亮
            // 备选：_propBlock.SetColor("_BaseColor", flashColor); 
            r.SetPropertyBlock(_propBlock);
        }

        yield return new WaitForSeconds(duration);

        // 2. 恢复 (清除 PropertyBlock 即可，它会回退到材质默认值)
        foreach (var r in _renderers)
        {
            r.GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_EmissionColor", Color.black); // 黑色 Emission = 不发光
            r.SetPropertyBlock(_propBlock);
        }

        _flashRoutine = null;
    }
}

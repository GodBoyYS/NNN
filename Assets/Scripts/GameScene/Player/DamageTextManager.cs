using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.Pool;

public class DamageTextManager : MonoBehaviour
{
    public static DamageTextManager Instance {  get; private set; }
    [Header("配置")]
    [SerializeField] private DamageText textPrefab;
    [SerializeField] private Canvas gameCanvas; // 确保是screen space - overlay

    // 2.define the pool
    private ObjectPool<DamageText> _pool;
    private Camera _camera;

    private void Awake()
    {
        Instance = this;
        _camera = Camera.main;
        // 3.init the pool(too many parameters but clear logic)
        _pool = new ObjectPool<DamageText>(
            createFunc: CreateText,     // A.when nothing in the pool, how to create new instance?
            actionOnGet: OnGetText,     // B.what to do when taking out from the pool? --> display
            actionOnRelease: OnReleaseText,     // C.what to do when release to the pool? --> hide
            actionOnDestroy: OnDestroyText,     // D.how to handle the residual objects when destroying the object?
            defaultCapacity:20,
            maxSize:100
            );
    }
    // -- 4 lifetime methods for the pool --
    // A.when nothing in the pool, how to create new instance?
    private DamageText CreateText()
    {
        var instance = Instantiate(textPrefab, transform);
        // give the key to the instance
        instance.SetupPool(_pool);
        return instance;
    }
    // B.what to do when taking out from the pool? --> display
    private void OnGetText(DamageText text)
    {
        text.gameObject.SetActive(true);
        text.transform.localScale = Vector3.one; // 重置缩放，防止上次动画残留
        // here can make some animations
    }
    // C.what to do when release to the pool? --> hide
    private void OnReleaseText(DamageText text)
    {
        text.gameObject.SetActive(false);
    }
    // D.how to handle the residual objects when destroying the pool?
    private void OnDestroyText(DamageText text)
    {
        Destroy(text.gameObject);
    }
    
    // --- public API ---
    public void ShowDamage(int amount, Vector3 worldPos)
    {
        if (textPrefab == null) return;
        // 4. use Get instead of instantiate
        var instance = _pool.Get();
        // 2.计算屏幕位置（世界坐标->屏幕坐标）
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        instance.transform.position = screenPos;
        // 设置数值
        instance.Setup(amount);
    }

}

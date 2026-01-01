using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Pool; // 1.using namespace pool

public class DamageText : MonoBehaviour
{
    [SerializeField] private TMP_Text damageLable;
    // 初始化方法，由manager调用
    // hold the reference of the pool
    private IObjectPool<DamageText> _pool;
    // 2. init method: when manager create a text, send a "key" to the text for backing into the exact pool
    public void SetupPool(IObjectPool<DamageText> pool)
    {
        _pool = pool;
    }
    public void Setup(int damageAmount)
    {
        damageLable.text = damageAmount.ToString();
        // 如果不用 animator ，可以用简单的协程做淡入淡出
        //Destroy(gameObject, 1.0f);
        StartCoroutine(ReturnToPoolAfterTime(3.0f));
    }
    private IEnumerator ReturnToPoolAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        // 3.key point: not destroy anymore, but "release" to pool
        // this step will automatically invoke manager's OnReturnedToPool
        if(_pool != null)
        {
            _pool.Release(this);
        }
        else
        {
            // defence code:if forget to setup pool, then destroy object directly, avoiding error
            Destroy(gameObject);
        }
    }
}

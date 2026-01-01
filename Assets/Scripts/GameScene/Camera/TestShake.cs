using UnityEngine;
using Unity.Cinemachine;

public class TestShake : MonoBehaviour
{
    // 把你的虚拟相机(带有Listener的)拖进去
    public CinemachineCamera targetCamera;
    // 把你的 Noise Settings 文件拖进去
    public NoiseSettings noiseProfile;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("强制测试震动！");
            // 既然是 CM 3.0，我们直接生成一个脉冲
            if (targetCamera != null && noiseProfile != null)
            {
                //// 发送一个全局脉冲，看相机有没有反应
                //CinemachineImpulseManager.Instance.IgnoreTimeScale = true;
                //CinemachineImpulseManager.Instance.EnvelopeShake(
                //    noiseProfile,
                //    1.0f, // 力度
                //    0.2f, // 攻击时间
                //    0.5f, // 维持时间
                //    0.5f, // 衰减时间
                //    Vector3.up,
                //    Quaternion.identity
                //);
            }
        }
    }
}
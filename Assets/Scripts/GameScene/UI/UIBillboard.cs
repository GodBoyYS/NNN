// [FILE START: Assets\Scripts\GameScene\UI\UIBillboard.cs]
using UnityEngine;

public class UIBillboard : MonoBehaviour
{
    private Camera _mainCamera;

    private void Start()
    {
        _mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_mainCamera == null) return;

        // 让 UI 旋转以面朝摄像机
        // 使用 LookAt 的反向逻辑或者 forward 对齐逻辑
        transform.LookAt(transform.position + _mainCamera.transform.rotation * Vector3.forward, _mainCamera.transform.rotation * Vector3.up);
    }
}
// [FILE END]
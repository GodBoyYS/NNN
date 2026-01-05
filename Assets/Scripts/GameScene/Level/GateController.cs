using Unity.Netcode;
using UnityEngine;

public class GateController : NetworkBehaviour
{
    [SerializeField] private GameObject _visualModel;
    [SerializeField] private Collider _blockCollider;

    public override void OnNetworkSpawn()
    {
        // 默认状态：如果是新的区域，门是关着的
        SetGateState(true);
    }

    // Server 决定门是否关闭
    public void SetLocked(bool isLocked)
    {
        if (!IsServer) return;
        SetGateClientRpc(isLocked);
    }

    [ClientRpc]
    private void SetGateClientRpc(bool isLocked)
    {
        if (isLocked) return;
        _visualModel.GetComponent<NetworkObject>().Despawn();
        //if (_visualModel) _visualModel.SetActive(isLocked);
        //if (_blockCollider) _blockCollider.enabled = isLocked;

        //if (!isLocked)
        //{
        //    // 这里可以加个音效：门打开的声音
        //    Debug.Log("Gate Opened!");
        //}
    }

    private void SetGateState(bool isLocked)
    {
        _visualModel.SetActive(isLocked);
        //if (_visualModel) _visualModel.SetActive(isLocked);
        //if (_blockCollider) _blockCollider.enabled = isLocked;
    }
}

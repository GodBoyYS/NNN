using UnityEngine;

public class InteractCube : MonoBehaviour, IInteractable
{
    private bool _stateBig = false;
    public string InteractionPrompt => "PickUp"; // prompt？就像我作为用户咨询你这个AI一样，prompt不应该是由用户决定吗？
    // 为什么要放在接口类里面？
    // 为什么不使用 enum 来定义好能够交互的的情况？

    public void Interact(GameObject source)
    {
        if (_stateBig)
        {
            transform.localScale = Vector3.one * 2.0f;
            _stateBig = false;
        }
        else
        {
            transform.localScale = Vector3.one;
            _stateBig = true;
        }
    }
}

using UnityEngine;

public class PlayerNetworkStates
{
    // 放到任意脚本目录即可（建议 /Scripts/Networking/）
    public enum MotionState : byte
    {
        Idle = 0,
        Moving = 1,
        Skill = 2,
    }

    public enum LifeState : byte
    {
        Alive = 0,
        Dead = 1
    }
}

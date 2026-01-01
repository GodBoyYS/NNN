using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CharacterDatabase", menuName = "Game/Character Database")]
public class CharacterDatabaseSO : ScriptableObject
{
    // 这里存放所有的角色预制体
    public List<NetworkObject> characterPrefabs;

    // 辅助方法：确保索引不越界
    public NetworkObject GetPrefabById(int id)
    {
        if (id >= 0 && id < characterPrefabs.Count)
        {
            return characterPrefabs[id];
        }
        Debug.Log("获取默认角色");
        return characterPrefabs[0]; // 默认返回第一个，防止报错
    }
}
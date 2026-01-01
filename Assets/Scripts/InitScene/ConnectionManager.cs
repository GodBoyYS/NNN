using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConnectionManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private CharacterDatabaseSO characterDatabase;
    [SerializeField] private string gameSceneName = "Game";

    private Dictionary<ulong, int> _clientSelectionData = new Dictionary<ulong, int>();

    private void Start()
    {
        DontDestroyOnLoad(this);
    }

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
        {
            SubscribeEvents();
        }
    }

    private void SubscribeEvents()
    {
        // 先移除，防重复
        UnsubscribeEvents();

        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;

        // 【关键修改 1】这里不要订阅 SceneManager！
        // 因为此时 Server 还没启动，SceneManager 是空的，订阅必报错。
        // 我们把它移到 StartHost 成功之后去订阅。

        Debug.Log("ConnectionManager: 基础事件订阅成功！");
    }

    private void UnsubscribeEvents()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;

        // 【关键修改 2】安全移除订阅
        // 只有当 SceneManager 存在时才去取消订阅
        if (NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadComplete;
        }
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    // ================= 1. 启动入口 =================

    public void StartHostWithCharacter(int characterIndex)
    {
        // 1. 准备数据
        SubscribeEvents();
        var payload = Encoding.ASCII.GetBytes(characterIndex.ToString());
        NetworkManager.Singleton.NetworkConfig.ConnectionData = payload;
        NetworkManager.Singleton.NetworkConfig.PlayerPrefab = null;

        Debug.Log($"1. [Host] 准备启动，选择角色: {characterIndex}");

        // 2. 启动 Host
        if (NetworkManager.Singleton.StartHost())
        {
            Debug.Log("2. [Host] 启动成功！");

            // 【关键修改 3】在这里订阅场景事件！
            // 此时 Host 已经启动，SceneManager 已经创建出来了，不会报错。
            if (NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadComplete;
                Debug.Log("   -> 场景事件订阅成功");

                // 3. 加载场景
                NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
            }
            else
            {
                Debug.LogError("!!! 致命错误：SceneManager 依然为空！请检查 Inspector 中是否勾选了 'Enable Scene Management' !!!");
            }
        }
    }

    public void StartClientWithCharacter(int characterIndex)
    {
        SubscribeEvents();
        var payload = Encoding.ASCII.GetBytes(characterIndex.ToString());
        NetworkManager.Singleton.NetworkConfig.ConnectionData = payload;
        NetworkManager.Singleton.NetworkConfig.PlayerPrefab = null;

        Debug.Log($"1. [Client] 准备连接，选择角色: {characterIndex}");
        NetworkManager.Singleton.StartClient();

        // 客户端不需要订阅 SceneManager，因为生成角色的权力在服务器手里
    }

    // ================= 2. 审批 (记账) =================
    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        int charId = 0;
        if (request.Payload != null)
        {
            int.TryParse(Encoding.ASCII.GetString(request.Payload), out charId);
        }

        if (_clientSelectionData.ContainsKey(request.ClientNetworkId))
            _clientSelectionData[request.ClientNetworkId] = charId;
        else
            _clientSelectionData.Add(request.ClientNetworkId, charId);

        Debug.Log($"3. [Server] 审批通过: 客户端ID {request.ClientNetworkId} 选择了角色 {charId}");

        response.Approved = true;
        response.CreatePlayerObject = false;
    }

    // ================= 3. 连接成功 =================
    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        Debug.Log($"4. [Server] 客户端 {clientId} 连接成功。当前场景: {SceneManager.GetActiveScene().name}");

        // 如果中途加入，且已经在游戏场景，直接生成
        if (SceneManager.GetActiveScene().name == gameSceneName)
        {
            Debug.Log($"   -> 已经在游戏场景，直接生成角色给 {clientId}");
            SpawnPlayer(clientId);
        }
        else
        {
            Debug.Log($"   -> 还在 {SceneManager.GetActiveScene().name}，暂不生成，等待场景加载...");
        }
    }

    // ================= 4. 场景加载完毕 =================
    private void OnSceneLoadComplete(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        if (!sceneName.Contains(gameSceneName)) return;

        Debug.Log($"5. [Server] 场景 {sceneName} 加载完毕！开始检查并生成角色...");

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject == null)
            {
                Debug.Log($"   -> 为玩家 {clientId} 补发角色");
                SpawnPlayer(clientId);
            }
        }
    }

    // ================= 5. 生成逻辑 =================
    private void SpawnPlayer(ulong clientId)
    {
        int charId = _clientSelectionData.ContainsKey(clientId) ? _clientSelectionData[clientId] : 0;
        NetworkObject prefab = characterDatabase.GetPrefabById(charId);

        if (prefab == null) return;

        Vector3 pos = new Vector3(clientId * 2, 1, 0);
        GameObject instance = Instantiate(prefab.gameObject, pos, Quaternion.identity);
        NetworkObject netObj = instance.GetComponent<NetworkObject>();

        netObj.SpawnAsPlayerObject(clientId, true);

        Debug.Log($"6. [Server] 成功生成角色 {charId} 给玩家 {clientId}");
    }

    private void OnClientDisconnect(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer)
            _clientSelectionData.Remove(clientId);
    }
}
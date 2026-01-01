// GameLifecycleManager.cs
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLifecycleManager : NetworkBehaviour
{
    [SerializeField] private BossController bossInstance;

    private List<PlayerNetworkCore> activePlayers = new List<PlayerNetworkCore>();
    private bool isGameEnded = false;

    public static GameLifecycleManager Instance { get; private set; }

    public void Awake()
    {
        // 确保单例即使在切换场景后也能更新引用
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // 1. 监听新连接的客户端
        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect; // 建议监听断线

        // 2. 【关键修复】处理已经存在的玩家 (比如 Host 自己，或者 Manager 生成较晚的情况)
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                var playerCore = client.PlayerObject.GetComponent<PlayerNetworkCore>();
                RegisterPlayer(playerCore);
            }
        }

        // 3. Boss 订阅
        if (bossInstance != null)
        {
            bossInstance.OnBossDied += HandleBossDefeat;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
        }
    }

    public void RegisterPlayer(PlayerNetworkCore player)
    {
        if (!IsServer || player == null) return;

        if (!activePlayers.Contains(player))
        {
            activePlayers.Add(player);
            player.OnPlayerDied += HandlePlayerDeath;
            Debug.Log($"[Manager] Player Registered. ID: {player.OwnerClientId}. Total Players: {activePlayers.Count}");
        }
    }

    public void UnregisterPlayer(PlayerNetworkCore player)
    {
        if (!IsServer || player == null) return;

        if (activePlayers.Contains(player))
        {
            player.OnPlayerDied -= HandlePlayerDeath;
            activePlayers.Remove(player);
            Debug.Log($"[Manager] Player Unregistered. ID: {player.OwnerClientId}. Remaining: {activePlayers.Count}");

            // 可选：如果玩家中途退出，是否要检查剩余人数导致游戏结束？
            // CheckGameOverCondition(); 
        }
    }

    private void HandlePlayerDeath(PlayerNetworkCore deadPlayer)
    {
        if (isGameEnded) return;

        Debug.Log($"[Manager] HandlePlayerDeath Triggered for Player {deadPlayer.OwnerClientId}");

        int aliveCount = 0;
        foreach (var p in activePlayers)
        {
            // 确保只统计未死亡且对象还存在的玩家
            if (p != null && !p.IsDead)
            {
                aliveCount++;
            }
        }

        Debug.Log($"[Manager] Alive Count: {aliveCount}");

        if (aliveCount <= 0)
        {
            EndGame(false);
        }
    }

    // 【修复】参数是 clientId
    private void HandleClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            if (client.PlayerObject != null)
            {
                RegisterPlayer(client.PlayerObject.GetComponent<PlayerNetworkCore>());
            }
        }
    }

    private void HandleClientDisconnect(ulong clientId)
    {
        // 如果你需要处理玩家断线算作死亡/移除，可以在这里处理
        // 这里暂时不需要操作，因为PlayerNetworkCore的OnNetworkDespawn会调用Unregister
    }

    private void HandleBossDefeat()
    {
        if (isGameEnded) return;
        EndGame(true);
    }

    private void EndGame(bool isVictory)
    {
        isGameEnded = true;
        Debug.Log(isVictory ? "VICTORY! Loading WinScene..." : "DEFEAT! Loading Init...");
        if (isVictory)
        {
            SceneManager.LoadScene("WinScene", LoadSceneMode.Single);
        }
        else
        {
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("Init", LoadSceneMode.Single);
        }
    }
}
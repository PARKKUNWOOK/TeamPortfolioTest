using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public struct PlayerInputData : INetworkInput
{
    public float Horizontal;
    public float Vertical;
    public bool Jump;
}

public class FusionNetwork : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner _runner;
    private GameObject _player;

    private Vector3[] spawnPoints = new Vector3[]
                                    {
                                        new Vector3(-3, 0, 0),
                                        new Vector3(-1, 0, 0),
                                        new Vector3(1, 0, 0),
                                        new Vector3(3, 0, 0),
                                    };

    float jumpBufferTime = 0.2f;
    float jumpBufferCounter = 0.0f;

    bool jumpPressed = false;

    private void Awake()
    {
        StartGame(GameMode.AutoHostOrClient);

        _player = Resources.Load<GameObject>("Prefabs/Player");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpPressed = true;
            jumpBufferCounter = jumpBufferTime;
        }

        if (jumpBufferCounter > 0.0f)
        {
            jumpBufferCounter -= Time.deltaTime;
            if (jumpBufferCounter <= 0.0f)
            {
                jumpPressed = false;
            }
        }
    }

    private async void StartGame(GameMode mode)
    {
        // Fusion Runner를 생성하고, 사용자 입력을 제공할 것임을 설정
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);

        // 현재 씬으로부터 NetworkSceneInfo 생성
        SceneRef scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        NetworkSceneInfo sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid)
        {
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive); // 씬 정보를 추가로 등록
        }

        // 게임 모드에 따라 세션을 생성하거나 참가 (세션 이름은 "TestRoom")
        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode, // 게임 모드 설정 (Host, Server, Client 등)
            SessionName = "TestRoom", // 세션 이름
            Scene = scene, // 시작할 씬
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>() // 기본 씬 매니저 추가
        });
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        // hostMigrationToken에는 이전 마스터가 가지고 있던 모든 정보가 들어 있음 (호스트 이전)
        runner.StartGame(new StartGameArgs()
        {
            HostMigrationToken = hostMigrationToken,
            GameMode = GameMode.AutoHostOrClient,
            SessionName = "TestRoom",
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        PlayerInputData data = new PlayerInputData
        {
            Horizontal = Input.GetAxisRaw("Horizontal"),
            Vertical = Input.GetAxisRaw("Vertical"),
            Jump = jumpPressed
        };

        jumpPressed = false;

        input.Set(data);
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (SceneManager.GetActiveScene().name != "InGameScene")
            return;

        if (runner.IsServer || runner.IsSharedModeMasterClient)
        {
            // 로비 위치에 스폰
            int playerIndex = player.RawEncoded % spawnPoints.Length;
            Vector3 spawnPos = spawnPoints[playerIndex];
            runner.Spawn(_player, spawnPos, Quaternion.identity, player);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        NetworkObject playerObject = runner.GetPlayerObject(player);
        if (playerObject != null)
        {
            if (playerObject.HasStateAuthority)
            {
                Debug.Log($"Player {player} had StateAuthority, handling transfer...");

                // 여기서 권한 이전 로직 작성 (예: 게임 매니저, 적 스폰 시스템 등)
                // 예: 특정 오브젝트의 StateAuthority를 다른 플레이어에게 넘긴다
                PlayerRef newOwner = FindNewValidPlayer(runner, player);
                NetworkObject newPlayerObject = runner.GetPlayerObject(newOwner);

                if (newPlayerObject != null)
                {
                    newPlayerObject.RequestStateAuthority();
                }
            }

            runner.Despawn(playerObject);
        }
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }

    private PlayerRef FindNewValidPlayer(NetworkRunner runner, PlayerRef playerToExclude)
    {
        foreach (var p in runner.ActivePlayers)
        {
            if (p != playerToExclude)
                return p;
        }
        return default;
    }
}

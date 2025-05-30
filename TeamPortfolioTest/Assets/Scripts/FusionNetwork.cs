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
        // Fusion Runner�� �����ϰ�, ����� �Է��� ������ ������ ����
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);

        // ���� �����κ��� NetworkSceneInfo ����
        SceneRef scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        NetworkSceneInfo sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid)
        {
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive); // �� ������ �߰��� ���
        }

        // ���� ��忡 ���� ������ �����ϰų� ���� (���� �̸��� "TestRoom")
        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode, // ���� ��� ���� (Host, Server, Client ��)
            SessionName = "TestRoom", // ���� �̸�
            Scene = scene, // ������ ��
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>() // �⺻ �� �Ŵ��� �߰�
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
        // hostMigrationToken���� ���� �����Ͱ� ������ �ִ� ��� ������ ��� ���� (ȣ��Ʈ ����)
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
            // �κ� ��ġ�� ����
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

                // ���⼭ ���� ���� ���� �ۼ� (��: ���� �Ŵ���, �� ���� �ý��� ��)
                // ��: Ư�� ������Ʈ�� StateAuthority�� �ٸ� �÷��̾�� �ѱ��
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

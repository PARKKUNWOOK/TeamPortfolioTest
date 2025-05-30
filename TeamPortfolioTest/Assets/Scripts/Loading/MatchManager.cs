using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MatchManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public TextMeshProUGUI playerCountText;
    public float waitTime = 10f;

    private NetworkRunner _runner;
    private float _timer;
    private bool _isCounting = false;

    private void Start()
    {
        StartCoroutine(InitializeNetwork());
    }

    private IEnumerator InitializeNetwork()
    {
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);

        yield return _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.AutoHostOrClient,
            SessionName = "TestRoom",
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    private void Update()
    {
        if (_runner == null) return;

        int playerCount = _runner.ActivePlayers.Count();
        if (playerCountText != null)
        {
            playerCountText.text = $"{playerCount}/3";
        }

        if (playerCount >= 3)
        {
            SceneManager.LoadScene("InGameScene");
        }
        else if (playerCount == 2)
        {
            if (!_isCounting)
            {
                _isCounting = true;
                _timer = waitTime;
            }

            _timer -= Time.deltaTime;

            if (_timer <= 0f)
            {
                SceneManager.LoadScene("InGameScene");
            }
        }
        else // 1명일 때
        {
            _isCounting = false;
        }
    }

    // INetworkRunnerCallbacks 메서드 구현 (내용 비워도 됨)
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        throw new NotImplementedException();
    }
}
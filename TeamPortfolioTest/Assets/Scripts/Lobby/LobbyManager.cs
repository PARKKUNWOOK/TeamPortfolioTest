using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    private string _playerPrefabPath = "Prefabs/Player";
    private Transform _playerSettingPosition;
    private TextMeshProUGUI _nicknameText;
    private Button _gameStartButton;
    private Button _logoutButton;

    private void Start()
    {
        _playerSettingPosition = GameObject.Find("PlayerSetingPosition")?.transform;

        GameObject nicknameTextObj = GameObject.Find("NickNameText");
        if (nicknameTextObj != null)
            _nicknameText = nicknameTextObj.GetComponent<TextMeshProUGUI>();

        GameObject startButtonObj = GameObject.Find("GameStartButton");
        if (startButtonObj != null)
        {
            _gameStartButton = startButtonObj.GetComponent<Button>();
            _gameStartButton.onClick.AddListener(OnClickGameStart);
        }

        GameObject logoutButtonObj = GameObject.Find("LogoutButton");
        {
            if (logoutButtonObj != null)
            {
                _logoutButton = logoutButtonObj.GetComponent<Button>();
                _logoutButton.onClick.AddListener(OnClickLogout);
            }
        }

        SpawnPlayer();
        SetNickNameUI();
    }

    private void SpawnPlayer()
    {
        GameObject playerPrefab = Resources.Load<GameObject>(_playerPrefabPath);
        if (playerPrefab == null || _playerSettingPosition == null) return;

        Instantiate(playerPrefab, _playerSettingPosition.position, _playerSettingPosition.rotation);
    }

    private void SetNickNameUI()
    {
        FirebaseAuthManager.Instance.LoadNickname((nickname) =>
        {
            if (_nicknameText != null)
                _nicknameText.text = nickname;
        });
    }

    private void OnClickLogout()
    {
        PlayerPrefs.SetString("AutoLogin", "false");
        PlayerPrefs.DeleteKey("Email");
        PlayerPrefs.DeleteKey("Password");

        SceneManager.LoadScene("LoginScene");
    }

    private void OnClickGameStart()
    {
        SceneManager.LoadScene("LoadingScene");
    }
}

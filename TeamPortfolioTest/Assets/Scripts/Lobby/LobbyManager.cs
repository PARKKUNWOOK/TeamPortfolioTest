using TMPro;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    private string playerPrefabPath = "Prefabs/Player";
    private Transform playerSettingPosition;
    private TextMeshProUGUI nicknameText;

    private void Start()
    {
        playerSettingPosition = GameObject.Find("PlayerSetingPosition")?.transform;
        GameObject nicknameTextObj = GameObject.Find("NickNameText");
        if (nicknameTextObj != null)
            nicknameText = nicknameTextObj.GetComponent<TextMeshProUGUI>();

        SpawnPlayer();
        SetNickNameUI();
    }

    private void SpawnPlayer()
    {
        GameObject playerPrefab = Resources.Load<GameObject>(playerPrefabPath);
        if (playerPrefab == null || playerSettingPosition == null) return;

        Instantiate(playerPrefab, playerSettingPosition.position, playerSettingPosition.rotation);
    }

    private void SetNickNameUI()
    {
        FirebaseAuthManager.Instance.LoadNickname((nickname) =>
        {
            if (nicknameText != null)
                nicknameText.text = nickname;
        });
    }
}

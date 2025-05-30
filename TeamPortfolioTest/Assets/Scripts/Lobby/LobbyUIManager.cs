using UnityEngine;

public class LobbyUIManager : MonoBehaviour
{
    //임시로 만든 스크립트와 오브젝트

    public void OnClickXButton()
    {
        // 자동 로그인 해제
        PlayerPrefs.SetString("AutoLogin", "false");
        PlayerPrefs.DeleteKey("Email");
        PlayerPrefs.DeleteKey("Password");

        // LoginScene으로 이동
        UnityEngine.SceneManagement.SceneManager.LoadScene("LoginScene");
    }
}

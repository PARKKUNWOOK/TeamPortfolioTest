using UnityEngine;

public class LobbyUIManager : MonoBehaviour
{
    //�ӽ÷� ���� ��ũ��Ʈ�� ������Ʈ

    public void OnClickXButton()
    {
        // �ڵ� �α��� ����
        PlayerPrefs.SetString("AutoLogin", "false");
        PlayerPrefs.DeleteKey("Email");
        PlayerPrefs.DeleteKey("Password");

        // LoginScene���� �̵�
        UnityEngine.SceneManagement.SceneManager.LoadScene("LoginScene");
    }
}

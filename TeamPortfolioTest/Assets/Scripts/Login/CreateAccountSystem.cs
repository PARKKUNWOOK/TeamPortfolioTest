using Firebase;
using Firebase.Auth;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreateAccountSystem : MonoBehaviour
{
    private static CreateAccountSystem _instance;

    private GameObject _createAccountWindow;
    private GameObject _loginWindow;

    private enum CreateAccountInputFieldIndex
    {
        ID, NickName, Password, PasswordCheck
    }

    private enum CreateAccountButtonType
    {
        IDCheckBtn, NickNameCheckBtn, PasswordCheckBtn, CreateBtn, CancelBtn
    }

    private enum CreateAccountCheckResultType
    {
        IDCheckResultText, NickNameCheckResultText, PasswordCheckResultText
    }

    private Button[] _buttons;
    private TMP_InputField[] _inputFields;
    private TextMeshProUGUI[] _textMeshProUGUIs;

    private bool _isEmailAvailable = false;
    private bool _isNicknameAvailable = false;
    private bool _isPasswordMatched = false;
    private bool _shouldSwitchToLogin = false;

    private void Awake()
    {
        _instance = this;
    }

    private void Start()
    {
        _createAccountWindow = transform.Find("CreateAccountWindow").gameObject;
        _loginWindow = transform.Find("LoginWindow").gameObject;

        // CreateAccountWindow ���� Ʈ������ �������� ������Ʈ ã��
        Transform root = _createAccountWindow.transform;

        // �迭 �Ҵ�
        _inputFields = new TMP_InputField[4];
        _buttons = new Button[5];
        _textMeshProUGUIs = new TextMeshProUGUI[3];

        // TMP_InputField �ʱ�ȭ
        _inputFields[(int)CreateAccountInputFieldIndex.ID] = root.Find("IDInputField").GetComponent<TMP_InputField>();
        _inputFields[(int)CreateAccountInputFieldIndex.NickName] = root.Find("NickNameInputField").GetComponent<TMP_InputField>();
        _inputFields[(int)CreateAccountInputFieldIndex.Password] = root.Find("PasswordInputField").GetComponent<TMP_InputField>();
        _inputFields[(int)CreateAccountInputFieldIndex.PasswordCheck] = root.Find("PasswordCheckInputField").GetComponent<TMP_InputField>();

        // Button �ʱ�ȭ
        _buttons[(int)CreateAccountButtonType.IDCheckBtn] = root.Find("IDDuplicateCheckButton").GetComponent<Button>();
        _buttons[(int)CreateAccountButtonType.NickNameCheckBtn] = root.Find("NickNameDuplicateCheckButton").GetComponent<Button>();
        _buttons[(int)CreateAccountButtonType.PasswordCheckBtn] = root.Find("PasswordCheckButton").GetComponent<Button>();
        _buttons[(int)CreateAccountButtonType.CreateBtn] = root.Find("CreateButton").GetComponent<Button>();
        _buttons[(int)CreateAccountButtonType.CancelBtn] = root.Find("CancelButton").GetComponent<Button>();

        // Text ��� �ʱ�ȭ
        _textMeshProUGUIs[(int)CreateAccountCheckResultType.IDCheckResultText] = root.Find("IDDuplicateCheckText").GetComponent<TextMeshProUGUI>();
        _textMeshProUGUIs[(int)CreateAccountCheckResultType.NickNameCheckResultText] = root.Find("NickNameDuplicateCheckText").GetComponent<TextMeshProUGUI>();
        _textMeshProUGUIs[(int)CreateAccountCheckResultType.PasswordCheckResultText] = root.Find("PasswordCheckText").GetComponent<TextMeshProUGUI>();

        // ��ư ������ ����
        _buttons[(int)CreateAccountButtonType.CreateBtn].onClick.AddListener(OnCreateClicked);
        _buttons[(int)CreateAccountButtonType.CancelBtn].onClick.AddListener(OnCancelClicked);
        _buttons[(int)CreateAccountButtonType.IDCheckBtn].onClick.AddListener(OnEmailCheckClicked);
        _buttons[(int)CreateAccountButtonType.NickNameCheckBtn].onClick.AddListener(OnNicknameCheckClicked);
        _buttons[(int)CreateAccountButtonType.PasswordCheckBtn].onClick.AddListener(OnPasswordCheckClicked);

        _buttons[(int)CreateAccountButtonType.CreateBtn].interactable = false;
        _createAccountWindow.SetActive(false);
    }

    private void Update()
    {
        if (_shouldSwitchToLogin)
        {
            _shouldSwitchToLogin = false;
            _createAccountWindow.SetActive(false);
            _loginWindow.SetActive(true);
        }
    }

    public static void OpenWindow()
    {
        if (_instance != null)
        {
            _instance._OpenWindowInternal();
        }
    }

    private void _OpenWindowInternal()
    {
        _createAccountWindow.SetActive(true);
        _loginWindow.SetActive(false);

        foreach (var input in _inputFields)
            input.text = "";

        foreach (var text in _textMeshProUGUIs)
        {
            text.text = "";
            text.color = Color.white;
        }

        _isEmailAvailable = false;
        _isNicknameAvailable = false;
        _isPasswordMatched = false;

        _buttons[(int)CreateAccountButtonType.CreateBtn].interactable = false;
    }

    private void OnCancelClicked()
    {
        _createAccountWindow.SetActive(false);
        _loginWindow.SetActive(true);
    }

    private void OnEmailCheckClicked()
    {
        string email = _inputFields[(int)CreateAccountInputFieldIndex.ID].text;

        if (string.IsNullOrEmpty(email))
        {
            SetCheckResult(CreateAccountCheckResultType.IDCheckResultText, "�̸����� �Է����ּ���.", Color.red);
            return;
        }

        FirebaseAuthManager.Instance.CheckEmailDuplicate(email, (isDuplicate) =>
        {
            if (isDuplicate)
            {
                SetCheckResult(CreateAccountCheckResultType.IDCheckResultText, "�̹� ������� ���̵��Դϴ�.", Color.red);
                _isEmailAvailable = false;
            }
            else
            {
                SetCheckResult(CreateAccountCheckResultType.IDCheckResultText, "��밡���� ���̵��Դϴ�.", Color.green);
                _isEmailAvailable = true;
            }

            UpdateCreateButtonState();
        });
    }

    private void OnNicknameCheckClicked()
    {
        string nickname = _inputFields[(int)CreateAccountInputFieldIndex.NickName].text;

        if (string.IsNullOrEmpty(nickname))
        {
            SetCheckResult(CreateAccountCheckResultType.NickNameCheckResultText, "�г����� �Է����ּ���.", Color.red);
            return;
        }

        FirebaseAuthManager.Instance.CheckNicknameDuplicate(nickname, (isDuplicate) =>
        {
            if (isDuplicate)
            {
                SetCheckResult(CreateAccountCheckResultType.NickNameCheckResultText, "�̹� ������� �г����Դϴ�.", Color.red);
                _isNicknameAvailable = false;
            }
            else
            {
                SetCheckResult(CreateAccountCheckResultType.NickNameCheckResultText, "��밡���� �г����Դϴ�.", Color.green);
                _isNicknameAvailable = true;
            }

            UpdateCreateButtonState();
        });
    }

    private void OnPasswordCheckClicked()
    {
        string pw = _inputFields[(int)CreateAccountInputFieldIndex.Password].text;
        string pwCheck = _inputFields[(int)CreateAccountInputFieldIndex.PasswordCheck].text;

        if (pw == pwCheck && pw.Length >= 6)
        {
            SetCheckResult(CreateAccountCheckResultType.PasswordCheckResultText, "��й�ȣ�� ��ġ�մϴ�.", Color.green);
            _isPasswordMatched = true;
        }
        else
        {
            SetCheckResult(CreateAccountCheckResultType.PasswordCheckResultText, "��й�ȣ�� ��ġ���� �ʽ��ϴ�.", Color.red);
            _isPasswordMatched = false;
        }

        UpdateCreateButtonState();
    }

    private void OnCreateClicked()
    {
        if (!_isEmailAvailable)
        {
            SetCheckResult(CreateAccountCheckResultType.IDCheckResultText, "���̵� �ߺ�üũ�� ���ּ���.", Color.red);
            return;
        }

        if (!_isNicknameAvailable)
        {
            SetCheckResult(CreateAccountCheckResultType.NickNameCheckResultText, "�г��� �ߺ�üũ�� ���ּ���.", Color.red);
            return;
        }

        if (!_isPasswordMatched)
        {
            SetCheckResult(CreateAccountCheckResultType.PasswordCheckResultText, "��й�ȣ Ȯ��üũ�� ���ּ���.", Color.red);
            return;
        }

        string email = _inputFields[(int)CreateAccountInputFieldIndex.ID].text;
        string nickname = _inputFields[(int)CreateAccountInputFieldIndex.NickName].text;
        string password = _inputFields[(int)CreateAccountInputFieldIndex.Password].text;

        FirebaseAuthManager.Instance.Create(email, password, nickname, () =>
        {
            _shouldSwitchToLogin = true;
        });
    }

    private void SetCheckResult(CreateAccountCheckResultType type, string message, Color color)
    {
        _textMeshProUGUIs[(int)type].text = message;
        _textMeshProUGUIs[(int)type].color = color;
    }

    private void UpdateCreateButtonState()
    {
        _buttons[(int)CreateAccountButtonType.CreateBtn].interactable =
            _isEmailAvailable && _isPasswordMatched && _isNicknameAvailable;
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;

public class FirebaseAuthManager
{
    private static FirebaseAuthManager _instance = null;
    public static FirebaseAuthManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = new FirebaseAuthManager();
            return _instance;
        }
    }

    private FirebaseAuth _auth;
    private FirebaseUser _user;
    private DatabaseReference _dbRef;

    private bool firebaseInitialized = false;
    private bool isCreatingAccount = false;

    public string UserId => _user?.UserId;
    public Action<bool> LoginState;

    public async void Init()
    {
        var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (dependencyStatus == DependencyStatus.Available)
        {
            FirebaseApp app = FirebaseApp.DefaultInstance;

            // Database URL ����
            app.Options.DatabaseUrl = new Uri("https://teamportfoliotest-default-rtdb.firebaseio.com/");
            _dbRef = FirebaseDatabase.GetInstance(app).RootReference;

            _auth = FirebaseAuth.DefaultInstance;
            firebaseInitialized = true;

            string autoLogin = PlayerPrefs.GetString("AutoLogin", "false");
            if (autoLogin == "true")
            {
                string savedEmail = PlayerPrefs.GetString("Email", "");
                string savedPassword = PlayerPrefs.GetString("Password", "");

                if (!string.IsNullOrEmpty(savedEmail) && !string.IsNullOrEmpty(savedPassword))
                {
                    LogIn(savedEmail, savedPassword);
                }
            }
            else
            {
                // �ڵ� �α����� ���� ������ �α׾ƿ� ó��
                if (_auth.CurrentUser != null)
                {
                    LogOut();
                }
            }

            _auth.StateChanged += OnChanged;
            Debug.Log("Firebase �ʱ�ȭ �Ϸ�");
        }
        else
        {
            Debug.LogError("Firebase dependencies not available: " + dependencyStatus.ToString());
        }
    }

    private void OnChanged(object sender, EventArgs e)
    {
        if (_auth.CurrentUser != _user)
        {
            bool signedIn = (_auth.CurrentUser != null);
            _user = _auth.CurrentUser;

            if (signedIn)
            {
                if (isCreatingAccount)
                {
                    // ȸ������ ���� �ڵ� �α����� ����
                    Debug.Log("ȸ������ �� �ڵ� �α��� ����");
                    return;
                }

                Debug.Log("�α���");
                LoginState?.Invoke(true);
            }
            else
            {
                Debug.Log("�α׾ƿ�");
                LoginState?.Invoke(false);
            }
        }
    }

    public async void CheckEmailDuplicate(string email, Action<bool> onCheckComplete)
    {
        if (!firebaseInitialized)
        {
            Debug.LogError("Firebase�� �ʱ�ȭ���� �ʾҽ��ϴ�.");
            onCheckComplete?.Invoke(true); // �ߺ� ó��
            return;
        }

        if (string.IsNullOrEmpty(email))
        {
            onCheckComplete?.Invoke(true);
            return;
        }

        try
        {
            string safeEmailKey = email.Replace(".", "_").Replace("@", "_at_");
            DataSnapshot snapshot = await _dbRef.Child("Emails").Child(safeEmailKey).GetValueAsync();
            bool exists = snapshot.Exists;
            onCheckComplete?.Invoke(exists);
        }
        catch (Exception ex)
        {
            Debug.LogError("�̸��� �ߺ� Ȯ�� ����: " + ex.Message);
            onCheckComplete?.Invoke(true); // ���� �� �ߺ� ó��
        }
    }

    public async void CheckNicknameDuplicate(string nickname, Action<bool> onCheckComplete)
    {
        if (!firebaseInitialized)
        {
            Debug.LogError("Firebase�� �ʱ�ȭ���� �ʾҽ��ϴ�.");
            onCheckComplete?.Invoke(true); // �ߺ����� ó��
            return;
        }

        if (string.IsNullOrEmpty(nickname))
        {
            onCheckComplete?.Invoke(true);
            return;
        }

        try
        {
            DataSnapshot snapshot = await _dbRef.Child("Nicknames").Child(nickname).GetValueAsync();
            bool exists = snapshot.Exists;
            onCheckComplete?.Invoke(exists);
        }
        catch (Exception ex)
        {
            Debug.LogError("�г��� �ߺ� Ȯ�� ����: " + ex.Message);
            onCheckComplete?.Invoke(true); // ���� �� �ߺ����� ó��
        }
    }

    public void Create(string email, string password, string nickname, Action onSuccess = null)
    {
        if (!firebaseInitialized)
        {
            Debug.LogError("Firebase�� �ʱ�ȭ���� �ʾҽ��ϴ�.");
            return;
        }

        isCreatingAccount = true;

        _auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWith(async task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("ȸ������ ����");
                isCreatingAccount = false;
                return;
            }

            FirebaseUser newUser = task.Result.User;
            Debug.Log("ȸ������ �Ϸ�: " + newUser.UserId);

            try
            {
                string safeEmailKey = email.Replace(".", "_").Replace("@", "_at_");
                await _dbRef.Child("Emails").Child(safeEmailKey).SetValueAsync(newUser.UserId);
                await _dbRef.Child("Nicknames").Child(nickname).SetValueAsync(newUser.UserId);
            }
            catch (Exception ex)
            {
                Debug.LogError("���� ���� ���� ����: " + ex.Message);
            }

            LogOut(); // �α׾ƿ� ó��

            isCreatingAccount = false;
            onSuccess?.Invoke();
        });
    }

    public void LogIn(string email, string password)
    {
        if (!firebaseInitialized)
        {
            Debug.LogError("Firebase�� �ʱ�ȭ���� �ʾҽ��ϴ�.");
            return;
        }

        _auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("�α��� ���");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("�α��� ����");
                return;
            }

            FirebaseUser newUser = task.Result.User;
            Debug.Log("�α��� �Ϸ�: " + newUser.UserId);
        });
    }

    public void LogOut()
    {
        _auth?.SignOut();
        Debug.Log("�α׾ƿ�");
    }
}

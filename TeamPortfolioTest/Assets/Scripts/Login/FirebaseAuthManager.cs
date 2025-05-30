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

            // Database URL 설정
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
                // 자동 로그인이 꺼져 있으면 로그아웃 처리
                if (_auth.CurrentUser != null)
                {
                    LogOut();
                }
            }

            _auth.StateChanged += OnChanged;
            Debug.Log("Firebase 초기화 완료");
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
                    // 회원가입 직후 자동 로그인은 무시
                    Debug.Log("회원가입 중 자동 로그인 무시");
                    return;
                }

                Debug.Log("로그인");
                LoginState?.Invoke(true);
            }
            else
            {
                Debug.Log("로그아웃");
                LoginState?.Invoke(false);
            }
        }
    }

    public async void CheckEmailDuplicate(string email, Action<bool> onCheckComplete)
    {
        if (!firebaseInitialized)
        {
            Debug.LogError("Firebase가 초기화되지 않았습니다.");
            onCheckComplete?.Invoke(true); // 중복 처리
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
            Debug.LogError("이메일 중복 확인 실패: " + ex.Message);
            onCheckComplete?.Invoke(true); // 실패 시 중복 처리
        }
    }

    public async void CheckNicknameDuplicate(string nickname, Action<bool> onCheckComplete)
    {
        if (!firebaseInitialized)
        {
            Debug.LogError("Firebase가 초기화되지 않았습니다.");
            onCheckComplete?.Invoke(true); // 중복으로 처리
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
            Debug.LogError("닉네임 중복 확인 실패: " + ex.Message);
            onCheckComplete?.Invoke(true); // 실패 시 중복으로 처리
        }
    }

    public void Create(string email, string password, string nickname, Action onSuccess = null)
    {
        if (!firebaseInitialized)
        {
            Debug.LogError("Firebase가 초기화되지 않았습니다.");
            return;
        }

        isCreatingAccount = true;

        _auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWith(async task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("회원가입 실패");
                isCreatingAccount = false;
                return;
            }

            FirebaseUser newUser = task.Result.User;
            Debug.Log("회원가입 완료: " + newUser.UserId);

            try
            {
                string safeEmailKey = email.Replace(".", "_").Replace("@", "_at_");
                await _dbRef.Child("Emails").Child(safeEmailKey).SetValueAsync(newUser.UserId);
                await _dbRef.Child("Nicknames").Child(nickname).SetValueAsync(newUser.UserId);
            }
            catch (Exception ex)
            {
                Debug.LogError("유저 정보 저장 실패: " + ex.Message);
            }

            LogOut(); // 로그아웃 처리

            isCreatingAccount = false;
            onSuccess?.Invoke();
        });
    }

    public void LogIn(string email, string password)
    {
        if (!firebaseInitialized)
        {
            Debug.LogError("Firebase가 초기화되지 않았습니다.");
            return;
        }

        _auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("로그인 취소");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("로그인 실패");
                return;
            }

            FirebaseUser newUser = task.Result.User;
            Debug.Log("로그인 완료: " + newUser.UserId);
        });
    }

    public void LogOut()
    {
        _auth?.SignOut();
        Debug.Log("로그아웃");
    }
}

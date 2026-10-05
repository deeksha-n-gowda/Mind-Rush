using System;
using System.Collections;
using UnityEngine;
using MindRush.Network.DTOs;

namespace MindRush.Network
{
    public class AuthManager : MonoBehaviour
    {
        private static AuthManager _instance;
        public static AuthManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("AuthManager");
                    _instance = go.AddComponent<AuthManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        public event Action<PlayerData> OnLoginSuccess;
        public event Action<string> OnLoginFailed;
        public event Action OnLogout;

        private const string TokenPrefsKey = "mindrush_auth_token";
        private const string UsernamePrefsKey = "mindrush_username";

        public PlayerData CurrentPlayer { get; private set; }
        public bool IsLoggedIn => CurrentPlayer != null && ApiClient.Instance.IsAuthenticated;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Try auto-login on start
            TryAutoLogin();
        }

        private void TryAutoLogin()
        {
            string savedToken = PlayerPrefs.GetString(TokenPrefsKey, "");
            string savedUsername = PlayerPrefs.GetString(UsernamePrefsKey, "");

            if (!string.IsNullOrEmpty(savedToken))
            {
                ApiClient.Instance.SetAuthToken(savedToken);
                StartCoroutine(ValidateTokenAndRestore(savedUsername));
            }
        }

        private IEnumerator ValidateTokenAndRestore(string username)
        {
            var response = yield return ApiClient.Instance.Get<AuthResponse>(ApiRoutes.Me);
            
            if (response.success && response.data?.player != null)
            {
                CurrentPlayer = response.data.player;
                OnLoginSuccess?.Invoke(CurrentPlayer);
                Debug.Log($"[AuthManager] Auto-login successful for {CurrentPlayer.username}");
            }
            else
            {
                // Token expired or invalid
                ClearStoredCredentials();
                Debug.Log("[AuthManager] Saved token invalid, need fresh login");
            }
        }

        public void Register(string username, string email, string password, Action<bool, string> callback = null)
        {
            var request = new RegisterRequest
            {
                username = username,
                email = email,
                password = password
            };

            StartCoroutine(RegisterRoutine(request, callback));
        }

        private IEnumerator RegisterRoutine(RegisterRequest request, Action<bool, string> callback)
        {
            var response = yield return ApiClient.Instance.Post<AuthResponse>(ApiRoutes.Register, request);

            if (response.success && response.data != null)
            {
                HandleAuthSuccess(response.data);
                callback?.Invoke(true, "Registered successfully");
            }
            else
            {
                callback?.Invoke(false, response.error ?? "Registration failed");
                OnLoginFailed?.Invoke(response.error ?? "Registration failed");
            }
        }

        public void Login(string username, string password, Action<bool, string> callback = null)
        {
            var request = new LoginRequest
            {
                username = username,
                password = password
            };

            StartCoroutine(LoginRoutine(request, callback));
        }

        private IEnumerator LoginRoutine(LoginRequest request, Action<bool, string> callback)
        {
            var response = yield return ApiClient.Instance.Post<AuthResponse>(ApiRoutes.Login, request);

            if (response.success && response.data != null)
            {
                HandleAuthSuccess(response.data);
                callback?.Invoke(true, "Logged in successfully");
            }
            else
            {
                callback?.Invoke(false, response.error ?? "Login failed");
                OnLoginFailed?.Invoke(response.error ?? "Login failed");
            }
        }

        private void HandleAuthSuccess(AuthResponse authData)
        {
            CurrentPlayer = authData.player;
            ApiClient.Instance.SetAuthToken(authData.token);

            // Persist credentials
            PlayerPrefs.SetString(TokenPrefsKey, authData.token);
            PlayerPrefs.SetString(UsernamePrefsKey, authData.player.username);
            PlayerPrefs.Save();

            OnLoginSuccess?.Invoke(CurrentPlayer);
            Debug.Log($"[AuthManager] Login successful for {CurrentPlayer.username}");
        }

        public void Logout()
        {
            CurrentPlayer = null;
            ApiClient.Instance.ClearAuthToken();
            ClearStoredCredentials();
            OnLogout?.Invoke();
            Debug.Log("[AuthManager] Logged out");
        }

        private void ClearStoredCredentials()
        {
            PlayerPrefs.DeleteKey(TokenPrefsKey);
            PlayerPrefs.DeleteKey(UsernamePrefsKey);
            PlayerPrefs.Save();
        }

        public void RefreshToken(Action<bool> callback = null)
        {
            if (!IsLoggedIn)
            {
                callback?.Invoke(false);
                return;
            }

            StartCoroutine(RefreshTokenRoutine(callback));
        }

        private IEnumerator RefreshTokenRoutine(Action<bool> callback)
        {
            var response = yield return ApiClient.Instance.Post<AuthResponse>(ApiRoutes.Refresh, null);

            if (response.success && response.data != null)
            {
                HandleAuthSuccess(response.data);
                callback?.Invoke(true);
            }
            else
            {
                Logout();
                callback?.Invoke(false);
            }
        }

        public void UpdateLocalPlayerData(PlayerData updatedData)
        {
            CurrentPlayer = updatedData;
            OnLoginSuccess?.Invoke(CurrentPlayer);
        }
    }
}
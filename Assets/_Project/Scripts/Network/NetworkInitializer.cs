using UnityEngine;

namespace MindRush.Network
{
    /// <summary>
    /// Initializes all network services on game start.
    /// Add this to a GameObject in your first loading scene.
    /// </summary>
    public class NetworkInitializer : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Base API URL - change for production deployment")]
        public string apiBaseUrl = "http://localhost:5000";

        [Header("Auto-initialize")]
        public bool initializeOnStart = true;

        private void Awake()
        {
            // Override API base URL if configured
            if (!string.IsNullOrEmpty(apiBaseUrl))
            {
                // Note: ApiRoutes.BaseUrl is const, so we'd need to change it to static readonly
                // For now, ApiClient uses ApiRoutes.BaseUrl directly
            }

            if (initializeOnStart)
            {
                InitializeServices();
            }
        }

        private void InitializeServices()
        {
            // Force creation of all singletons in correct order
            var _ = ApiClient.Instance;
            var _ = AuthManager.Instance;
            var _ = ScoreService.Instance;
            var _ = CharacterService.Instance;
            var _ = MimiService.Instance;

            Debug.Log("[NetworkInitializer] All network services initialized");

            // Subscribe to auth events for debugging
            AuthManager.Instance.OnLoginSuccess += (player) => 
                Debug.Log($"[Network] Logged in as {player.username} (Level {player.level})");
            
            AuthManager.Instance.OnLoginFailed += (error) => 
                Debug.LogWarning($"[Network] Login failed: {error}");
            
            AuthManager.Instance.OnLogout += () => 
                Debug.Log("[Network] User logged out");
        }

        /// <summary>
        /// Call this to manually initialize if not using auto-initialize
        /// </summary>
        public void Initialize()
        {
            InitializeServices();
        }

        /// <summary>
        /// Quick test method - call from a debug button or test script
        /// </summary>
        [ContextMenu("Test Connection")]
        public async void TestConnection()
        {
            Debug.Log("[Network] Testing connection to API...");
            var response = await ApiClient.Instance.Get<dynamic>(ApiRoutes.Health);
            
            if (response.success)
            {
                Debug.Log("[Network] ✓ API connection successful!");
            }
            else
            {
                Debug.LogError($"[Network] ✗ API connection failed: {response.error}");
            }
        }
    }
}
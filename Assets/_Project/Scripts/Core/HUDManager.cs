using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MindRush.Player;
using MindRush.Network;

namespace MindRush.Core
{
    /// <summary>
    /// Live HUD during gameplay - shows score, distance, ideas, gems, multiplier
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        [Header("Text Components")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI distanceText;
        [SerializeField] private TextMeshProUGUI ideasText;
        [SerializeField] private TextMeshProUGUI gemsText;
        [SerializeField] private TextMeshProUGUI multiplierText;
        [SerializeField] private TextMeshProUGUI speedText;

        [Header("Character Info")]
        [SerializeField] private TextMeshProUGUI characterNameText;
        [SerializeField] private Image characterIcon;

        [Header("References")]
        [SerializeField] private PlayerController playerController;

        private int lastScore = 0;

        private void Awake()
        {
            // Subscribe to auth events to update character info
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.OnLoginSuccess += UpdateCharacterInfo;
                UpdateCharacterInfo(AuthManager.Instance.CurrentPlayer);
            }
        }

        private void OnDestroy()
        {
            if (AuthManager.Instance != null)
                AuthManager.Instance.OnLoginSuccess -= UpdateCharacterInfo;
        }

        private void Update()
        {
            if (playerController == null) return;

            // Calculate live score
            int currentScore = CalculateLiveScore();
            
            if (scoreText != null && currentScore != lastScore)
            {
                scoreText.text = $"SCORE: {currentScore:N0}";
                lastScore = currentScore;
            }

            if (distanceText != null)
                distanceText.text = $"{Mathf.RoundToInt(playerController.DistanceTraveled):N0}m";

            if (ideasText != null)
                ideasText.text = $"×{playerController.IdeasCollected}";

            if (gemsText != null)
                gemsText.text = $"×{playerController.GemsCollected}";

            if (multiplierText != null)
            {
                float multiplier = 1f + (playerController.CurrentSpeed - 8f) / 14f; // 1x to 2x
                multiplierText.text = $"{multiplier:F1}x";
            }

            if (speedText != null)
                speedText.text = $"{playerController.CurrentSpeed:F0}";
        }

        private int CalculateLiveScore()
        {
            if (playerController == null) return 0;
            
            int distanceScore = Mathf.RoundToInt(playerController.DistanceTraveled);
            int ideaBonus = playerController.IdeasCollected * 50;
            int gemBonus = playerController.GemsCollected * 200;
            int speedBonus = Mathf.RoundToInt((playerController.CurrentSpeed - 8f) * 10f);
            
            return distanceScore + ideaBonus + gemBonus + speedBonus;
        }

        private void UpdateCharacterInfo(PlayerData player)
        {
            if (player == null) return;

            if (characterNameText != null)
                characterNameText.text = player.equipped_character.ToUpper();

            // Could load character icon here based on equipped_character
        }

        public void SetPlayerController(PlayerController pc)
        {
            playerController = pc;
        }
    }
}
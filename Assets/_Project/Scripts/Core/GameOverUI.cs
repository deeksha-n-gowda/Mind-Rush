using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using MindRush.Player;
using MindRush.Network;

namespace MindRush.Core
{
    public class GameOverUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject leaderboardPanel;

        [Header("Text Components")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI bestScoreText;
        [SerializeField] private TextMeshProUGUI distanceText;
        [SerializeField] private TextMeshProUGUI ideasText;
        [SerializeField] private TextMeshProUGUI gemsText;
        [SerializeField] private TextMeshProUGUI playtimeText;

        [Header("Leaderboard")]
        [SerializeField] private Transform leaderboardContainer;
        [SerializeField] private GameObject leaderboardEntryPrefab;

        [Header("References")]
        [SerializeField] private PlayerController playerController;

        private void Awake()
        {
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);
            if (leaderboardPanel != null)
                leaderboardPanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (playerController != null)
                playerController.OnPlayerDied += ShowGameOverScreen;
        }

        private void OnDisable()
        {
            if (playerController != null)
                playerController.OnPlayerDied -= ShowGameOverScreen;
        }

        private void ShowGameOverScreen()
        {
            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);

            // Update stats
            if (playerController != null)
            {
                int finalScore = CalculateScore();
                int distance = Mathf.RoundToInt(playerController.DistanceTraveled);
                int ideas = playerController.IdeasCollected;
                int gems = playerController.GemsCollected;
                int playtime = Mathf.RoundToInt(playerController.PlaytimeSeconds);

                if (scoreText != null) scoreText.text = $"SCORE: {finalScore:N0}";
                if (distanceText != null) distanceText.text = $"DISTANCE: {distance:N0}m";
                if (ideasText != null) ideasText.text = $"IDEAS: {ideas}";
                if (gemsText != null) gemsText.text = $"GEMS: {gems}";
                if (playtimeText != null) playtimeText.text = $"TIME: {FormatTime(playtime)}";

                // Fetch and show leaderboard if logged in
                if (AuthManager.Instance != null && AuthManager.Instance.IsLoggedIn)
                {
                    FetchLeaderboard();
                    FetchBestScore();
                }
                else
                {
                    if (bestScoreText != null) bestScoreText.text = "BEST: Login to track";
                    if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
                }
            }
        }

        private int CalculateScore()
        {
            if (playerController == null) return 0;
            
            int distanceScore = Mathf.RoundToInt(playerController.DistanceTraveled);
            int ideaBonus = playerController.IdeasCollected * 50;
            int gemBonus = playerController.GemsCollected * 200;
            int speedBonus = Mathf.RoundToInt((playerController.CurrentSpeed - 8f) * 10f);
            
            return distanceScore + ideaBonus + gemBonus + speedBonus;
        }

        private string FormatTime(int seconds)
        {
            int mins = seconds / 60;
            int secs = seconds % 60;
            return $"{mins:D2}:{secs:D2}";
        }

        private void FetchBestScore()
        {
            ScoreService.Instance.GetBestScore((success, score) =>
            {
                if (success && score != null && bestScoreText != null)
                {
                    bestScoreText.text = $"BEST: {score.score:N0}";
                }
                else if (bestScoreText != null)
                {
                    bestScoreText.text = "BEST: --";
                }
            });
        }

        private void FetchLeaderboard()
        {
            if (leaderboardPanel != null)
                leaderboardPanel.SetActive(true);

            ScoreService.Instance.GetLeaderboard(10, 0, (success, entries) =>
            {
                if (success && leaderboardContainer != null && leaderboardEntryPrefab != null)
                {
                    PopulateLeaderboard(entries);
                }
            });
        }

        private void PopulateLeaderboard(LeaderboardEntry[] entries)
        {
            // Clear existing entries
            foreach (Transform child in leaderboardContainer)
                Destroy(child.gameObject);

            // Create new entries
            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                var entryObj = Instantiate(leaderboardEntryPrefab, leaderboardContainer);
                
                // Try to find text components in the prefab
                var texts = entryObj.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 3)
                {
                    texts[0].text = $"#{entry.rank}";
                    texts[1].text = entry.username;
                    texts[2].text = $"{entry.score:N0}";
                }
            }
        }

        public void RestartGame()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void ShowLeaderboard()
        {
            if (leaderboardPanel != null)
                leaderboardPanel.SetActive(true);
            
            FetchLeaderboard();
        }

        public void HideLeaderboard()
        {
            if (leaderboardPanel != null)
                leaderboardPanel.SetActive(false);
        }
    }
}
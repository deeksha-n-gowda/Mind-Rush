using UnityEngine;
using UnityEngine.SceneManagement;
using CyberpunkRunner.Player;

namespace CyberpunkRunner.Core
{
    public class GameOverUI : MonoBehaviour
    {
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private PlayerController playerController;

        private void Awake()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (playerController != null)
            {
                playerController.OnPlayerDied += ShowGameOverScreen;
            }
        }

        private void OnDisable()
        {
            if (playerController != null)
            {
                playerController.OnPlayerDied -= ShowGameOverScreen;
            }
        }

        private void ShowGameOverScreen()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
        }

        public void RestartGame()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
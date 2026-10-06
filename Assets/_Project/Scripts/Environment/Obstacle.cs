using UnityEngine;
using MindRush.Player;

namespace MindRush.Environment
{
    /// <summary>
    /// Attached to track barriers/obstacles.
    /// Detects player collision and triggers transition to DeadState.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Obstacle : MonoBehaviour
    {
        [Header("Obstacle Type")]
        [Tooltip("Identifier for this obstacle type (used for death cause tracking)")]
        [SerializeField] private string obstacleType = "obstacle";

        private void Awake()
        {
            // Ensure the collider is configured as a trigger for crisp arcade collision
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                PlayerController player = other.GetComponent<PlayerController>();
                if (player != null)
                {
                    player.SetCauseOfDeath(obstacleType);
                    player.Die();
                }
            }
        }
    }
}
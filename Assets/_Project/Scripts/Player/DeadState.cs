using UnityEngine;

namespace CyberpunkRunner.Player
{
    /// <summary>
    /// Represents the Dead state when colliding with an obstacle.
    /// Freezes movement and disables input handling.
    /// </summary>
    public class DeadState : IPlayerState
    {
        public void Enter(PlayerController player)
        {
            Debug.Log("<color=red>[State Pattern]</color> Player entered DeadState! Game Over triggered.");
            player.StopMovement();
        }

        public void Update(PlayerController player)
        {
            // Intentionally empty: player cannot move or take input while dead
        }

        public void Exit(PlayerController player)
        {
            // Reset state if restarting or respawning
        }
    }
}

namespace MindRush.Player
{
    /// <summary>
    /// Contract for all player movement states (State Pattern).
    /// Enforces clean separation between running, jumping, sliding, and death.
    /// </summary>
    public interface IPlayerState
    {
        /// <summary>
        /// Called once when entering this state (setup/triggers).
        /// </summary>
        void Enter(PlayerController player);

        /// <summary>
        /// Called every frame while in this state (handles state-specific input and logic).
        /// </summary>
        void Update(PlayerController player);

        /// <summary>
        /// Called once when exiting this state (cleanup).
        /// </summary>
        void Exit(PlayerController player);
    }
}

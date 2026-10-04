using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CyberpunkRunner.Player
{
    /// <summary>
    /// Represents the default Running state of the player.
    /// Handles left/right lane changes and transitions to Jump/Slide.
    /// </summary>
    public class RunningState : IPlayerState
    {
        public void Enter(PlayerController player)
        {
            // Reset state-specific flags or triggers when entering Run state
        }

        public void Update(PlayerController player)
        {
            HandleInput(player);
        }

        public void Exit(PlayerController player)
        {
            // Cleanup state when leaving Run state
        }

        private void HandleInput(PlayerController player)
        {
            bool moveLeft = false;
            bool moveRight = false;
            bool jump = false;
            bool slide = false;

            // Check Unity New Input System
            #if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                moveLeft = Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame;
                moveRight = Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame;
                jump = Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame;
                slide = Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame;
            }
            #endif

            // Fallback for legacy input if enabled
            if (!moveLeft && !moveRight && !jump && !slide)
            {
                moveLeft = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
                moveRight = Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
                jump = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Space);
                slide = Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
            }

            // Lane changes: Left (-1), Right (+1)
            if (moveLeft)
            {
                player.ChangeLane(-1);
            }
            else if (moveRight)
            {
                player.ChangeLane(1);
            }

            // Jump trigger (only when grounded)
            if (jump && player.IsGrounded())
            {
                player.Jump();
            }
            // Slide trigger (only when grounded)
            else if (slide && player.IsGrounded())
            {
                player.TransitionToState(player.SlideState);
            }
        }
    }
}

using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MindRush.Player
{
    /// <summary>
    /// Represents the Sliding (crouching) state of the player.
    /// Handles temporary collider reduction, duration countdown, and lane switching while sliding.
    /// Follows the State Pattern for clean, decoupled state transitions.
    /// </summary>
    public class SlideState : IPlayerState
    {
        // Duration of the slide in seconds before standing back up
        private readonly float slideDuration = 0.75f;
        private float slideTimer = 0f;

        public void Enter(PlayerController player)
        {
            slideTimer = slideDuration;
            player.StartSlide();
        }

        public void Update(PlayerController player)
        {
            slideTimer -= Time.deltaTime;

            // Allow lane changing even while sliding (responsive arcade control)
            HandleLaneInput(player);

            // Allow canceling slide directly into an immediate jump
            HandleJumpInput(player);

            // Once the slide duration elapses, return cleanly to RunningState
            if (slideTimer <= 0f)
            {
                player.TransitionToState(player.RunningState);
            }
        }

        public void Exit(PlayerController player)
        {
            player.StopSlide();
        }

        private void HandleLaneInput(PlayerController player)
        {
            bool moveLeft = false;
            bool moveRight = false;

            #if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                moveLeft = Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame;
                moveRight = Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame;
            }
            #endif

            if (!moveLeft && !moveRight)
            {
                moveLeft = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
                moveRight = Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
            }

            if (moveLeft) player.ChangeLane(-1);
            else if (moveRight) player.ChangeLane(1);
        }

        private void HandleJumpInput(PlayerController player)
        {
            bool jump = false;

            #if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                jump = Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame;
            }
            #endif

            if (!jump)
            {
                jump = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Space);
            }

            if (jump && player.IsGrounded())
            {
                // Smoothly cancel slide and immediately trigger jump
                player.TransitionToState(player.RunningState);
                player.Jump();
            }
        }
    }
}

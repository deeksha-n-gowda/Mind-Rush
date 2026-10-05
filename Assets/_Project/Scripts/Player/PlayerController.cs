using UnityEngine;

namespace MindRush.Player
{
    /// <summary>
    /// Core Player Controller: Manages state machine, 3-lane movement, forward progression, and physics.
    /// Follows Clean Architecture and OOP principles.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Lane Settings")]
        [Tooltip("Distance between adjacent lanes in Unity units")]
        [SerializeField] private float laneDistance = 3f;

        [Tooltip("Speed of sideways movement when switching lanes")]
        [SerializeField] private float sideSpeed = 15f;

        [Header("Speed Settings")]
        [Tooltip("Initial forward running speed")]
        [SerializeField] private float forwardSpeed = 8f;

        [Tooltip("Maximum forward running speed")]
        [SerializeField] private float maxSpeed = 22f;

        [Tooltip("How much forward speed increases per second")]
        [SerializeField] private float speedIncreaseRate = 0.1f;

        [Header("Jump & Gravity")]
        [Tooltip("Upward velocity applied on jump")]
        [SerializeField] private float jumpForce = 7.5f;

        [Tooltip("Downward gravitational acceleration")]
        [SerializeField] private float gravity = 20f;

        [Header("Slide Settings")]
        [Tooltip("Collider height while sliding (default is 2.0)")]
        [SerializeField] private float slideHeight = 1.0f;

        [Tooltip("Collider center Y while sliding (keeps bottom pinned to ground)")]
        [SerializeField] private float slideCenterY = -0.5f;

        // Current lane: 0 = Left (-3), 1 = Center (0), 2 = Right (+3)
        private int currentLane = 1;

        // Vertical velocity accumulator for jumping and falling
        private float verticalVelocity = 0f;

        // Cached components and original dimensions for sliding
        private CharacterController characterController;
        private float originalHeight;
        private Vector3 originalCenter;
        private Vector3 originalScale;
        private bool isSliding = false;

        // State Machine
        private IPlayerState currentState;
        public readonly RunningState RunningState = new RunningState();
        public readonly SlideState SlideState = new SlideState();
        public readonly DeadState DeadState = new DeadState();

        // Event fired when player hits an obstacle (Observer Pattern for decoupled UI/Audio)
        public event System.Action OnPlayerDied;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            
            // Cache standing proportions so we can restore them precisely after sliding
            originalHeight = characterController.height;
            originalCenter = characterController.center;
            originalScale = transform.localScale;
        }

        private void Start()
        {
            // Initial state is Running
            TransitionToState(RunningState);
        }

        private void Update()
        {
            // Update state logic (input, animations)
            currentState?.Update(this);

            // Only accelerate and move if alive
            if (!(currentState is DeadState))
            {
                if (forwardSpeed < maxSpeed)
                {
                    forwardSpeed += speedIncreaseRate * Time.deltaTime;
                }

                // Perform frame movement
                ApplyMovement();
            }
        }

        /// <summary>
        /// Switches the active state using the State Pattern.
        /// </summary>
        public void TransitionToState(IPlayerState newState)
        {
            currentState?.Exit(this);
            currentState = newState;
            currentState.Enter(this);
        }

        /// <summary>
        /// Changes target lane (-1 for Left, +1 for Right).
        /// Clamped between 0 (Left) and 2 (Right).
        /// </summary>
        public void ChangeLane(int direction)
        {
            int targetLane = currentLane + direction;
            if (targetLane >= 0 && targetLane <= 2)
            {
                currentLane = targetLane;
            }
        }

        /// <summary>
        /// Applies an upward jump impulse if grounded.
        /// If currently sliding, cancels slide immediately to jump.
        /// </summary>
        public void Jump()
        {
            if (characterController.isGrounded)
            {
                if (isSliding)
                {
                    StopSlide();
                }
                verticalVelocity = jumpForce;
            }
        }

        /// <summary>
        /// Checks if the CharacterController is currently touching the ground.
        /// </summary>
        public bool IsGrounded()
        {
            return characterController.isGrounded;
        }

        /// <summary>
        /// Temporarily shrinks CharacterController collider and visual scale for sliding.
        /// </summary>
        public void StartSlide()
        {
            if (isSliding) return;
            isSliding = true;

            characterController.height = slideHeight;
            characterController.center = new Vector3(originalCenter.x, slideCenterY, originalCenter.z);
            transform.localScale = new Vector3(originalScale.x, originalScale.y * 0.5f, originalScale.z);
        }

        /// <summary>
        /// Restores CharacterController collider and visual scale to standing dimensions.
        /// </summary>
        public void StopSlide()
        {
            if (!isSliding) return;
            isSliding = false;

            characterController.height = originalHeight;
            characterController.center = originalCenter;
            transform.localScale = originalScale;
        }

        public bool IsSliding => isSliding;

        /// <summary>
        /// Computes frame movement across X (lane lerp), Y (gravity/jump), and Z (forward dash).
        /// </summary>
        private void ApplyMovement()
        {
            // 1. Calculate Target X position for the selected lane
            // Lane 0 -> -laneDistance (-3)
            // Lane 1 -> 0
            // Lane 2 -> +laneDistance (+3)
            float targetX = (currentLane - 1) * laneDistance;

            // Smoothly interpolate current X position towards target X
            float currentX = transform.position.x;
            float newX = Mathf.MoveTowards(currentX, targetX, sideSpeed * Time.deltaTime);
            float deltaX = newX - currentX;

            // 2. Handle Vertical Gravity
            if (characterController.isGrounded)
            {
                // Small downward force to stay glued to ramps/ground
                if (verticalVelocity < 0f)
                {
                    verticalVelocity = -1f;
                }
            }
            else
            {
                verticalVelocity -= gravity * Time.deltaTime;
            }

            // 3. Assemble full 3D displacement vector
            Vector3 moveVector = new Vector3(
                deltaX,
                verticalVelocity * Time.deltaTime,
                forwardSpeed * Time.deltaTime
            );

            // Execute movement through Unity's CharacterController
            characterController.Move(moveVector);
        }

        /// <summary>
        /// Halts forward and vertical movement when transitioning to DeadState.
        /// </summary>
        public void StopMovement()
        {
            forwardSpeed = 0f;
            verticalVelocity = 0f;
        }

        /// <summary>
        /// Triggers player death, transitions to DeadState, and invokes OnPlayerDied event.
        /// </summary>
        public void Die()
        {
            if (currentState is DeadState) return;
            if (isSliding)
            {
                StopSlide();
            }
            TransitionToState(DeadState);
            OnPlayerDied?.Invoke();
        }
    }
}

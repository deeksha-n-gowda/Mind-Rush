using UnityEngine;
using MindRush.Network;

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

        [Header("Run Tracking")]
        [Tooltip("Starting Z position for distance calculation")]
        [SerializeField] private float startZ = 0f;

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

        // Run statistics
        private float runStartTime;
        private int ideasCollected = 0;
        private int gemsCollected = 0;
        private string causeOfDeath = "unknown";

        // State Machine
        private IPlayerState currentState;
        public readonly RunningState RunningState = new RunningState();
        public readonly SlideState SlideState = new SlideState();
        public readonly DeadState DeadState = new DeadState();

        // Event fired when player hits an obstacle (Observer Pattern for decoupled UI/Audio)
        public event System.Action OnPlayerDied;

        // Properties for external access
        public float DistanceTraveled => transform.position.z - startZ;
        public int IdeasCollected => ideasCollected;
        public int GemsCollected => gemsCollected;
        public float PlaytimeSeconds => Time.time - runStartTime;
        public float CurrentSpeed => forwardSpeed;

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
            runStartTime = Time.time;
            startZ = transform.position.z;
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
        /// Call when player collects an Idea Lightbulb
        /// </summary>
        public void CollectIdea()
        {
            ideasCollected++;
        }

        /// <summary>
        /// Call when player collects a Focus Gem
        /// </summary>
        public void CollectGem()
        {
            gemsCollected++;
        }

        /// <summary>
        /// Sets the cause of death for score submission
        /// </summary>
        public void SetCauseOfDeath(string cause)
        {
            causeOfDeath = cause;
        }

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
        /// Submits score to backend if authenticated.
        /// </summary>
        public void Die()
        {
            if (currentState is DeadState) return;
            if (isSliding)
            {
                StopSlide();
            }
            
            // Calculate final stats before stopping
            int finalScore = CalculateScore();
            int finalDistance = Mathf.RoundToInt(DistanceTraveled);
            int finalPlaytime = Mathf.RoundToInt(PlaytimeSeconds);
            string equippedChar = GetEquippedCharacter();

            TransitionToState(DeadState);
            OnPlayerDied?.Invoke();

            // Submit score to backend (async, fire-and-forget)
            SubmitScoreToBackend(finalScore, finalDistance, finalPlaytime, equippedChar);
        }

        private int CalculateScore()
        {
            // Base score from distance + bonuses
            int distanceScore = Mathf.RoundToInt(DistanceTraveled);
            int ideaBonus = ideasCollected * 50;
            int gemBonus = gemsCollected * 200;
            int speedBonus = Mathf.RoundToInt((forwardSpeed - 8f) * 10f); // Bonus for maintaining high speed
            
            return distanceScore + ideaBonus + gemBonus + speedBonus;
        }

        private string GetEquippedCharacter()
        {
            if (AuthManager.Instance != null && AuthManager.Instance.CurrentPlayer != null)
            {
                return AuthManager.Instance.CurrentPlayer.equipped_character;
            }
            return "aura";
        }

        private async void SubmitScoreToBackend(int score, int distance, int playtime, string characterUsed)
        {
            if (ScoreService.Instance != null && AuthManager.Instance != null && AuthManager.Instance.IsLoggedIn)
            {
                try
                {
                    await ScoreService.Instance.SubmitScore(score, distance, ideasCollected, gemsCollected, 
                        playtime, characterUsed, causeOfDeath, (success, msg) => 
                    {
                        if (success)
                        {
                            Debug.Log($"[PlayerController] Score submitted: {score}");
                            
                            // Trigger Mimi commentary for milestone
                            if (MimiService.Instance != null)
                            {
                                MimiService.Instance.OnMilestoneReached(distance, score, ideasCollected, gemsCollected, characterUsed);
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[PlayerController] Score submission failed: {msg}");
                        }
                    });
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PlayerController] Score submission error: {e.Message}");
                }
            }
        }
    }
}
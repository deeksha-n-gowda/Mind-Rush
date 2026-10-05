using UnityEngine;

namespace CyberpunkRunner.Core
{
    /// <summary>
    /// Smooth third-person camera follower.
    /// Runs in LateUpdate to prevent camera jitter after player physics moves.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Tooltip("The player transform to follow")]
        [SerializeField] private Transform target;

        [Tooltip("Offset distance behind and above the player")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 2.5f, -5f);

        [Tooltip("How smoothly the camera pans horizontally between lanes")]
        [SerializeField] private float smoothSpeed = 10f;

        private void Start()
        {
            // Auto-find player if not assigned in Inspector
            if (target == null)
            {
                var playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                {
                    target = playerObj.transform;
                }
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Follow player's Z (forward) and Y (height) directly, smoothly interpolate X (lane)
            Vector3 desiredPosition = new Vector3(
                Mathf.Lerp(transform.position.x, target.position.x + offset.x, smoothSpeed * Time.deltaTime),
                target.position.y + offset.y,
                target.position.z + offset.z
            );

            transform.position = desiredPosition;
        }
    }
}

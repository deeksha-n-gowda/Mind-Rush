using UnityEngine;
using MindRush.Player;

namespace MindRush.Environment
{
    /// <summary>
    /// Collectible items: Idea Lightbulbs and Focus Gems.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Collectible : MonoBehaviour
    {
        public enum CollectibleType
        {
            Idea,      // Lightbulb - 50 points
            Gem        // Focus Gem - 200 points
        }

        [Header("Collectible Settings")]
        [SerializeField] private CollectibleType type = CollectibleType.Idea;
        [SerializeField] private int value = 1; // For future: multipliers, etc.

        [Header("Visual Feedback")]
        [SerializeField] private ParticleSystem collectParticles;
        [SerializeField] private AudioClip collectSound;
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private float bobHeight = 0.3f;
        [SerializeField] private float bobSpeed = 2f;

        private Vector3 startPosition;
        private Collider col;

        private void Awake()
        {
            col = GetComponent<Collider>();
            col.isTrigger = true;
            startPosition = transform.position;
        }

        private void Start()
        {
            // Auto-set value based on type
            if (type == CollectibleType.Idea) value = 50;
            else if (type == CollectibleType.Gem) value = 200;
        }

        private void Update()
        {
            // Rotate
            transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);

            // Bob up/down
            float y = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(startPosition.x, y, startPosition.z);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                PlayerController player = other.GetComponent<PlayerController>();
                if (player != null)
                {
                    Collect(player);
                }
            }
        }

        private void Collect(PlayerController player)
        {
            // Notify player
            if (type == CollectibleType.Idea)
            {
                player.CollectIdea();
                
                // Mimi commentary occasionally
                if (MimiService.Instance != null && UnityEngine.Random.value < 0.1f)
                {
                    MimiService.Instance.OnMilestoneReached(
                        Mathf.RoundToInt(player.DistanceTraveled), 
                        0, player.IdeasCollected, player.GemsCollected, 
                        player.GetEquippedCharacter?.Invoke() ?? "aura");
                }
            }
            else if (type == CollectibleType.Gem)
            {
                player.CollectGem();
            }

            // Visual/audio feedback
            if (collectParticles != null)
            {
                var ps = Instantiate(collectParticles, transform.position, Quaternion.identity);
                Destroy(ps.gameObject, 2f);
            }

            if (collectSound != null)
            {
                AudioSource.PlayClipAtPoint(collectSound, transform.position);
            }

            // Destroy collectible
            Destroy(gameObject);
        }
    }
}
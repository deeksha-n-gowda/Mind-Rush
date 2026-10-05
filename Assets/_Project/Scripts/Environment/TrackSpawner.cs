using System.Collections.Generic;
using UnityEngine;

namespace MindRush.Environment
{
    /// <summary>
    /// Procedural Track Spawner using Object Pooling (Queue-based recycling).
    /// Recycles track segments dynamically with ZERO runtime garbage collection.
    /// </summary>
    public class TrackSpawner : MonoBehaviour
    {
        [Header("Prefab & Target References")]
        [Tooltip("The modular track segment prefab to pool and spawn")]
        [SerializeField] private GameObject trackPrefab;

        [Tooltip("The player transform used to measure forward progress")]
        [SerializeField] private Transform playerTransform;

        [Header("Pool Configuration")]
        [Tooltip("Number of track segments alive simultaneously")]
        [SerializeField] private int numberOfSegments = 6;

        [Tooltip("Length of each track segment along the Z axis (matches our prefab: 30 units)")]
        [SerializeField] private float segmentLength = 30f;

        // Current Z position where the next recycled segment will be placed
        private float spawnZ = 0f;

        // Queue storing our recycled segments (First-In, First-Out conveyor belt)
        private readonly Queue<GameObject> activeSegments = new Queue<GameObject>();

        private void Start()
        {
            // Auto-detect player if not dragged into the Inspector
            if (playerTransform == null)
            {
                var playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                {
                    playerTransform = playerObj.transform;
                }
            }

            // Pre-warm the pool by spawning the initial runway chunks
            InitializePool();
        }

        private void Update()
        {
            if (playerTransform == null || activeSegments.Count == 0) return;

            // Check if the player has passed beyond the oldest segment
            // When player passes the center of the first segment, recycle it to the front!
            GameObject oldestSegment = activeSegments.Peek();
            if (playerTransform.position.z - segmentLength > oldestSegment.transform.position.z)
            {
                RecycleSegment();
            }
        }

        /// <summary>
        /// Pre-populates the pool at game launch.
        /// Spawns 'numberOfSegments' sequentially along the Z axis.
        /// </summary>
        private void InitializePool()
        {
            for (int i = 0; i < numberOfSegments; i++)
            {
                SpawnInitialSegment();
            }
        }

        private void SpawnInitialSegment()
        {
            Vector3 spawnPosition = new Vector3(0f, 0f, spawnZ);
            GameObject segment = Instantiate(trackPrefab, spawnPosition, Quaternion.identity, transform);
            
            activeSegments.Enqueue(segment);
            spawnZ += segmentLength;
        }

        /// <summary>
        /// Object Pooling in action:
        /// Instead of Destroy() and Instantiate() (which causes memory lag),
        /// we pop the oldest segment from behind the player, teleport it to the front,
        /// and push it back into the queue!
        /// </summary>
        private void RecycleSegment()
        {
            GameObject segmentToRecycle = activeSegments.Dequeue();

            // Teleport to the new front of the track
            segmentToRecycle.transform.position = new Vector3(0f, 0f, spawnZ);

            // Re-enqueue as the newest front segment
            activeSegments.Enqueue(segmentToRecycle);

            // Advance the next spawn marker
            spawnZ += segmentLength;
        }
    }
}

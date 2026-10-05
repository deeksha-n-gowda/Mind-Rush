using System;
using System.Collections;
using UnityEngine;
using MindRush.Network.DTOs;

namespace MindRush.Network
{
    public class MimiService : MonoBehaviour
    {
        private static MimiService _instance;
        public static MimiService Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("MimiService");
                    _instance = go.AddComponent<MimiService>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        public event Action<MimiCommentaryResponse> OnCommentaryReceived;
        public event Action<DailyChallengesResponse> OnDailyChallengesFetched;
        public event Action<int, int> OnChallengeClaimed; // challengeIndex, rewardXP

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void GetCommentary(string eventType, CommentaryContext context, Action<bool, MimiCommentaryResponse> callback = null)
        {
            var request = new MimiCommentaryRequest
            {
                event_type = eventType,
                context = context
            };

            StartCoroutine(GetCommentaryRoutine(request, callback));
        }

        private IEnumerator GetCommentaryRoutine(MimiCommentaryRequest request, Action<bool, MimiCommentaryResponse> callback)
        {
            var response = yield return ApiClient.Instance.Post<MimiCommentaryResponse>(ApiRoutes.MimiCommentary, request);

            if (response.success && response.data != null)
            {
                OnCommentaryReceived?.Invoke(response.data);
                callback?.Invoke(true, response.data);
            }
            else
            {
                // Return a fallback commentary so game doesn't break
                var fallback = new MimiCommentaryResponse
                {
                    text = GetFallbackCommentary(eventType),
                    audio_url = null,
                    event_type = eventType
                };
                OnCommentaryReceived?.Invoke(fallback);
                callback?.Invoke(true, fallback);
            }
        }

        private string GetFallbackCommentary(string eventType)
        {
            return eventType switch
            {
                "milestone" => "Great progress! Keep focusing!",
                "near_miss" => "Close call! Stay sharp!",
                "death" => "Don't worry, every run teaches us something.",
                "unlock" => "New ally unlocked!",
                "daily_complete" => "Daily challenge complete!",
                "level_up" => "Level up! Your mind grows stronger.",
                _ => "Stay focused."
            };
        }

        public void GetDailyChallenges(Action<bool, DailyChallengesResponse> callback = null)
        {
            StartCoroutine(GetDailyChallengesRoutine(callback));
        }

        private IEnumerator GetDailyChallengesRoutine(Action<bool, DailyChallengesResponse> callback)
        {
            var response = yield return ApiClient.Instance.Get<DailyChallengesResponse>(ApiRoutes.MimiDailyChallenges);

            if (response.success && response.data != null)
            {
                OnDailyChallengesFetched?.Invoke(response.data);
                callback?.Invoke(true, response.data);
            }
            else
            {
                callback?.Invoke(false, null);
            }
        }

        public void ClaimDailyChallenge(int challengeIndex, Action<bool, int> callback = null)
        {
            string endpoint = string.Format(ApiRoutes.MimiClaimChallenge, challengeIndex);
            StartCoroutine(ClaimChallengeRoutine(endpoint, challengeIndex, callback));
        }

        private IEnumerator ClaimChallengeRoutine(string endpoint, int challengeIndex, Action<bool, int> callback)
        {
            var response = yield return ApiClient.Instance.Post<ClaimChallengeResponse>(endpoint, new { });

            if (response.success && response.data != null)
            {
                OnChallengeClaimed?.Invoke(challengeIndex, response.data.reward_xp);
                callback?.Invoke(true, response.data.reward_xp);
            }
            else
            {
                callback?.Invoke(false, 0);
            }
        }

        [System.Serializable]
        private class ClaimChallengeResponse
        {
            public string message;
            public int reward_xp;
            public int challenge_index;
        }

        // Convenience methods for common game events
        public void OnMilestoneReached(int distance, int score, int ideas, int gems, string characterName, Action<bool, MimiCommentaryResponse> callback = null)
        {
            GetCommentary("milestone", new CommentaryContext
            {
                distance = distance,
                score = score,
                ideas = ideas,
                gems = gems,
                character_name = characterName
            }, callback);
        }

        public void OnNearMiss(Action<bool, MimiCommentaryResponse> callback = null)
        {
            GetCommentary("near_miss", new CommentaryContext(), callback);
        }

        public void OnPlayerDeath(Action<bool, MimiCommentaryResponse> callback = null)
        {
            GetCommentary("death", new CommentaryContext(), callback);
        }

        public void OnCharacterUnlocked(string characterName, Action<bool, MimiCommentaryResponse> callback = null)
        {
            GetCommentary("unlock", new CommentaryContext
            {
                character_name = characterName
            }, callback);
        }

        public void OnDailyChallengeComplete(int xp, Action<bool, MimiCommentaryResponse> callback = null)
        {
            GetCommentary("daily_complete", new CommentaryContext
            {
                xp = xp
            }, callback);
        }

        public void OnLevelUp(int level, Action<bool, MimiCommentaryResponse> callback = null)
        {
            GetCommentary("level_up", new CommentaryContext
            {
                level = level
            }, callback);
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MindRush.Network.DTOs;

namespace MindRush.Network
{
    public class ScoreService : MonoBehaviour
    {
        private static ScoreService _instance;
        public static ScoreService Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("ScoreService");
                    _instance = go.AddComponent<ScoreService>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        public event Action<ScoreData> OnScoreSubmitted;
        public event Action<ScoreData[]> OnScoresFetched;
        public event Action<ScoreData> OnBestScoreFetched;
        public event Action<LeaderboardEntry[]> OnLeaderboardFetched;

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

        public void SubmitScore(int score, int distance, int ideasCollected, int gemsCollected,
            int playtimeSeconds, string characterUsed, string causeOfDeath, Action<bool, string> callback = null)
        {
            var request = new SubmitScoreRequest
            {
                score = score,
                distance = distance,
                ideas_collected = ideasCollected,
                gems_collected = gemsCollected,
                playtime_seconds = playtimeSeconds,
                character_used = characterUsed,
                cause_of_death = causeOfDeath
            };

            StartCoroutine(SubmitScoreRoutine(request, callback));
        }

        private IEnumerator SubmitScoreRoutine(SubmitScoreRequest request, Action<bool, string> callback)
        {
            var response = yield return ApiClient.Instance.Post<ScoreSubmitResponse>(ApiRoutes.SubmitScore, request);

            if (response.success && response.data != null)
            {
                OnScoreSubmitted?.Invoke(response.data.score);
                callback?.Invoke(true, "Score submitted");
            }
            else
            {
                callback?.Invoke(false, response.error ?? "Failed to submit score");
            }
        }

        [System.Serializable]
        private class ScoreSubmitResponse
        {
            public ScoreData score;
        }

        public void GetMyScores(int limit = 20, int skip = 0, Action<bool, ScoreData[]> callback = null)
        {
            string endpoint = $"{ApiRoutes.MyScores}?limit={limit}&skip={skip}";
            StartCoroutine(GetMyScoresRoutine(endpoint, callback));
        }

        private IEnumerator GetMyScoresRoutine(string endpoint, Action<bool, ScoreData[]> callback)
        {
            var response = yield return ApiClient.Instance.Get<ScoresListResponse>(endpoint);

            if (response.success && response.data != null)
            {
                OnScoresFetched?.Invoke(response.data.scores);
                callback?.Invoke(true, response.data.scores);
            }
            else
            {
                callback?.Invoke(false, null);
            }
        }

        [System.Serializable]
        private class ScoresListResponse
        {
            public ScoreData[] scores;
        }

        public void GetBestScore(Action<bool, ScoreData> callback = null)
        {
            StartCoroutine(GetBestScoreRoutine(callback));
        }

        private IEnumerator GetBestScoreRoutine(Action<bool, ScoreData> callback)
        {
            var response = yield return ApiClient.Instance.Get<BestScoreResponse>(ApiRoutes.BestScore);

            if (response.success && response.data != null)
            {
                OnBestScoreFetched?.Invoke(response.data.score);
                callback?.Invoke(true, response.data.score);
            }
            else
            {
                callback?.Invoke(false, null);
            }
        }

        [System.Serializable]
        private class BestScoreResponse
        {
            public ScoreData score;
        }

        public void GetLeaderboard(int limit = 50, int skip = 0, Action<bool, LeaderboardEntry[]> callback = null)
        {
            string endpoint = $"{ApiRoutes.Leaderboard}?limit={limit}&skip={skip}";
            StartCoroutine(GetLeaderboardRoutine(endpoint, callback));
        }

        private IEnumerator GetLeaderboardRoutine(string endpoint, Action<bool, LeaderboardEntry[]> callback)
        {
            var response = yield return ApiClient.Instance.Get<LeaderboardResponse>(endpoint);

            if (response.success && response.data != null)
            {
                OnLeaderboardFetched?.Invoke(response.data.leaderboard);
                callback?.Invoke(true, response.data.leaderboard);
            }
            else
            {
                callback?.Invoke(false, null);
            }
        }

        public void GetDailyLeaderboard(int limit = 20, Action<bool, LeaderboardEntry[]> callback = null)
        {
            string endpoint = $"{ApiRoutes.DailyLeaderboard}?limit={limit}";
            StartCoroutine(GetLeaderboardRoutine(endpoint, callback));
        }

        public void GetCharacterLeaderboard(string characterId, int limit = 20, int skip = 0, Action<bool, LeaderboardEntry[]> callback = null)
        {
            string endpoint = string.Format(ApiRoutes.CharacterLeaderboard, characterId) + $"?limit={limit}&skip={skip}";
            StartCoroutine(GetLeaderboardRoutine(endpoint, callback));
        }
    }
}
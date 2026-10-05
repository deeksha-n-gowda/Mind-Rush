namespace MindRush.Network.DTOs
{
    [System.Serializable]
    public class SubmitScoreRequest
    {
        public int score;
        public int distance;
        public int ideas_collected;
        public int gems_collected;
        public int playtime_seconds;
        public string character_used;
        public string cause_of_death;
    }

    [System.Serializable]
    public class ScoreData
    {
        public string id;
        public string player_id;
        public int score;
        public int distance;
        public int ideas_collected;
        public int gems_collected;
        public int playtime_seconds;
        public string character_used;
        public string cause_of_death;
        public string created_at;
    }

    [System.Serializable]
    public class LeaderboardEntry
    {
        public int rank;
        public string username;
        public int score;
        public int distance;
        public int ideas_collected;
        public int gems_collected;
        public string character_used;
        public string equipped_character;
        public string created_at;
    }

    [System.Serializable]
    public class LeaderboardResponse
    {
        public LeaderboardEntry[] leaderboard;
    }
}
namespace MindRush.Network.DTOs
{
    [System.Serializable]
    public class RegisterRequest
    {
        public string username;
        public string email;
        public string password;
    }

    [System.Serializable]
    public class LoginRequest
    {
        public string username;
        public string password;
    }

    [System.Serializable]
    public class AuthResponse
    {
        public string message;
        public string token;
        public PlayerData player;
    }

    [System.Serializable]
    public class PlayerData
    {
        public string id;
        public string username;
        public string email;
        public int level;
        public int experience;
        public int total_ideas_collected;
        public int total_gems_collected;
        public int high_score;
        public string[] unlocked_characters;
        public string equipped_character;
        public int daily_streak;
        public StatisticsData statistics;
    }

    [System.Serializable]
    public class StatisticsData
    {
        public int total_runs;
        public int total_distance;
        public int total_playtime_seconds;
        public DeathStats deaths_by_obstacle;
    }

    [System.Serializable]
    public class DeathStats
    {
        // Dynamic keys - parsed as dictionary
    }
}
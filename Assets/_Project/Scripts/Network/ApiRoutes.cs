namespace MindRush.Network
{
    public static class ApiRoutes
    {
        // Base
        public const string BaseUrl = "http://localhost:5000"; // Change for production
        public const string Health = "/health";

        // Auth
        public const string Register = "/api/auth/register";
        public const string Login = "/api/auth/login";
        public const string Me = "/api/auth/me";
        public const string Refresh = "/api/auth/refresh";

        // Scores
        public const string SubmitScore = "/api/scores";
        public const string MyScores = "/api/scores/my";
        public const string BestScore = "/api/scores/best";

        // Characters
        public const string Characters = "/api/characters";
        public const string CharacterById = "/api/characters/{0}";
        public const string UnlockCharacter = "/api/characters/{0}/unlock";
        public const string EquipCharacter = "/api/characters/{0}/equip";
        public const string CharacterPerk = "/api/characters/perk/{0}";

        // Leaderboard
        public const string Leaderboard = "/api/leaderboard";
        public const string DailyLeaderboard = "/api/leaderboard/daily";
        public const string CharacterLeaderboard = "/api/leaderboard/character/{0}";

        // Mimi (AI Voice Companion)
        public const string MimiCommentary = "/api/mimi/commentary";
        public const string MimiPersonality = "/api/mimi/personality";
        public const string MimiDailyChallenges = "/api/mimi/daily-challenges";
        public const string MimiClaimChallenge = "/api/mimi/daily-challenges/{0}/claim";

        public static string Format(string route, params object[] args)
        {
            return string.Format(route, args);
        }
    }
}
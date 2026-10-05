namespace MindRush.Network.DTOs
{
    [System.Serializable]
    public class MimiCommentaryRequest
    {
        public string event_type;
        public CommentaryContext context;
    }

    [System.Serializable]
    public class CommentaryContext
    {
        public int distance;
        public int score;
        public int ideas;
        public int gems;
        public string character_name;
        public int xp;
        public int level;
    }

    [System.Serializable]
    public class MimiCommentaryResponse
    {
        public string text;
        public string audio_url;
        public string event_type;
    }

    [System.Serializable]
    public class DailyChallengeData
    {
        public int index;
        public string type;
        public string description;
        public int target;
        public int current;
        public bool completed;
        public bool claimed;
        public int reward_xp;
    }

    [System.Serializable]
    public class DailyChallengesResponse
    {
        public string date;
        public DailyChallengeData[] challenges;
    }
}
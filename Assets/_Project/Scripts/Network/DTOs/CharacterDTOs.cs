namespace MindRush.Network.DTOs
{
    [System.Serializable]
    public class CharacterData
    {
        public string id;
        public string name;
        public string description;
        public string rarity;
        public PerkData perk;
        public UnlockRequirement unlock_requirement;
        public VisualData visual;
        public bool unlocked;
        public bool can_unlock;
        public string unlock_reason;
    }

    [System.Serializable]
    public class PerkData
    {
        public string type;
        public float value;
        public string description;
    }

    [System.Serializable]
    public class UnlockRequirement
    {
        public string type;
        public int value;
        public string description;
    }

    [System.Serializable]
    public class VisualData
    {
        public string model;
        public string color;
        public string trail_effect;
    }

    [System.Serializable]
    public class CharactersResponse
    {
        public CharacterData[] characters;
    }
}
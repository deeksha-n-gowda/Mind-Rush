using System;
using System.Collections;
using UnityEngine;
using MindRush.Network.DTOs;

namespace MindRush.Network
{
    public class CharacterService : MonoBehaviour
    {
        private static CharacterService _instance;
        public static CharacterService Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("CharacterService");
                    _instance = go.AddComponent<CharacterService>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        public event Action<CharacterData[]> OnCharactersFetched;
        public event Action<CharacterData> OnCharacterUnlocked;
        public event Action<string> OnCharacterEquipped;
        public event Action<PerkData> OnPerkFetched;

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

        public void GetAllCharacters(Action<bool, CharacterData[]> callback = null)
        {
            StartCoroutine(GetAllCharactersRoutine(callback));
        }

        private IEnumerator GetAllCharactersRoutine(Action<bool, CharacterData[]> callback)
        {
            var response = yield return ApiClient.Instance.Get<CharactersResponse>(ApiRoutes.Characters);

            if (response.success && response.data != null)
            {
                OnCharactersFetched?.Invoke(response.data.characters);
                callback?.Invoke(true, response.data.characters);
            }
            else
            {
                callback?.Invoke(false, null);
            }
        }

        public void UnlockCharacter(string characterId, Action<bool, string> callback = null)
        {
            string endpoint = string.Format(ApiRoutes.UnlockCharacter, characterId);
            StartCoroutine(UnlockCharacterRoutine(endpoint, characterId, callback));
        }

        private IEnumerator UnlockCharacterRoutine(string endpoint, string characterId, Action<bool, string> callback)
        {
            var response = yield return ApiClient.Instance.Post<UnlockCharacterResponse>(endpoint, new { });

            if (response.success)
            {
                // Refetch characters to get updated unlock status
                yield return GetAllCharactersRoutine((success, chars) => {
                    if (success)
                    {
                        var unlocked = Array.Find(chars, c => c.id == characterId);
                        if (unlocked != null)
                            OnCharacterUnlocked?.Invoke(unlocked);
                    }
                    callback?.Invoke(success, success ? "Character unlocked!" : "Failed to unlock");
                });
            }
            else
            {
                callback?.Invoke(false, response.error ?? "Failed to unlock character");
            }
        }

        [System.Serializable]
        private class UnlockCharacterResponse
        {
            public string message;
            public string character_id;
        }

        public void EquipCharacter(string characterId, Action<bool, string> callback = null)
        {
            string endpoint = string.Format(ApiRoutes.EquipCharacter, characterId);
            StartCoroutine(EquipCharacterRoutine(endpoint, characterId, callback));
        }

        private IEnumerator EquipCharacterRoutine(string endpoint, string characterId, Action<bool, string> callback)
        {
            var response = yield return ApiClient.Instance.Post<EquipCharacterResponse>(endpoint, new { });

            if (response.success)
            {
                // Update local player data
                if (AuthManager.Instance.CurrentPlayer != null)
                {
                    AuthManager.Instance.CurrentPlayer.equipped_character = characterId;
                    AuthManager.Instance.UpdateLocalPlayerData(AuthManager.Instance.CurrentPlayer);
                }
                OnCharacterEquipped?.Invoke(characterId);
                callback?.Invoke(true, "Character equipped");
            }
            else
            {
                callback?.Invoke(false, response.error ?? "Failed to equip character");
            }
        }

        [System.Serializable]
        private class EquipCharacterResponse
        {
            public string message;
            public string character_id;
        }

        public void GetCharacterPerk(string characterId, Action<bool, PerkData> callback = null)
        {
            string endpoint = string.Format(ApiRoutes.CharacterPerk, characterId);
            StartCoroutine(GetPerkRoutine(endpoint, callback));
        }

        private IEnumerator GetPerkRoutine(string endpoint, Action<bool, PerkData> callback)
        {
            var response = yield return ApiClient.Instance.Get<PerkResponse>(endpoint);

            if (response.success && response.data != null)
            {
                OnPerkFetched?.Invoke(response.data.perk);
                callback?.Invoke(true, response.data.perk);
            }
            else
            {
                callback?.Invoke(false, null);
            }
        }

        [System.Serializable]
        private class PerkResponse
        {
            public PerkData perk;
        }

        public float ApplyPerkToValue(string characterId, float baseValue, string perkType)
        {
            // This would ideally fetch from server, but for performance we can cache
            // For now, return base value - actual perk application happens server-side
            // or you'd cache the perk data locally after fetching
            return baseValue;
        }
    }
}
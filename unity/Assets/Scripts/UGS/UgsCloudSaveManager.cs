// Battle of the Brooms: player profile in Unity Cloud Save (one JSON entry under "player_profile").
// Every write sends the whole profile, so a failed write can never half-apply a change.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using UnityEngine;

namespace Bob.Ugs
{
    public class UgsCloudSaveManager : MonoBehaviour
    {
        public const string ProfileKey = "player_profile";

        public static UgsCloudSaveManager Instance { get; private set; }

        public PlayerProfileData Profile { get; private set; } = new PlayerProfileData();
        public bool IsLoaded { get; private set; }
        public string LastError { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Reads the profile from Cloud Save; a missing entry simply means a fresh player.</summary>
        public async Task<PlayerProfileData> LoadProfileAsync()
        {
            try
            {
                var keys = new HashSet<string> { ProfileKey };
                Dictionary<string, Item> data = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);
                if (data.TryGetValue(ProfileKey, out Item item))
                {
                    string json = item.Value.GetAs<string>();
                    Profile = string.IsNullOrEmpty(json) ? new PlayerProfileData() : JsonUtility.FromJson<PlayerProfileData>(json);
                }
                else
                {
                    Profile = new PlayerProfileData();
                    Debug.Log("[UGS] no cloud profile yet · starting a new one");
                }

                Profile.unlockedItems ??= new List<string>();
                IsLoaded = true;
                LastError = null;
                Debug.Log($"[UGS] profile loaded · coins {Profile.coins} · level {Profile.level} · items {Profile.unlockedItems.Count}");
            }
            catch (CloudSaveException e)
            {
                LastError = e.Message;
                Debug.LogError("[UGS] cloud save load failed: " + e);
            }
            catch (Exception e)
            {
                LastError = e.Message;
                Debug.LogError("[UGS] cloud save load failed: " + e);
            }

            return Profile;
        }

        /// <summary>Writes the current profile back to Cloud Save.</summary>
        public async Task<bool> SaveProfileAsync()
        {
            try
            {
                var payload = new Dictionary<string, object> { { ProfileKey, JsonUtility.ToJson(Profile) } };
                await CloudSaveService.Instance.Data.Player.SaveAsync(payload);
                LastError = null;
                Debug.Log($"[UGS] profile saved · coins {Profile.coins} · level {Profile.level} · items {Profile.unlockedItems.Count}");
                return true;
            }
            catch (CloudSaveException e)
            {
                LastError = e.Message;
                Debug.LogError("[UGS] cloud save write failed: " + e);
            }
            catch (Exception e)
            {
                LastError = e.Message;
                Debug.LogError("[UGS] cloud save write failed: " + e);
            }

            return false;
        }

        public async Task<bool> AddCoinsAndSaveAsync(int amount)
        {
            Profile.coins += amount;
            return await SaveProfileAsync();
        }

        public async Task<bool> UnlockItemAndSaveAsync(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId)) return false;
            Profile.unlockedItems ??= new List<string>();
            if (!Profile.unlockedItems.Contains(itemId)) Profile.unlockedItems.Add(itemId);
            return await SaveProfileAsync();
        }
    }
}

// Battle of the Brooms: Unity Services bootstrap — anonymous sign-in, then the cloud profile.
// Relay/Lobby/Leaderboards calls all assume this has run; EnsureReadyAsync() is safe to call again.
using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace Bob.Ugs
{
    public class UgsBootstrapManager : MonoBehaviour
    {
        public static UgsBootstrapManager Instance { get; private set; }

        [Tooltip("Sign in as soon as the game starts, then load the cloud profile.")]
        public bool signInOnAwake = true;

        public bool IsReady { get; private set; }
        public string PlayerId => AuthenticationService.Instance.IsSignedIn ? AuthenticationService.Instance.PlayerId : null;
        public string LastError { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
        }

        async void Start()
        {
            if (signInOnAwake) await KickoffAsync();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public async Task KickoffAsync()
        {
            await EnsureReadyAsync();
            if (!IsReady) return;

            var save = UgsCloudSaveManager.Instance != null ? UgsCloudSaveManager.Instance : FindAnyObjectByType<UgsCloudSaveManager>();
            if (save != null) await save.LoadProfileAsync();
            else Debug.LogWarning("[UGS] no UgsCloudSaveManager in the scene — profile not loaded");
        }

        /// <summary>Initializes Unity Services once and signs in anonymously if needed.</summary>
        public async Task EnsureReadyAsync()
        {
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                    await UnityServices.InitializeAsync();
                if (!AuthenticationService.Instance.IsSignedIn)
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();

                IsReady = true;
                LastError = null;
                Debug.Log("[UGS] signed in · PlayerId " + AuthenticationService.Instance.PlayerId);
            }
            catch (AuthenticationException e)
            {
                IsReady = false;
                LastError = e.Message;
                Debug.LogError("[UGS] sign-in failed: " + e);
            }
            catch (Exception e)
            {
                IsReady = false;
                LastError = e.Message;
                Debug.LogError("[UGS] services init failed: " + e);
            }
        }
    }
}

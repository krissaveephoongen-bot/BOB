// Battle of the Brooms: Unity Gaming Services bootstrap for the Relay + Lobby multiplayer test.
// Needs the project linked to a Unity Cloud project (Edit > Project Settings > Services) with Relay and
// Lobby enabled; anonymous sign-in fails without it. See art/docs/27_UGS_Multiplayer.md.
using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace Bob.Networking
{
    /// <summary>Initializes UGS once per session and signs in anonymously; every lobby action awaits this first.</summary>
    public class UgsBootstrapManager : MonoBehaviour
    {
        public static UgsBootstrapManager Instance { get; private set; }

        [Tooltip("Sign in as soon as the object loads. Off = wait for the first lobby action.")]
        public bool signInOnAwake = true;

        public bool IsReady =>
            UnityServices.State == ServicesInitializationState.Initialized &&
            AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn;

        public string PlayerId => IsReady ? AuthenticationService.Instance.PlayerId : null;
        public string LastError { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (signInOnAwake) _ = KickoffAsync();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        async Task KickoffAsync()
        {
            try { await EnsureReadyAsync(); }
            catch (Exception e)
            {
                LastError = e.Message;
                Debug.LogError("[UGS] sign-in failed: " + e);
            }
        }

        /// <summary>Idempotent: safe to call from every button. Throws when services or auth cannot start.</summary>
        public async Task EnsureReadyAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            LastError = null;
            Debug.Log("[UGS] signed in · PlayerId " + AuthenticationService.Instance.PlayerId);
        }
    }
}

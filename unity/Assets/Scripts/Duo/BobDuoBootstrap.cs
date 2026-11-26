// Battle of the Brooms: the DuoClean scene is one canvas plus this object. Everything else is built at runtime so the
// scene stays a one-screen file that never conflicts with the island scenes.
using UnityEngine;

namespace Bob.Duo
{
    public sealed class BobDuoBootstrap : MonoBehaviour
    {
        void Awake()
        {
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) { Debug.LogError("[Duo] DuoClean has no Canvas"); return; }
            var match = gameObject.AddComponent<BobDuoMatch>();
            var hud = gameObject.AddComponent<BobDuoHud>();
            hud.Build(canvas, match);
            match.Bind(hud);
            if (PlayerPrefs.GetInt("bob.duo.devcheck", 0) == 1)
            {
                PlayerPrefs.SetInt("bob.duo.devcheck", 0);
                PlayerPrefs.Save();
                match.BeginSoloSmoke();
                gameObject.AddComponent<BobDuoDevCheck>().Arm(match, hud);
            }
            Debug.Log("[Duo] slice booted · landscape " + Screen.width + "x" + Screen.height);
        }
    }
}

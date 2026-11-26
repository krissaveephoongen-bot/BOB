// Battle of the Brooms: the screen between the lobby and a match. Pressing play used to call SceneManager.LoadScene
// directly, so the phone showed nothing while Unity built the whole 3D scene inside one frame - and that frame is where
// the vivo X200 process disappears (docs 24 §15.41). This overlay is on a persistent object so it survives the load,
// loads asynchronously with real progress, and releases the menu video a frame before the load starts.
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bob.Game
{
    public class BobSceneLoader : MonoBehaviour
    {
        public static bool Busy => live != null;

        static BobSceneLoader live;

        Image fill;
        TMP_Text caption;
        string title;

        /// <summary>Open a scene behind the loading screen. A second call while a load runs is dropped, not queued.</summary>
        public static void Load(string scene, string heading)
        {
            if (Busy) { Debug.LogWarning("[BobLoader] already loading: " + heading); return; }
            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                Debug.LogWarning("[BobLoader] " + scene + " is not in Build Settings: nothing to load.");
                return;
            }
            // A MediaCodec surface torn down inside the load frame is one of the few native faults a scene load can cause,
            // so the video layer is stopped while the old scene is still intact.
            MenuBackgroundVideo.StopAll();

            var go = new GameObject("BobSceneLoader", typeof(Canvas), typeof(CanvasScaler));
            DontDestroyOnLoad(go);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90; // above the HUD (10) and every card overlay
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f; // landscape build: keep heights constant

            live = go.AddComponent<BobSceneLoader>();
            live.title = heading;
            live.Build(go.GetComponent<RectTransform>());
            live.StartCoroutine(live.Run(scene));
        }

        void Build(RectTransform canvasRt)
        {
            var back = canvasRt.gameObject.AddComponent<Image>();
            back.color = BobUi.Bg;
            back.raycastTarget = false;

            var mark = BobUi.Rect("Title", canvasRt);
            BobUi.Pin(mark, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 90), new Vector2(1400, 90));
            caption = BobUi.Label(mark, title, 52, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            var bar = BobUi.Rect("Bar", canvasRt);
            BobUi.Pin(bar, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(760, 26));
            bar.gameObject.AddComponent<Image>().color = BobUi.Line;
            var r = BobUi.Rect("Fill", bar);
            r.anchorMin = Vector2.zero; r.anchorMax = new Vector2(0f, 1f);
            r.offsetMin = r.offsetMax = Vector2.zero;
            fill = r.gameObject.AddComponent<Image>();
            fill.color = BobUi.Teal;
            fill.raycastTarget = false;

            var note = BobUi.Rect("Note", canvasRt);
            BobUi.Pin(note, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -90), new Vector2(1400, 60));
            BobUi.Label(note, "อย่าปิดแอปขณะโหลดฉาก", 30, BobUi.Muted, TextAnchor.MiddleCenter);
        }

        IEnumerator Run(string scene)
        {
            // The overlay covers the whole screen, so it must come down even if the load throws: a stuck loader would
            // look like a new kind of freeze and hide the crash this screen exists to measure.
            try
            {
                yield return null; // one presented frame, so the screen is on screen before the load begins
                Debug.Log("[BobLoader] loading scene " + scene + " async");
                var op = SceneManager.LoadSceneAsync(scene);
                op.allowSceneActivation = false; // holds the load at 0.9 until the bar has been read as progress
                while (op.progress < 0.9f) { Show(op.progress / 0.9f); yield return null; }
                Show(1f);
                yield return new WaitForSecondsRealtime(0.3f);
                op.allowSceneActivation = true;
                yield return op;
                yield return null; // let the new scene present before the overlay goes away
                Debug.Log("[BobLoader] " + scene + " loaded");
            }
            finally
            {
                if (live == this) live = null;
                Destroy(gameObject);
            }
        }

        void Show(float t)
        {
            t = Mathf.Clamp01(t);
            if (fill != null) fill.rectTransform.anchorMax = new Vector2(t, 1f);
            if (caption != null) caption.text = title + "  " + Mathf.RoundToInt(t * 100f) + "%";
        }
    }
}

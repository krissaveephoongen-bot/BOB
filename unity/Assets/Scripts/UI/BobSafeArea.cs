// Battle of the Brooms: keeps a RectTransform inside Screen.safeArea plus a 24u inner margin (UX spec 1.3).
// Put it on a full-screen child named "SafeArea" under each Canvas; buttons and HUD go inside it,
// backgrounds and modal dims stay outside so they still cover the notch.
using UnityEngine;

namespace Bob.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class BobSafeArea : MonoBehaviour
    {
        [Tooltip("Extra inner margin in canvas units (spec: 24u)")] public float inset = 24f;
        Rect last; Vector2Int lastSize;

        void OnEnable() => Apply();
        void Update() { if (Screen.safeArea != last || lastSize.x != Screen.width || lastSize.y != Screen.height) Apply(); }

        void Apply()
        {
            last = Screen.safeArea; lastSize = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;
            var rt = (RectTransform)transform;
            Vector2 min = last.position, max = last.position + last.size;
            rt.anchorMin = new Vector2(min.x / Screen.width, min.y / Screen.height);
            rt.anchorMax = new Vector2(max.x / Screen.width, max.y / Screen.height);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset); // offsets are canvas units: the CanvasScaler applies the screen scale
        }

        /// <summary>Adds a full-screen SafeArea child under a canvas and returns it.</summary>
        public static RectTransform Create(Transform canvas, float inset = 24f)
        {
            var go = new GameObject("SafeArea", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            go.AddComponent<BobSafeArea>().inset = inset;
            return rt;
        }
    }
}

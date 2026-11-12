// Battle of the Brooms: grows a Graphic's touch area to the spec minimum without growing the picture (UX spec 1.2):
// 144u for menus, 160u in a match. Uses Image/Text raycastPadding (negative padding = bigger hit area).
using UnityEngine;
using UnityEngine.UI;

namespace Bob.UI
{
    public static class BobHitArea
    {
        /// <summary>Make the hit area of `g` at least `min` x `min` canvas units, centred on it.</summary>
        public static void Ensure(Graphic g, float min = 144f)
        {
            if (g == null) return;
            var size = g.rectTransform.rect.size;
            if (size == Vector2.zero) size = g.rectTransform.sizeDelta;
            float px = Mathf.Max(0, (min - size.x) / 2), py = Mathf.Max(0, (min - size.y) / 2);
            g.raycastPadding = new Vector4(-px, -py, -px, -py); // left, bottom, right, top
            g.raycastTarget = true;
        }

        /// <summary>Ensure() for every Button under `root` (call after layout, e.g. at the end of building a screen).</summary>
        public static void EnsureAll(Transform root, float min = 144f)
        {
            Canvas.ForceUpdateCanvases();
            foreach (var b in root.GetComponentsInChildren<Button>(true)) Ensure(b.targetGraphic, min);
        }
    }
}

// Battle of the Brooms: code tweens for "game feel" without a tween package.
// Call from any MonoBehaviour, for example:
//   StartCoroutine(BobTween.Pop(icon.transform));            part picked up / button pressed
//   StartCoroutine(BobTween.Squash(layer.transform));        layer dropped onto the cart
//   StartCoroutine(BobTween.Shake(cart.transform, 0.08f));   bad reaction
//   StartCoroutine(BobTween.FloatUp(label.transform, 0.6f)); "+3 สะอาด" text rising
//   StartCoroutine(BobTween.MoveArc(part.transform, target, 0.35f)); part thrown into the cart
// Swap for DOTween or LeanTween later if the team prefers; the timings here are the starting point.
using System.Collections;
using UnityEngine;

namespace Bob.Art
{
    public static class BobTween
    {
        public static float EaseOutBack(float x) { const float c1 = 1.70158f, c3 = c1 + 1; return 1 + c3 * Mathf.Pow(x - 1, 3) + c1 * Mathf.Pow(x - 1, 2); }
        public static float EaseOutCubic(float x) => 1 - Mathf.Pow(1 - x, 3);
        public static float EaseOutElastic(float x)
        {
            const float c4 = 2 * Mathf.PI / 3;
            return x <= 0 ? 0 : x >= 1 ? 1 : Mathf.Pow(2, -10 * x) * Mathf.Sin((x * 10 - 0.75f) * c4) + 1;
        }

        public static IEnumerator Pop(Transform t, float dur = 0.25f, float peak = 1.25f)
        {
            var s0 = t.localScale;
            for (float e = 0; e < dur; e += Time.deltaTime)
            {
                float k = e / dur;
                float s = k < 0.4f ? Mathf.Lerp(1, peak, EaseOutCubic(k / 0.4f)) : Mathf.Lerp(peak, 1, EaseOutBack((k - 0.4f) / 0.6f));
                t.localScale = s0 * s; yield return null;
            }
            t.localScale = s0;
        }

        public static IEnumerator Squash(Transform t, float dur = 0.3f, float amount = 0.3f)
        {
            var s0 = t.localScale;
            for (float e = 0; e < dur; e += Time.deltaTime)
            {
                float k = e / dur, w = (1 - EaseOutElastic(k)) * amount;
                t.localScale = new Vector3(s0.x * (1 + w), s0.y * (1 - w), s0.z); yield return null;
            }
            t.localScale = s0;
        }

        public static IEnumerator Shake(Transform t, float strength = 0.08f, float dur = 0.3f)
        {
            var p0 = t.localPosition;
            for (float e = 0; e < dur; e += Time.deltaTime)
            {
                float k = 1 - e / dur;
                t.localPosition = p0 + (Vector3)(Random.insideUnitCircle * strength * k); yield return null;
            }
            t.localPosition = p0;
        }

        public static IEnumerator FloatUp(Transform t, float height = 0.6f, float dur = 0.8f, bool destroy = true)
        {
            var p0 = t.localPosition;
            var cg = t.GetComponent<CanvasGroup>();
            var sr = t.GetComponent<SpriteRenderer>();
            for (float e = 0; e < dur; e += Time.deltaTime)
            {
                float k = e / dur;
                t.localPosition = p0 + Vector3.up * height * EaseOutCubic(k);
                float a = k < 0.6f ? 1 : 1 - (k - 0.6f) / 0.4f;
                if (cg) cg.alpha = a;
                if (sr) { var c = sr.color; c.a = a; sr.color = c; }
                yield return null;
            }
            if (destroy) Object.Destroy(t.gameObject);
        }

        public static IEnumerator MoveArc(Transform t, Vector3 to, float dur = 0.35f, float arc = 0.8f)
        {
            var from = t.position;
            for (float e = 0; e < dur; e += Time.deltaTime)
            {
                float k = EaseOutCubic(e / dur);
                t.position = Vector3.Lerp(from, to, k) + Vector3.up * arc * 4 * k * (1 - k); yield return null;
            }
            t.position = to;
        }

        /// <summary>Gentle idle bob for props and pickups (run forever; stop the coroutine to end it).</summary>
        public static IEnumerator Bob(Transform t, float amp = 0.05f, float speed = 3f)
        {
            var p0 = t.localPosition;
            for (float e = 0; ; e += Time.deltaTime) { t.localPosition = p0 + Vector3.up * Mathf.Sin(e * speed) * amp; yield return null; }
        }
    }
}

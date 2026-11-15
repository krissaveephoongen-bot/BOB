// Battle of the Brooms: the island breathing (docs 24 section 15.15). One tiny component per prop, animated on the
// CPU with a sine, so the whole nature set costs a few quaternion writes a frame - no vertex shader variant, no
// Animator, no coroutine per prop. The phase comes from the prop's world position, so two bushes next to each other
// never sway in lockstep.
using UnityEngine;

namespace Bob.TaskRace
{
    [DisallowMultipleComponent]
    public class AmbientWindSway : MonoBehaviour
    {
        [Tooltip("Degrees of lean at the peak of the gust.")]
        public float amplitude = 1.6f;
        public float speed = 1.15f;
        [Tooltip("Fraction of the gust that runs perpendicular, so the sway is an oval, not a pendulum.")]
        public float cross = 0.45f;

        float phase;
        Quaternion rest;

        void OnEnable()
        {
            phase = (transform.position.x * 0.7f + transform.position.z * 1.3f) % 6.283f;
            rest = transform.localRotation;
        }

        void Update()
        {
            // Unscaled: the island keeps breathing on a pause or a menu, where Time.time stands still.
            float t = Time.unscaledTime * speed + phase;
            float a = Mathf.Sin(t);
            float b = Mathf.Sin(t * 0.83f + 1.1f) * cross;
            float bx = b * amplitude * 0.6f;
            float bz = a * amplitude;
            transform.localRotation = rest * Quaternion.Euler(bx, 0f, bz);
        }

        void OnDisable() => transform.localRotation = rest;
    }
}

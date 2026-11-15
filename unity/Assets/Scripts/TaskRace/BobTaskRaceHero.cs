// Battle of the Brooms: the local hero on the Task Race map (docs 24). A flat X-Z walker: CharacterController
// movement on the straight play plane, driven by the HUD touch layer (BobTaskRaceTouch) or the keyboard in the editor.
// A tap on the map sets a walk destination; the stick always overrides it. While the S28 card or the result overlay
// is up the body stays put, but the stick still reads so the manager can close the card at 70%.
// The curved shader bends the visuals only; this transform and its collider are the truth.
using System;
using UnityEngine;

namespace Bob.TaskRace
{
    [RequireComponent(typeof(CharacterController))]
    public class BobTaskRaceHero : MonoBehaviour
    {
        [Header("Walk")]
        [Tooltip("Walk speed in world units per second.")] public float speed = 3.2f;
        [Tooltip("How fast the body turns to face the walk direction.")] public float turnSpeed = 12f;
        public float gravity = -20f;

        [Header("Tap to walk")]
        [Tooltip("The hero stops this close to a tapped point.")]
        public float arriveRadius = 0.4f;
        [Tooltip("Distance over which a tap walk eases down, so it never overshoots the point.")]
        public float slowRadius = 0.9f;

        /// <summary>HUD joystick (length 1 = full tilt). Empty = keyboard axes, for desktop testing.</summary>
        public Func<Vector2> input;

        CharacterController controller;
        TaskRaceModeManager manager;
        Vector2 move;
        float fall;
        float stunUntil;
        Vector3 lastSafe;
        Vector3? destination;

        /// <summary>Current stick input as x = right, y = forward (Z). The mode manager reads its magnitude for S28 cancel.</summary>
        public Vector2 InputVector => move;
        public bool IsMoving => move.sqrMagnitude > 0.0001f;
        public bool HasDestination => destination.HasValue;
        /// <summary>The card or the result overlay holds the body; the stick still reads for the cancel gesture.</summary>
        public bool IsFrozen => manager != null && (manager.CardOpen || manager.LastResult.HasValue);
        /// <summary>Stage 3 "fragile": the body stands still for a moment after a card is abandoned.</summary>
        public bool IsStunned => Time.unscaledTime < stunUntil;
        public float StunRemaining => Mathf.Max(0f, stunUntil - Time.unscaledTime);

        /// <summary>Stun the hero for `seconds` (stage 3). The camera keeps tracking; only the walk stops.</summary>
        public void Stun(float seconds)
        {
            if (seconds <= 0f) return;
            stunUntil = Mathf.Max(stunUntil, Time.unscaledTime + seconds);
            destination = null;
        }

        void Awake() => controller = GetComponent<CharacterController>();

        void Start()
        {
            manager = TaskRaceModeManager.Instance;
            lastSafe = transform.position;
        }

        /// <summary>Tap-to-walk: the touch layer hands over the world point under the finger.</summary>
        public void SetDestination(Vector3 worldPoint) => destination = worldPoint;

        public void ClearDestination() => destination = null;

        void Update()
        {
            var v = input != null ? input() : new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            move = Vector2.ClampMagnitude(v, 1f);

            var step = Vector2.zero;
            if (!IsFrozen && !IsStunned)
            {
                if (move.sqrMagnitude > 0.02f) destination = null; // the stick wins over an old tap
                else if (destination.HasValue) move = Steer(destination.Value);
                step = move;
            }

            if (controller.isGrounded && fall < 0f) fall = -2f;
            fall += gravity * Time.deltaTime;

            var delta = new Vector3(step.x, 0f, step.y) * (speed * Time.deltaTime) + Vector3.up * (fall * Time.deltaTime);
            controller.Move(delta);

            if (controller.isGrounded) lastSafe = transform.position;
            else if (transform.position.y < -6f) Respawn(); // walked off the island rim: no void death, back to the last bank

            if (step.sqrMagnitude > 0.0001f)
            {
                var want = Quaternion.LookRotation(new Vector3(step.x, 0f, step.y), Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, want, turnSpeed * Time.deltaTime);
            }
        }

        /// <summary>Direction toward the tapped point, easing off near the end; clears the destination on arrival.</summary>
        Vector2 Steer(Vector3 point)
        {
            var to = point - transform.position;
            to.y = 0f;
            float dist = to.magnitude;
            if (dist <= arriveRadius) { destination = null; return Vector2.zero; }

            var dir = to / dist;
            float ramp = slowRadius > arriveRadius
                ? Mathf.Clamp01((dist - arriveRadius) / (slowRadius - arriveRadius))
                : 1f;
            return new Vector2(dir.x, dir.z) * Mathf.Max(0.2f, ramp);
        }

        /// <summary>Put the body back on the last ground it stood on, facing the same way; the camera follows.</summary>
        void Respawn()
        {
            destination = null;
            fall = -2f;
            controller.enabled = false;
            transform.position = lastSafe + Vector3.up * 0.05f;
            controller.enabled = true;
        }
    }
}

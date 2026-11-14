// Battle of the Brooms: the local hero on the Task Race map (docs 24). A flat X-Z walker for testing the map:
// CharacterController movement on the straight play plane, keyboard or a HUD joystick handed in as a Func.
// The curved shader bends the visuals only; this transform and its collider are the truth.
using System;
using UnityEngine;

namespace Bob.TaskRace
{
    [RequireComponent(typeof(CharacterController))]
    public class BobTaskRaceHero : MonoBehaviour
    {
        [Tooltip("Walk speed in world units per second.")] public float speed = 3.2f;
        [Tooltip("How fast the body turns to face the walk direction.")] public float turnSpeed = 12f;
        public float gravity = -20f;

        /// <summary>HUD joystick (length 1 = full tilt). Empty = keyboard axes, for desktop testing.</summary>
        public Func<Vector2> input;

        CharacterController controller;
        Vector2 move;
        float fall;

        /// <summary>Current input as x = right, y = forward (Z). The mode manager reads its magnitude for S28 cancel.</summary>
        public Vector2 InputVector => move;
        public bool IsMoving => move.sqrMagnitude > 0.0001f;

        void Awake() => controller = GetComponent<CharacterController>();

        void Update()
        {
            var v = input != null ? input() : new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            move = Vector2.ClampMagnitude(v, 1f);

            if (controller.isGrounded && fall < 0f) fall = -2f;
            fall += gravity * Time.deltaTime;

            var step = new Vector3(move.x, 0f, move.y) * (speed * Time.deltaTime) + Vector3.up * (fall * Time.deltaTime);
            controller.Move(step);

            if (IsMoving)
            {
                var want = Quaternion.LookRotation(new Vector3(move.x, 0f, move.y), Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, want, turnSpeed * Time.deltaTime);
            }
        }
    }
}

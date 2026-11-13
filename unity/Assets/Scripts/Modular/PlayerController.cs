// Battle of the Brooms: the hero's movement in room units (x right, y up). Input is a vector (keyboard, floating
// joystick, or none) plus an optional tap-to-walk target; the controller integrates it, keeps the hero inside the
// room and pushes it out of solid rectangles (shelves, sofa, table). It has no idea about the HUD or the rules.
// Position is the only state other scripts need; set it to teleport (spawn point, server correction).
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bob.Modular
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement (room units)")]
        public float speed = 2.3f;
        public float radius = 0.16f;
        [Tooltip("Slowest the analogue stick may walk, as a fraction of full speed")] public float minSpeedFactor = 0.45f;
        [Tooltip("Margin kept to the room walls")] public float wallMargin = 0.18f;
        public Rect room = new Rect(0, 0, 8, 5);

        /// <summary>Solid rectangles to stay out of. Supplied by the game each frame.</summary>
        public Func<IEnumerable<Rect>> Solids;

        public Vector2 Position { get; set; }
        public Vector2 LastMove { get; private set; }
        public bool IsMoving => LastMove != Vector2.zero;
        /// <summary>Tap-to-walk destination (null when walking by stick / keys or standing).</summary>
        public Vector2? Target { get; set; }

        float stuckTime;

        public void ClearTarget() { Target = null; stuckTime = 0; }

        /// <summary>Walk to a point, clamped to the room.</summary>
        public void WalkTo(Vector2 p)
        {
            Target = new Vector2(Mathf.Clamp(p.x, room.xMin + wallMargin, room.xMax - wallMargin), Mathf.Clamp(p.y, room.yMin + wallMargin, room.yMax - wallMargin));
            stuckTime = 0;
        }

        /// <summary>Advance one frame. `input` is a direction (length 0..1); zero means "follow the tap target, if any".</summary>
        public void Step(Vector2 input, float dt)
        {
            Vector2 dir = input; float mag = input.magnitude;
            if (dir == Vector2.zero && Target.HasValue)
            {
                var d = Target.Value - Position;
                if (d.magnitude < 0.05f) { Target = null; }
                else { dir = d.normalized; mag = 1f; }
            }
            if (dir == Vector2.zero) { LastMove = Vector2.zero; return; }

            var old = Position;
            float step = speed * dt * Mathf.Clamp(mag, minSpeedFactor, 1f);
            var p = old + dir.normalized * step;
            p.x = Mathf.Clamp(p.x, room.xMin + wallMargin, room.xMax - wallMargin);
            p.y = Mathf.Clamp(p.y, room.yMin + wallMargin, room.yMax - wallMargin);
            if (Solids != null)
            {
                foreach (var r in Solids())
                {
                    var c = new Vector2(Mathf.Clamp(p.x, r.xMin, r.xMax), Mathf.Clamp(p.y, r.yMin, r.yMax));
                    float dist = Vector2.Distance(p, c);
                    if (dist < radius) p = dist > 0.0001f ? c + (p - c) / dist * radius : old;
                }
            }
            Position = p;
            if (Target.HasValue)
            {
                // blocked on the way (a shelf in the path): give up after 0.4 s instead of pushing forever
                if (Vector2.Distance(p, old) < step * 0.25f) { stuckTime += dt; if (stuckTime > 0.4f) ClearTarget(); }
                else stuckTime = 0;
            }
            LastMove = p - old;
        }
    }
}

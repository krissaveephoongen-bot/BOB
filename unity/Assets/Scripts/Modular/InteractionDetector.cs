// Battle of the Brooms: finds what the hero is standing next to. Keep ONE in the scene; props register themselves
// (or the game registers them). Evaluate(position) once a frame, then read NearestOf(kind), Primary and IsNear(kind).
// Several kinds can be "near" at the same time (a shelf next to the cart), so each kind is tracked on its own and
// Primary picks the one the main button should act on by priority (cart, table, trash, team-mate, shelf).
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bob.Modular
{
    public class InteractionDetector : MonoBehaviour
    {
        static readonly InteractableKind[] Priority =
            { InteractableKind.Cart, InteractableKind.Table, InteractableKind.Trash, InteractableKind.Teammate, InteractableKind.Shelf };

        readonly List<IInteractable> items = new List<IInteractable>();
        readonly Dictionary<InteractableKind, IInteractable> nearest = new Dictionary<InteractableKind, IInteractable>();
        IInteractable primary;

        /// <summary>Raised when the main button's target changes (null = nothing in reach).</summary>
        public event Action<IInteractable> PrimaryChanged;
        /// <summary>Raised when the set of near props changes at all.</summary>
        public event Action NearChanged;

        public IInteractable Primary => primary;
        public int Count => items.Count;

        public void Register(IInteractable it) { if (it != null && !items.Contains(it)) items.Add(it); }
        public void Unregister(IInteractable it) { items.Remove(it); }
        public void Clear() { items.Clear(); nearest.Clear(); primary = null; }

        public bool IsNear(InteractableKind k) => nearest.ContainsKey(k);
        public IInteractable NearestOf(InteractableKind k) => nearest.TryGetValue(k, out var it) ? it : null;
        public string NearestId(InteractableKind k) { var it = NearestOf(k); return it?.Id; }

        public static float DistanceTo(Vector2 p, Rect r)
        {
            var c = new Vector2(Mathf.Clamp(p.x, r.xMin, r.xMax), Mathf.Clamp(p.y, r.yMin, r.yMax));
            return Vector2.Distance(p, c);
        }

        /// <summary>Recompute what is in reach of `position`. Returns true when something changed.</summary>
        public bool Evaluate(Vector2 position)
        {
            var found = new Dictionary<InteractableKind, (IInteractable it, float d)>();
            foreach (var it in items)
            {
                if (it == null) continue;
                float d = DistanceTo(position, it.Bounds);
                if (d >= it.Reach) continue;
                if (!found.TryGetValue(it.Kind, out var cur) || d < cur.d) found[it.Kind] = (it, d);
            }
            bool changed = found.Count != nearest.Count;
            foreach (var kv in found) if (!nearest.TryGetValue(kv.Key, out var old) || !ReferenceEquals(old, kv.Value.it)) changed = true;
            nearest.Clear();
            foreach (var kv in found) nearest[kv.Key] = kv.Value.it;

            IInteractable next = null;
            foreach (var k in Priority) if (nearest.TryGetValue(k, out var it) && it.CanInteract) { next = it; break; }
            bool primaryChanged = !ReferenceEquals(next, primary);
            if (primaryChanged)
            {
                primary?.SetHighlight(false); primary = next; primary?.SetHighlight(true);
                PrimaryChanged?.Invoke(primary);
            }
            if (changed || primaryChanged) NearChanged?.Invoke();
            return changed || primaryChanged;
        }

        /// <summary>The main button: act on the primary target. Returns false when nothing was in reach.</summary>
        public bool InteractPrimary(GameObject actor)
        {
            if (primary == null || !primary.CanInteract) return false;
            primary.Interact(actor);
            return true;
        }
    }
}

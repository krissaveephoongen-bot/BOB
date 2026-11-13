// Battle of the Brooms: a prop in the room the hero can use (shelf, cart, rest table). Put it on the prop's GameObject.
// It is data + hooks: the game supplies the rectangle (a function, because the cart moves) and what Interact does,
// so the same component serves a shelf prefab in the Inspector or a prop built from code.
using System;
using UnityEngine;

namespace Bob.Modular
{
    public class StationProp : MonoBehaviour, IInteractable
    {
        [SerializeField] InteractableKind kind = InteractableKind.Shelf;
        [SerializeField] string id = "";
        [SerializeField, Tooltip("Room units from the footprint's edge")] float reach = 0.46f;
        [SerializeField, Tooltip("Footprint in room units; used when no bounds provider is set")] Rect bounds = new Rect(0, 0, 1, 0.6f);
        [SerializeField, Tooltip("Tint applied to this sprite while it is the target")] SpriteRenderer sprite;
        [SerializeField] Color highlightTint = new Color(1f, 0.93f, 0.6f);
        [SerializeField] string prompt = "ใช้";
        [SerializeField] bool usable = true;

        public Func<Rect> BoundsProvider;      // set by code when the footprint moves
        public Func<bool> CanInteractProvider; // optional: usable only sometimes (table full, hand empty ...)
        public Func<string> PromptProvider;    // optional: prompt that depends on state ("วาง" / "ยก")
        public Action<GameObject> OnInteract;  // what happens on the main button

        Color baseColor = Color.white; bool baseKnown;

        public InteractableKind Kind => kind;
        public string Id => id;
        public Rect Bounds => BoundsProvider != null ? BoundsProvider() : bounds;
        public float Reach => reach;
        public bool CanInteract => CanInteractProvider != null ? CanInteractProvider() : usable;
        public string Prompt => PromptProvider != null ? PromptProvider() : prompt;

        public void Configure(InteractableKind k, string propId, float reachUnits, SpriteRenderer sr)
        {
            kind = k; id = propId; reach = reachUnits; sprite = sr;
        }

        public void SetHighlight(bool on)
        {
            if (sprite == null) return;
            if (!baseKnown) { baseColor = sprite.color; baseKnown = true; }
            sprite.color = on ? highlightTint : baseColor;
        }

        public void Interact(GameObject actor) { if (CanInteract) OnInteract?.Invoke(actor); }

        void OnDrawGizmosSelected()
        {
            var b = Bounds; Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(new Vector3(b.center.x, b.center.y, 0), new Vector3(b.width + reach * 2, b.height + reach * 2, 0.01f));
        }
    }
}

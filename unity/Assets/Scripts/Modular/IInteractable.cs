// Battle of the Brooms: anything the hero can stand next to and act on (a shelf, the cart, the rest table).
// The main button asks the InteractionDetector for the nearest IInteractable and calls Interact(); the detector never
// knows what a shelf or a cart is, so a new prop (trash bin, team-mate hand-off spot) only has to implement this.
using UnityEngine;

namespace Bob.Modular
{
    public enum InteractableKind { Shelf, Cart, Table, Trash, Teammate }

    public interface IInteractable
    {
        InteractableKind Kind { get; }
        /// <summary>Stable id, e.g. "tool" for the tool shelf. Empty for singletons such as the cart.</summary>
        string Id { get; }
        /// <summary>Footprint in room units (the cart moves, so this is read every frame).</summary>
        Rect Bounds { get; }
        /// <summary>How close (room units, measured from the rectangle's edge) the hero must be.</summary>
        float Reach { get; }
        /// <summary>False greys the prop out (it is not usable right now).</summary>
        bool CanInteract { get; }
        /// <summary>The word the main button shows when this is the target, e.g. "วาง".</summary>
        string Prompt { get; }
        void SetHighlight(bool on);
        void Interact(GameObject actor);
    }
}

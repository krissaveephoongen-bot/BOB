// Battle of the Brooms: the only three inputs of the slice (docs 32 section 4). Each one is a small pointer adapter so
// BobDuoHud can build the card and BobDuoMatch can read intent without knowing about uGUI events.
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Bob.Duo
{
    /// <summary>Press and keep pressing. Reads as "Held" so the match samples it every frame.</summary>
    public sealed class BobDuoHoldArea : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public bool Held { get; private set; }
        public void OnPointerDown(PointerEventData e) => Held = true;
        public void OnPointerUp(PointerEventData e) => Held = false;
        public void OnPointerExit(PointerEventData e) => Held = false;

        /// <summary>The card changed task under the finger: a hold must not survive into the next gesture.</summary>
        public void Release() => Held = false;

        /// <summary>Lets the evidence run hold the pad the same way a finger does, so the HUD stays the only writer.</summary>
        public void SetHeld(bool down) => Held = down;
    }

    /// <summary>Tap on the beat. Beat 0.75 s, ±0.2 s window; a late or early tap still counts as a try but adds nothing.</summary>
    public sealed class BobDuoRhythmPad : MonoBehaviour, IPointerDownHandler
    {
        public const float Beat = 0.75f;
        public const float Window = 0.2f;

        public event Action<bool> Tapped; // true = good tap

        public void OnPointerDown(PointerEventData e)
        {
            float p = Mathf.Repeat(Time.time, Beat);
            bool good = p <= Window || p >= Beat - Window;
            Tapped?.Invoke(good);
        }

        /// <summary>The ring's only visual: how far into the beat we are.</summary>
        public static float BeatProgress => Mathf.Repeat(Time.time, Beat) / Beat;
    }

    /// <summary>Drag anywhere on the card; the delta is scaled to the 1920-wide reference so a phone and a tablet owe
    /// the same distance.</summary>
    public sealed class BobDuoDragPad : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        float pending;

        public void OnBeginDrag(PointerEventData e) { }
        public void OnDrag(PointerEventData e) => pending += e.delta.magnitude * (1920f / Mathf.Max(1, Screen.width));

        public float Consume()
        {
            float value = pending;
            pending = 0f;
            return value;
        }
    }
}

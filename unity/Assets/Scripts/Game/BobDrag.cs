// Battle of the Brooms: drag-and-drop for the HUD (GDD sections 9 and 20): drag a part from the hand onto the cart
// to place it, drag the cart's top layer back to the hand to lift it. A short drag still counts as a tap (the button).
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bob.Game
{
    public class BobDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Sprite icon;
        /// <summary>Called with the screen point where the drag ended; return true when it was dropped somewhere useful.</summary>
        public Func<Vector2, bool> onDrop;
        public RectTransform layer; // where the ghost is drawn (the HUD root)

        RectTransform ghost;

        public void OnBeginDrag(PointerEventData e)
        {
            if (layer == null) return;
            ghost = BobUi.Rect("DragGhost", layer);
            var img = ghost.gameObject.AddComponent<Image>(); img.sprite = icon; img.preserveAspect = true; img.raycastTarget = false;
            img.color = new Color(1, 1, 1, 0.9f);
            ghost.sizeDelta = new Vector2(120, 120);
            Move(e);
        }

        public void OnDrag(PointerEventData e) => Move(e);

        public void OnEndDrag(PointerEventData e)
        {
            if (ghost != null) Destroy(ghost.gameObject);
            ghost = null;
            onDrop?.Invoke(e.position);
        }

        void Move(PointerEventData e)
        {
            if (ghost == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, e.position, null, out var lp);
            ghost.anchorMin = ghost.anchorMax = new Vector2(0.5f, 0.5f);
            ghost.anchoredPosition = lp + layer.rect.center * -1f;
        }
    }
}

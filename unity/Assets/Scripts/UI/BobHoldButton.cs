// Battle of the Brooms: press-and-hold confirm for "ส่งทดสอบ" (UX spec BTN-Submit): hold 0.6 s while a ring fills
// around the button; letting go early cancels. No popup, so the room stays visible. Can be turned off in settings
// (PlayerPrefs bob.holdSubmit = 0) and then a tap confirms at once.
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bob.UI
{
    public class BobHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public float holdSeconds = 0.6f;
        /// <summary>A radial-filled Image drawn over or around the button.</summary>
        public Image ring;
        public UnityEvent onConfirm = new UnityEvent();
        public bool interactable = true;

        float held = -1;

        public static bool HoldEnabled => PlayerPrefs.GetInt("bob.holdSubmit", 1) == 1;

        public void OnPointerDown(PointerEventData e)
        {
            if (!interactable) return;
            if (!HoldEnabled) { Confirm(); return; }
            held = 0;
        }
        public void OnPointerUp(PointerEventData e) => Cancel();
        public void OnPointerExit(PointerEventData e) => Cancel();

        void Cancel() { held = -1; if (ring != null) ring.fillAmount = 0; }

        void Update()
        {
            if (held < 0) return;
            held += Time.unscaledDeltaTime;
            if (ring != null) ring.fillAmount = Mathf.Clamp01(held / Mathf.Max(0.01f, holdSeconds));
            if (held >= holdSeconds) Confirm();
        }

        void Confirm() { Cancel(); onConfirm.Invoke(); }

        /// <summary>Adds the hold behaviour and a radial ring (sprite: any round sprite, e.g. BobUi.Circle) to a button graphic.</summary>
        public static BobHoldButton Attach(Graphic target, Sprite ringSprite, Color ringColor, UnityAction onConfirm, float seconds = 0.6f)
        {
            var h = target.gameObject.AddComponent<BobHoldButton>();
            h.holdSeconds = seconds;
            var go = new GameObject("HoldRing", typeof(RectTransform));
            go.transform.SetParent(target.transform, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = new Vector2(-10, -10); rt.offsetMax = new Vector2(10, 10);
            var img = go.AddComponent<Image>();
            img.sprite = ringSprite; img.color = ringColor; img.raycastTarget = false;
            img.type = Image.Type.Filled; img.fillMethod = Image.FillMethod.Radial360; img.fillOrigin = (int)Image.Origin360.Top; img.fillAmount = 0;
            go.transform.SetAsFirstSibling(); // behind the label, so the fill reads as a ring round the button
            h.ring = img;
            if (onConfirm != null) h.onConfirm.AddListener(onConfirm);
            return h;
        }
    }
}

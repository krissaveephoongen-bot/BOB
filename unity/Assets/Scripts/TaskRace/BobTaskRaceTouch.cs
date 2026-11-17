// Battle of the Brooms: touch controls for the Task Race map (UX spec CTRL-Joystick / CTRL-TapToMove, docs 24
// section 10). Two invisible zones under the HUD page:
//
//   Joystick   the bottom-left area. Press anywhere in it and a floating base + knob appear under the thumb; the
//              drag vector is the hero's walk input, and the mode manager also reads it to close an S28 card at 70%.
//   WalkTap    the rest of the screen. A clean tap puts a walk destination on the floor under the finger; drags and
//              taps on a HUD panel never reach it, because both zones are the first children of the page.
//
// The joystick zone is lifted above the S28 dim when the card is built, so pushing the stick still cancels the card
// while the panel itself stays on top. Once the pause/result overlay exists it covers both zones.
using Bob.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bob.TaskRace
{
    [DisallowMultipleComponent]
    public class BobTaskRaceTouch : MonoBehaviour
    {
        [Tooltip("Knob travel in canvas units that means full speed.")]
        public float fullTilt = 150f;
        [Tooltip("Below this the stick reads zero, so a tap does not drift the hero.")]
        [Range(0f, 0.4f)] public float deadZone = 0.12f;
        [Tooltip("Bottom-left area that holds the joystick instead of tap-to-walk.")]
        public Vector2 zoneSize = new Vector2(760f, 560f);
        [Tooltip("Base disc and knob size, canvas units.")]
        public float baseSize = 190f, knobSize = 84f;

        public static BobTaskRaceTouch Current { get; private set; }

        RectTransform zone, baseDisc, hint, knob;
        Camera cam;
        BobTaskRaceHero hero;
        Vector2 origin, move, restPos;
        bool held;

        /// <summary>Walk input, x = right, y = forward; length 1 = full speed.</summary>
        public Vector2 Move => move;
        public bool IsHeld => held;
        /// <summary>True once a hero was found and wired; the verify log reads this.</summary>
        public bool HasHero => hero != null;

        /// <summary>Build the two zones under `pageRect`. Safe to call on every HUD rebuild.</summary>
        public void Build(RectTransform pageRect, Camera camera)
        {
            Current = this;
            cam = camera != null ? camera : Camera.main;

            var catcher = BobUi.Stretch(BobUi.Rect("WalkTap", pageRect));
            var catcherImage = catcher.gameObject.AddComponent<Image>();
            catcherImage.color = new Color(0f, 0f, 0f, 0f);
            catcher.gameObject.AddComponent<Pad>().Init(this, false);

            zone = BobUi.Pin(BobUi.Rect("Joystick", pageRect), Vector2.zero, Vector2.zero, Vector2.zero, zoneSize);
            var zoneImage = zone.gameObject.AddComponent<Image>();
            zoneImage.color = new Color(0f, 0f, 0f, 0f);
            zone.gameObject.AddComponent<Pad>().Init(this, true);

            baseSize = 216f; knobSize = 106f;
            var ink = new Color(BobUiR1.Paper.r, BobUiR1.Paper.g, BobUiR1.Paper.b, 0.15f);
            baseDisc = BobUi.Pin(BobUi.Disc(zone, ink, baseSize, "Base").rectTransform,
                Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(baseSize, baseSize));
            baseDisc.GetComponent<Image>().raycastTarget = false;
            var outline = baseDisc.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(BobUiR1.Paper.r, BobUiR1.Paper.g, BobUiR1.Paper.b, 0.85f);
            outline.effectDistance = new Vector2(3f, -3f);
            knob = BobUi.Pin(BobUi.Disc(zone, new Color(1f, 1f, 1f, 0.9f), knobSize, "Knob").rectTransform,
                Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(knobSize, knobSize));
            knob.GetComponent<Image>().raycastTarget = false;
            hint = BobUi.Pin(BobUi.Disc(zone, BobUiR1.Paper, knobSize, "Hint").rectTransform,
                Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(knobSize, knobSize));
            hint.GetComponent<Image>().raycastTarget = false;
            restPos = new Vector2(264f, 194f);
            baseDisc.anchoredPosition = restPos;
            knob.anchoredPosition = restPos;
            hint.anchoredPosition = restPos;
            SetVisible(false);

            hero = FindAnyObjectByType<BobTaskRaceHero>();
            if (hero != null) hero.input = ReadInput;
            else Debug.LogWarning("[TaskRaceTouch] no BobTaskRaceHero in the scene: the stick has nothing to drive.");
        }

        /// <summary>The S28 dim is built after the zones; lift the stick above it so the cancel gesture works.</summary>
        public void KeepJoystickOnTop()
        {
            if (zone != null) zone.SetAsLastSibling();
        }

        /// <summary>Swap the resting hint for the knob while the stick is held.</summary>
        public void SetVisible(bool visible)
        {
            if (knob != null) knob.gameObject.SetActive(visible);
            if (hint != null) hint.gameObject.SetActive(!visible);
        }

        void OnDestroy()
        {
            if (Current != this) return;
            Current = null;
            if (hero != null) hero.input = null;
        }

        /// <summary>Stick first, keyboard second: the editor and the APK emulator both work without a build flag.</summary>
        Vector2 ReadInput()
        {
            if (move.sqrMagnitude > 0.0001f) return move;
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        }

        internal void StickDown(PointerEventData e)
        {
            if (zone == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(zone, e.position, e.pressEventCamera, out origin)) return;
            held = true;
            move = Vector2.zero;
            baseDisc.anchoredPosition = origin;
            knob.anchoredPosition = origin;
            SetVisible(true);
        }

        internal void StickDrag(PointerEventData e)
        {
            if (!held || zone == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(zone, e.position, e.pressEventCamera, out var local)) return;

            var delta = local - origin;
            var tilt = delta / Mathf.Max(1f, fullTilt);
            if (tilt.magnitude < deadZone) tilt = Vector2.zero;
            else if (tilt.magnitude > 1f) tilt = tilt.normalized;
            move = tilt;
            knob.anchoredPosition = origin + Vector2.ClampMagnitude(delta, fullTilt);
        }

        internal void StickUp()
        {
            held = false;
            move = Vector2.zero;
            SetVisible(false);
            if (baseDisc != null) baseDisc.anchoredPosition = restPos;
        }

        internal void Tap(Vector2 screenPoint)
        {
            if (hero == null) return;
            if (cam == null) { cam = Camera.main; if (cam == null) return; }
            var plane = new Plane(Vector3.up, new Vector3(0f, hero.transform.position.y, 0f));
            var ray = cam.ScreenPointToRay(screenPoint);
            if (!plane.Raycast(ray, out float enter)) return;
            var point = ray.GetPoint(enter);

            // The river is not walkable: a tap on the far island while the gates are shut would walk the hero off
            // the rim, so it is ignored until the bridges open (the top bar carries the countdown).
            var manager = TaskRaceModeManager.Instance;
            if (manager != null && !manager.AreBridgesOpen)
            {
                float hx = hero.transform.position.x, px = point.x;
                if (Mathf.Abs(hx) > 2f && Mathf.Abs(px) > 2f && Mathf.Sign(hx) != Mathf.Sign(px)) return;
            }
            hero.SetDestination(point);
        }

        sealed class Pad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IPointerClickHandler
        {
            BobTaskRaceTouch owner;
            bool stick;

            public void Init(BobTaskRaceTouch touch, bool isStick) { owner = touch; stick = isStick; }

            public void OnPointerDown(PointerEventData e) { if (stick) owner.StickDown(e); }
            public void OnDrag(PointerEventData e) { if (stick) owner.StickDrag(e); }
            public void OnPointerUp(PointerEventData e) { if (stick) owner.StickUp(); }
            public void OnPointerClick(PointerEventData e) { if (!stick) owner.Tap(e.position); }
        }
    }
}

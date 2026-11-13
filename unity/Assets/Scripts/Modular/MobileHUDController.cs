// Battle of the Brooms: the mobile play HUD as one component (UX spec 5A.2). It holds the references to the HUD
// widgets and exposes a small API (SetTimer, SetRound, SetCoins, SetCartSlots, SetPrimary); it has no game rules.
//   top centre : pill timer (pulses red under `warnAt` seconds) + "รอบ 1/3"
//   top right  : coin counter, pause button, cart slot bar (icons only, 5 slots)
//   bottom right: one big contextual primary button + a hold-to-send button with a radial progress ring
// Every text can be a TextMeshProUGUI or a legacy UI Text: assign whichever you have (TMP wins when both are set).
// The widgets can be dragged in from a prefab in the Inspector, or built by code (BobGame.Hud.cs) and bound with the
// public fields before the first frame.
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bob.Modular
{
    public class MobileHUDController : MonoBehaviour
    {
        [Header("Top centre")]
        public Image timerPill;
        public TMP_Text timerTextTmp; public Text timerText;
        public TMP_Text roundTextTmp; public Text roundText;
        public Color pillNormal = new Color(1f, 0.99f, 0.95f, 1f), pillWarn = new Color(0.93f, 0.28f, 0.38f, 1f), pillFrozen = new Color(0.87f, 0.92f, 0.98f, 1f);
        public Color textNormal = new Color(0.17f, 0.18f, 0.26f, 1f), textWarn = Color.white;
        [Tooltip("Seconds left under which the pill pulses red")] public float warnAt = 15f;
        [Tooltip("Pulses per second while in the warning")] public float pulseRate = 2f;

        [Header("Top right")]
        public TMP_Text coinTextTmp; public Text coinText;
        public Button pauseButton;
        [Tooltip("Up to 5 Images, left to right; icons only")] public Image[] cartSlots = new Image[0];
        public Color emptySlot = new Color(0.17f, 0.18f, 0.26f, 0.18f);

        [Header("Bottom right")]
        public Button primaryButton;
        public TMP_Text primaryLabelTmp; public Text primaryLabel;
        public Image primaryImage;
        [Tooltip("Hold-to-submit button (needs a BobHoldButton or this script's own hold handler)")] public Button submitButton;
        public Image submitRing;
        [Tooltip("Seconds to hold the submit button")] public float submitHoldSeconds = 0.6f;

        public UnityEvent OnPrimary = new UnityEvent();
        public UnityEvent OnPause = new UnityEvent();
        public UnityEvent OnSubmit = new UnityEvent();

        bool warning, frozen;
        float held = -1f;

        bool bound;

        void Awake()
        {
            if (primaryButton != null || pauseButton != null || submitButton != null) Bind();
        }

        /// <summary>Wire the buttons. Awake does it when they are assigned in the Inspector; code that assigns them later calls it.</summary>
        public void Bind()
        {
            if (bound) return; bound = true;
            if (primaryButton != null) primaryButton.onClick.AddListener(() => OnPrimary.Invoke());
            if (pauseButton != null) pauseButton.onClick.AddListener(() => OnPause.Invoke());
            if (submitButton != null)
            {
                // only add the hold handler when the button does not already have one (BobHoldButton)
                if (submitButton.GetComponent<Bob.UI.BobHoldButton>() == null && submitButton.GetComponent<SubmitHold>() == null)
                {
                    var h = submitButton.gameObject.AddComponent<SubmitHold>(); h.owner = this;
                }
                if (submitRing != null) submitRing.fillAmount = 0;
            }
        }

        void Update()
        {
            if (timerPill != null && warning && !frozen)
            {
                float k = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseRate * Mathf.PI * 2f);
                timerPill.color = Color.Lerp(pillNormal, pillWarn, k);
                SetColor(timerTextTmp, timerText, Color.Lerp(textNormal, textWarn, k));
            }
        }

        // ------------------------------------------------------------------ API

        public void SetTimer(float seconds, bool isFrozen, bool live)
        {
            frozen = isFrozen;
            warning = live && !isFrozen && seconds <= warnAt && seconds > 0;
            SetText(timerTextTmp, timerText, Mathf.CeilToInt(Mathf.Max(0, seconds)) + (isFrozen ? "*" : ""));
            if (timerPill == null) return;
            if (isFrozen) { timerPill.color = pillFrozen; SetColor(timerTextTmp, timerText, textNormal); }
            else if (!warning) { timerPill.color = pillNormal; SetColor(timerTextTmp, timerText, textNormal); }
        }

        public void SetRound(int round, int total) => SetText(roundTextTmp, roundText, $"รอบ {round}/{total}");
        public void SetCoins(int coins) => SetText(coinTextTmp, coinText, coins.ToString());

        /// <summary>Show the cart's layers as icons (bottom layer first). Missing entries show an empty slot.</summary>
        public void SetCartSlots(IList<Sprite> layerIcons)
        {
            for (int i = 0; i < cartSlots.Length; i++)
            {
                var img = cartSlots[i]; if (img == null) continue;
                bool has = layerIcons != null && i < layerIcons.Count && layerIcons[i] != null;
                img.sprite = has ? layerIcons[i] : null;
                img.color = has ? Color.white : emptySlot;
                img.preserveAspect = true; img.raycastTarget = false;
            }
        }

        public void SetPrimary(string label, bool enabled, Color color)
        {
            SetText(primaryLabelTmp, primaryLabel, label);
            if (primaryButton != null) primaryButton.interactable = enabled;
            if (primaryImage != null) primaryImage.color = color;
        }

        public void SetSubmitEnabled(bool enabled) { if (submitButton != null) submitButton.interactable = enabled; }

        // ------------------------------------------------------------------ hold-to-submit (used when the button has no BobHoldButton)

        internal void HoldDown() { if (submitButton != null && submitButton.interactable) held = 0; }
        internal void HoldUp() { held = -1; if (submitRing != null) submitRing.fillAmount = 0; }
        internal void HoldTick()
        {
            if (held < 0) return;
            held += Time.unscaledDeltaTime;
            if (submitRing != null) submitRing.fillAmount = Mathf.Clamp01(held / Mathf.Max(0.01f, submitHoldSeconds));
            if (held >= submitHoldSeconds) { HoldUp(); OnSubmit.Invoke(); }
        }

        sealed class SubmitHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
        {
            public MobileHUDController owner;
            public void OnPointerDown(PointerEventData e) => owner.HoldDown();
            public void OnPointerUp(PointerEventData e) => owner.HoldUp();
            public void OnPointerExit(PointerEventData e) => owner.HoldUp();
            void Update() { if (owner != null) owner.HoldTick(); }
        }

        // ------------------------------------------------------------------ helpers

        static void SetText(TMP_Text tmp, Text legacy, string s)
        {
            if (tmp != null) { if (tmp.text != s) tmp.text = s; }
            else if (legacy != null && legacy.text != s) legacy.text = s;
        }

        static void SetColor(TMP_Text tmp, Text legacy, Color c)
        {
            if (tmp != null) tmp.color = c; else if (legacy != null) legacy.color = c;
        }
    }
}

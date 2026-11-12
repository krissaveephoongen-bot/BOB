// Battle of the Brooms: frame animator without an Animator Controller. Good for FX, UI icons and quick tests.
// Works on a SpriteRenderer or a UI Image. Fill `frames` by dragging the sliced sprites of a sheet in.
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Bob.Art
{
    public class BobSpriteAnimator : MonoBehaviour
    {
        public Sprite[] frames;
        public float fps = 10f;
        public bool loop = true;
        public bool playOnEnable = true;
        [Tooltip("For one-shot FX: destroy this GameObject after the last frame.")]
        public bool destroyWhenDone;
        public event Action Finished;

        SpriteRenderer sr; Image img;
        float t; bool playing;

        void Awake() { sr = GetComponent<SpriteRenderer>(); img = GetComponent<Image>(); }
        void OnEnable() { if (playOnEnable) Play(); }

        public void Play(Sprite[] newFrames = null, float? newFps = null, bool? newLoop = null)
        {
            if (newFrames != null) frames = newFrames;
            if (newFps.HasValue) fps = newFps.Value;
            if (newLoop.HasValue) loop = newLoop.Value;
            t = 0; playing = frames != null && frames.Length > 0;
            Show(0);
        }

        public void Stop() => playing = false;

        void Update()
        {
            if (!playing) return;
            t += Time.deltaTime;
            int i = Mathf.FloorToInt(t * fps);
            if (i >= frames.Length)
            {
                if (loop) { t %= frames.Length / fps; i %= frames.Length; }
                else
                {
                    playing = false; Show(frames.Length - 1); Finished?.Invoke();
                    if (destroyWhenDone) Destroy(gameObject);
                    return;
                }
            }
            Show(i);
        }

        void Show(int i)
        {
            if (frames == null || frames.Length == 0) return;
            if (sr) sr.sprite = frames[i];
            if (img) img.sprite = frames[i];
        }
    }
}

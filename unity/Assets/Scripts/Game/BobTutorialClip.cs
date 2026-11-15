// Battle of the Brooms: the optional tutorial preview inside the S28 card (docs 24 section 15.15). If a clip named
// Resources/Video/tutorial_<taskId>.mp4 exists, the card plays it in the top-left corner for a few seconds, muted, then
// fades out and pauses - it never covers the play area for long, and with no clips shipped it costs one resource lookup.
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Bob.Game
{
    public class BobTutorialClip : MonoBehaviour
    {
        const float HoldSeconds = 4f;
        const float FadeSeconds = 0.5f;

        RawImage image;
        VideoPlayer player;
        RenderTexture target;
        float age;

        /// <summary>Card opened: show the clip for this minigame id, or hide the slot when there is none.</summary>
        public static void Play(RectTransform card, string taskId)
        {
            if (card == null || string.IsNullOrEmpty(taskId)) return;
            var clip = Resources.Load<VideoClip>("Video/tutorial_" + taskId);
            var slot = card.GetComponentInChildren<BobTutorialClip>(true);
            if (clip == null)
            {
                if (slot != null) slot.gameObject.SetActive(false);
                return;
            }
            if (slot == null) slot = Create(card);
            slot.Show(clip);
        }

        static BobTutorialClip Create(RectTransform card)
        {
            var rt = BobUi.Rect("TutorialClip", card);
            BobUi.Pin(rt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(240f, 135f));
            var img = rt.gameObject.AddComponent<RawImage>();
            img.raycastTarget = false;
            var slot = rt.gameObject.AddComponent<BobTutorialClip>();
            slot.image = img;
            return slot;
        }

        void Show(VideoClip clip)
        {
            if (target == null)
            {
                target = new RenderTexture(480, 270, 0, RenderTextureFormat.ARGB32) { name = "TutorialClip" };
                target.Create();
            }
            if (player == null)
            {
                player = gameObject.AddComponent<VideoPlayer>();
                player.playOnAwake = false;
                player.isLooping = true;
                player.waitForFirstFrame = true;
                player.audioOutputMode = VideoAudioOutputMode.None;
                player.renderMode = VideoRenderMode.RenderTexture;
                player.targetTexture = target;
                image.texture = target;
            }
            gameObject.SetActive(true);
            player.Stop();
            player.clip = clip;
            player.Play();
            age = 0f;
            image.color = Color.white;
        }

        void Update()
        {
            age += Time.unscaledDeltaTime;
            if (age < HoldSeconds) return;
            float a = Mathf.Clamp01(1f - (age - HoldSeconds) / FadeSeconds);
            var c = image.color;
            c.a = a;
            image.color = c;
            if (a <= 0f)
            {
                gameObject.SetActive(false);
                if (player != null) player.Pause();
            }
        }

        void OnDisable()
        {
            // The card hides between tasks: stop paying for the decode.
            if (player != null && player.isPlaying) player.Pause();
        }

        void OnDestroy()
        {
            if (player != null) player.targetTexture = null;
            if (target != null)
            {
                target.Release();
                Destroy(target);
            }
        }
    }
}

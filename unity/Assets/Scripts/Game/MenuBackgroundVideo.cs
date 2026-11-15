// Battle of the Brooms: the video backdrop behind the mode and stage select screens (docs 24 section 15.15). The
// still room picture stays exactly where it was; when a clip exists this RawImage sits on top of it, so a slow decode
// shows the picture instead of a black frame. One 1280x720 zero-depth RenderTexture serves the whole screen, the audio
// track is never decoded, and nothing here allocates per frame.
//
// The clip lives at Resources/Video/menu_loop.mp4 (bob video importer: H264, no audio). Dropping files in
// Assets/Video/ still imports them with the same settings; the importer logs the one-line move if a clip sits outside
// Resources, where Resources.Load cannot see it.
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Bob.Game
{
    [RequireComponent(typeof(RawImage))]
    public class MenuBackgroundVideo : MonoBehaviour
    {
        /// <summary>True once a menu clip is imported; without one every screen keeps its still backdrop.</summary>
        public static bool Available => Resources.Load<VideoClip>("Video/menu_loop") != null;

        /// <summary>Add the video layer just above the still backdrop and below its softening veil.</summary>
        public static MenuBackgroundVideo Attach(RectTransform screen)
        {
            if (screen == null || !Available) return null;
            var go = BobUi.Rect("MenuVideo", screen);
            go.SetSiblingIndex(1);
            BobUi.Stretch(go);
            var img = go.gameObject.AddComponent<RawImage>();
            img.raycastTarget = false;
            img.color = Color.white;
            var video = go.gameObject.AddComponent<MenuBackgroundVideo>();
            video.Bind(img);
            return video;
        }

        RawImage image;
        VideoPlayer player;
        RenderTexture target;

        void Bind(RawImage img)
        {
            image = img;
            target = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32) { name = "MenuVideo" };
            target.Create();

            player = gameObject.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.isLooping = true;
            player.waitForFirstFrame = true;
            player.audioOutputMode = VideoAudioOutputMode.None; // never steps on the BGM
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = target;
            player.clip = Resources.Load<VideoClip>("Video/menu_loop");
            player.prepareCompleted += OnPrepared;

            image.texture = target; // before the first frame arrives this shows the still backdrop through clear alpha
            image.color = new Color(1f, 1f, 1f, 0f);
            player.Prepare();
        }

        void OnPrepared(VideoPlayer source)
        {
            source.Play();
            if (image != null) image.color = Color.white;
        }

        void OnDestroy()
        {
            if (player != null)
            {
                player.prepareCompleted -= OnPrepared;
                player.targetTexture = null;
            }
            if (target != null)
            {
                target.Release();
                Destroy(target);
            }
        }
    }
}

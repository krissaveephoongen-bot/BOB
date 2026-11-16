// Battle of the Brooms: tiny sound player. One instance (from Assets/Art/BobAudio.prefab) survives scene loads.
//   BobSfx.Play("place_layer");            one-shot with slight pitch variation
//   BobSfx.Loop("scrub_loop", true/false); start / stop a looping effect
//   BobSfx.Music(true);                    start the placeholder music loop
using System.Collections.Generic;
using UnityEngine;

namespace Bob.Art
{
    public class BobSfx : MonoBehaviour
    {
        public AudioClip[] clips;
        public AudioClip music;
        [Tooltip("Named music tracks (menu_, game_, result_, tense_ prefix); Track() crossfades between them")] public AudioClip[] tracks = new AudioClip[0];
        [Range(0, 1)] public float sfxVolume = 0.9f, musicVolume = 0.45f;
        [Range(0, 0.3f)] public float pitchJitter = 0.06f;

        static BobSfx inst;
        readonly Dictionary<string, AudioClip> byName = new Dictionary<string, AudioClip>();
        readonly Dictionary<string, AudioSource> loops = new Dictionary<string, AudioSource>();
        AudioSource oneShot, musicSrc;
        bool musicOn = true; string trackNow; Coroutine fade;

        void Awake()
        {
            if (inst != null && inst != this) { Destroy(gameObject); return; }
            inst = this;
            DontDestroyOnLoad(gameObject);
            foreach (var c in clips) if (c != null) byName[c.name] = c;
            oneShot = gameObject.AddComponent<AudioSource>();
            oneShot.playOnAwake = false;
            musicSrc = gameObject.AddComponent<AudioSource>();
            musicSrc.loop = true; musicSrc.playOnAwake = false; musicSrc.clip = music;
        }

        public static void Play(string name, float volume = 1f)
        {
            if (inst == null) { Debug.LogWarning("BobSfx: no player for " + name); return; }
            if (!inst.byName.TryGetValue(name, out var c))
            {
                // ping_pop ships as code, not as a clip (docs 12 section 6): synthesise it instead of going silent.
                if (name != "ping_pop") { Debug.LogWarning("BobSfx: no clip " + name); return; }
                c = inst.byName[name] = BobTone.PingPop();
            }
            inst.oneShot.pitch = 1f + Random.Range(-inst.pitchJitter, inst.pitchJitter);
            inst.oneShot.PlayOneShot(c, volume * inst.sfxVolume);
        }

        public static void Loop(string name, bool on, float volume = 1f)
        {
            if (inst == null || !inst.byName.TryGetValue(name, out var c)) return;
            if (!inst.loops.TryGetValue(name, out var src))
            {
                src = inst.gameObject.AddComponent<AudioSource>();
                src.clip = c; src.loop = true; src.playOnAwake = false;
                inst.loops[name] = src;
            }
            src.volume = volume * inst.sfxVolume;
            if (on && !src.isPlaying) src.Play();
            else if (!on) src.Stop();
        }

        // The Task Race scenes carry no BobAudio prefab, so the player boots itself once from Resources the
        // first time a scene loads. A scene that already has one wins: its Awake ran before this and set inst.
        static bool bootTried;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (bootTried) return;
            bootTried = true;
            AudioListener.volume = PlayerPrefs.GetInt("bob.sound", 1) == 1 ? 1f : 0f;
            if (inst != null) return;
            var prefab = Resources.Load<GameObject>("Audio/BobAudio");
            if (prefab != null) Object.Instantiate(prefab);
        }

        public static void Music(bool on)
        {
            if (inst == null) return;
            inst.musicOn = on;
            if (inst.musicSrc.clip == null) return;
            inst.musicSrc.volume = inst.musicVolume;
            if (on && !inst.musicSrc.isPlaying) inst.musicSrc.Play();
            else if (!on) inst.musicSrc.Stop();
        }

        /// <summary>Crossfade to the music track whose name starts with `kind` (menu, game, result, tense). No-op if already playing.</summary>
        public static void Track(string kind)
        {
            if (inst == null || inst.trackNow == kind) return;
            AudioClip next = null;
            foreach (var c in inst.tracks) if (c != null && c.name.StartsWith(kind)) { next = c; break; }
            if (next == null) return;
            inst.trackNow = kind;
            if (inst.fade != null) inst.StopCoroutine(inst.fade);
            inst.fade = inst.StartCoroutine(inst.CrossFade(next));
        }

        System.Collections.IEnumerator CrossFade(AudioClip next)
        {
            float v0 = musicSrc.isPlaying ? musicSrc.volume : 0f;
            for (float e = 0; e < 0.6f && musicSrc.isPlaying; e += Time.unscaledDeltaTime) { musicSrc.volume = Mathf.Lerp(v0, 0, e / 0.6f); yield return null; }
            musicSrc.clip = next; musicSrc.volume = 0;
            if (musicOn) musicSrc.Play();
            for (float e = 0; e < 0.8f; e += Time.unscaledDeltaTime) { musicSrc.volume = Mathf.Lerp(0, musicVolume, e / 0.8f); yield return null; }
            musicSrc.volume = musicVolume;
        }
    }
}

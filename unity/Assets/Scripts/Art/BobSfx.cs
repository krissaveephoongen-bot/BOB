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
        [Range(0, 1)] public float sfxVolume = 0.9f, musicVolume = 0.45f;
        [Range(0, 0.3f)] public float pitchJitter = 0.06f;

        static BobSfx inst;
        readonly Dictionary<string, AudioClip> byName = new Dictionary<string, AudioClip>();
        readonly Dictionary<string, AudioSource> loops = new Dictionary<string, AudioSource>();
        AudioSource oneShot, musicSrc;

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
            if (inst == null || !inst.byName.TryGetValue(name, out var c)) { Debug.LogWarning("BobSfx: no clip " + name); return; }
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

        public static void Music(bool on)
        {
            if (inst == null || inst.music == null) return;
            inst.musicSrc.volume = inst.musicVolume;
            if (on && !inst.musicSrc.isPlaying) inst.musicSrc.Play();
            else if (!on) inst.musicSrc.Stop();
        }
    }
}

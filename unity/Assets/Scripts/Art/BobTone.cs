// Battle of the Brooms: the one earcon that has no file. docs 12 section 6 lists ping_pop and docs 24 section 15.29
// names it for a sabotaged station, but the sound library ships 38 clips and no ping, so it is synthesised once here
// (0.16 s, 880 -> 1500 Hz, 44.1 kHz mono, the same recipe as the placeholder SFX). A real .wav dropped into
// Assets/Audio/SFX later wins without touching this: BobSfx only asks for the tone when the name is missing.
using UnityEngine;

namespace Bob.Art
{
    public static class BobTone
    {
        const int Rate = 44100;
        const float Seconds = 0.16f;
        static AudioClip ping;

        public static AudioClip PingPop()
        {
            if (ping != null) return ping;
            int n = Mathf.RoundToInt(Rate * Seconds);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                // 6 ms fade-in so the clip does not click, then a fast exponential decay.
                float env = Mathf.Exp(-26f * t) * Mathf.Clamp01(t / 0.006f);
                float f = 880f + 620f * Mathf.Clamp01(t / 0.05f);   // a rise, which reads as "up/look here"
                float s = Mathf.Sin(2f * Mathf.PI * f * t) + 0.35f * Mathf.Sin(4f * Mathf.PI * f * t);
                data[i] = 0.55f * s * env;
            }
            ping = AudioClip.Create("ping_pop", n, 1, Rate, false);
            ping.SetData(data, 0);
            return ping;
        }
    }
}

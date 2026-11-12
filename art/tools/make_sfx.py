#!/usr/bin/env python3
"""Battle of the Brooms: placeholder sound effects, synthesised from code (no samples, no licences).

    python3 art/tools/make_sfx.py      (needs numpy)

Writes 16-bit mono 44.1 kHz WAVs to unity/Assets/Audio/SFX (and bob-audio-list.json) and a short placeholder music loop to
unity/Assets/Audio/Music. Every sound is deterministic (fixed random seed) so a rerun gives the same files.
"""
import os, wave, json
import numpy as np

# repo root (art/tools/ -> art/ -> repo), so the files land straight in the Unity project
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "unity", "Assets", "Audio")
SR = 44100
rng = np.random.default_rng(7)

def t_(dur): return np.arange(int(SR * dur)) / SR
def env(n, a=0.005, d=0.1, s=0.0, r=0.05, sus=None):
    """ADSR on n samples. Sustain level s held for whatever is left."""
    a_, d_, r_ = int(a * SR), int(d * SR), int(r * SR)
    hold = max(0, n - a_ - d_ - r_)
    e = np.concatenate([np.linspace(0, 1, a_, False), np.linspace(1, s, d_, False), np.full(hold, s), np.linspace(s, 0, r_)])
    return np.pad(e, (0, max(0, n - len(e))))[:n]
def expdecay(n, tau): return np.exp(-np.arange(n) / (tau * SR))
def sine(freq, dur):
    f = np.broadcast_to(freq, t_(dur).shape) if np.ndim(freq) else np.full(t_(dur).shape, freq)
    return np.sin(2 * np.pi * np.cumsum(f) / SR)
def square(freq, dur, duty=0.5):
    f = np.full(t_(dur).shape, freq) if not np.ndim(freq) else freq
    ph = np.cumsum(f) / SR % 1.0
    return np.where(ph < duty, 1.0, -1.0)
def tri(freq, dur):
    f = np.full(t_(dur).shape, freq) if not np.ndim(freq) else freq
    ph = np.cumsum(f) / SR % 1.0
    return 4 * np.abs(ph - 0.5) - 1
def noise(dur): return rng.uniform(-1, 1, int(SR * dur))
def lowpass(x, cutoff):
    """One-pole low-pass; cutoff may be an array (sweep)."""
    c = np.broadcast_to(cutoff, x.shape)
    a = 1 - np.exp(-2 * np.pi * c / SR)
    y = np.empty_like(x); acc = 0.0
    for i in range(len(x)):
        acc += a[i] * (x[i] - acc); y[i] = acc
    return y
def highpass(x, cutoff): return x - lowpass(x, cutoff)
def bandpass(x, lo, hi): return highpass(lowpass(x, hi), lo)
def mix(*parts):
    n = max(len(p) for p in parts)
    return sum(np.pad(p, (0, n - len(p))) for p in parts)
def at(x, sec, total):
    o = np.zeros(int(SR * total)); i = int(sec * SR); o[i:i + len(x)] += x[:len(o) - i]; return o
def note(midi): return 440.0 * 2 ** ((midi - 69) / 12)
def pluck(midi, dur, kind="tri", vol=0.5, tau=0.15):
    w = {"tri": tri, "sq": lambda f, d: square(f, d, 0.25), "sin": sine}[kind](note(midi), dur)
    return w * expdecay(len(w), tau) * env(len(w), 0.003, 0.0, 1.0, 0.02) * vol

def save(name, x, folder="SFX", peak=0.89):
    x = np.asarray(x, dtype=np.float64)
    fade = min(len(x), int(0.004 * SR)); x[-fade:] *= np.linspace(1, 0, fade)
    m = np.max(np.abs(x)) or 1.0
    x = x / m * peak
    path = os.path.join(OUT, folder, name + ".wav")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with wave.open(path, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes((x * 32767).astype("<i2").tobytes())
    LIST.append({"name": name, "folder": folder, "sec": round(len(x) / SR, 3)})
LIST = []

def make():
    # ---- UI
    save("ui_tap", sine(1400, 0.05) * expdecay(int(.05 * SR), 0.012), peak=.7)
    save("ui_confirm", mix(at(pluck(76, .12, "sin", tau=.06), 0, .25), at(pluck(83, .16, "sin", tau=.08), .07, .25)))
    save("ui_cancel", mix(at(pluck(72, .12, "tri", tau=.06), 0, .25), at(pluck(65, .16, "tri", tau=.08), .08, .25)))
    save("ui_card_flip", bandpass(noise(.09), 1500, 6000) * expdecay(int(.09 * SR), .025))
    save("coin", mix(at(pluck(83, .08, "sq", .4, .04), 0, .35), at(pluck(88, .3, "sq", .4, .1), .06, .35)))
    # ---- picking / stacking
    d = .12; f = np.linspace(500, 1100, int(d * SR))
    save("pickup", sine(f, d) * env(int(d * SR), .005, .1, 0, .01) + 0.3 * bandpass(noise(d), 2000, 5000) * expdecay(int(d * SR), .02))
    d = .25; f = np.linspace(180, 70, int(d * SR))
    save("place_layer", sine(f, d) * expdecay(int(d * SR), .06) + 0.5 * lowpass(noise(d), 900) * expdecay(int(d * SR), .02))
    d = .22; f = np.linspace(200, 600, int(d * SR))
    save("lift_layer", sine(f, d) * env(int(d * SR), .01, .2, 0, .01) * 0.8 + 0.2 * highpass(noise(d), 3000) * expdecay(int(d * SR), .05))
    save("drop_invalid", square(np.full(int(.18 * SR), 110.0), .18, .5) * env(int(.18 * SR), .005, .15, .3, .03) * .5, peak=.55)
    # ---- reactions
    save("reaction_good", mix(*[at(pluck(m, .4, "sin", .5, .25) + pluck(m + 12, .4, "sin", .15, .1), i * .06, .7) for i, m in enumerate((72, 76, 79, 84))]))
    d = .5; f = np.linspace(330, 140, int(d * SR))
    save("reaction_bad", (square(f, d, .5) * .4 + square(f * 1.01, d, .5) * .4) * env(int(d * SR), .01, .3, .4, .15) * 0.6, peak=.65)
    d = 1.0; n = int(d * SR)
    crackle = (rng.random(n) > 0.985) * rng.uniform(-1, 1, n)
    save("fizz", (0.6 * highpass(noise(d), 3000) * env(n, .02, .2, .5, .5) + lowpass(crackle, 7000)) )
    # ---- tools / energy
    d = .14; f = np.linspace(600, 1500, int(d * SR))
    save("bubble_pop", sine(f, d) * expdecay(int(d * SR), .03))
    d = 1.1; n = int(d * SR)
    save("steam_hiss", bandpass(noise(d), 2500, 9000) * env(n, .08, .2, .7, .6))
    d = .35; n = int(d * SR)
    save("spray_psst", bandpass(noise(d), 3000, 10000) * env(n, .005, .08, .5, .2))
    d = .45; n = int(d * SR)
    save("broom_sweep", bandpass(noise(d), 800, 5000) * np.sin(np.linspace(0, np.pi, n)) ** 2)
    one = bandpass(noise(.12), 1500, 6000) * np.sin(np.linspace(0, np.pi, int(.12 * SR)))
    save("scrub_loop", np.concatenate([one, one * .7, one, one * .7]))  # loopable, 0.48 s
    d = .5; n = int(d * SR)
    save("air_whoosh", bandpass(noise(d), np.linspace(400, 2500, n), 6000) * np.sin(np.linspace(0, np.pi, n)))
    d = .9; n = int(d * SR)
    save("water_pour", lowpass(noise(d), 1200) * env(n, .05, .1, .8, .3) + 0.3 * sum(at(sine(np.linspace(800, 1400, 2000), 2000 / SR) * expdecay(2000, .01), s, d) for s in rng.uniform(0, .8, 14)))
    # ---- movement / cart
    d = .07; n = int(d * SR)
    save("footstep", lowpass(noise(d), 600) * expdecay(n, .015) + 0.4 * sine(90, d) * expdecay(n, .02))
    d = 1.0; n = int(d * SR)
    rumble = lowpass(noise(d), 180) * 0.8 + 0.2 * np.sign(np.sin(2 * np.pi * 9 * t_(d))) * lowpass(noise(d), 400)
    save("cart_roll_loop", rumble)
    # ---- enemies
    d = .3; n = int(d * SR)
    save("enemy_hit", sine(np.linspace(300, 120, n), d) * expdecay(n, .07) + 0.5 * lowpass(noise(d), 2000) * expdecay(n, .03))
    d = .8; n = int(d * SR)
    save("enemy_defeat", mix(sine(np.linspace(600, 100, n), d) * env(n, .01, .4, .2, .3) * .6,
                             *[at(pluck(m, .3, "sq", .25, .08), .25 + i * .07, d + .2) for i, m in enumerate((84, 88, 91))]))
    d = .6; n = int(d * SR)
    save("enemy_spawn", mix(lowpass(noise(d), np.linspace(200, 2000, n)) * env(n, .2, .2, .5, .2) * .7, at(pluck(48, .4, "sq", .4, .2), .15, d)))
    d = .7; n = int(d * SR)
    save("mud_splat", lowpass(noise(d), 700) * expdecay(n, .08) + sine(np.linspace(160, 60, n), d) * expdecay(n, .1))
    # ---- round flow
    save("timer_tick", sine(2000, .03) * expdecay(int(.03 * SR), .006))
    save("timer_warning", mix(at(square(880, .09, .5) * .4, 0, .4), at(square(880, .09, .5) * .4, .18, .4)), peak=.55)
    save("round_start", mix(*[at(pluck(m, .25, "sq", .35, .1), i * .12, 1.0) for i, m in enumerate((60, 64, 67))], at(pluck(72, .5, "sq", .45, .25), .36, 1.0)))
    save("round_win", mix(*[at(pluck(m, .3, "sq", .35, .12) + pluck(m - 12, .3, "tri", .3, .15), i * .11, 1.5) for i, m in enumerate((67, 72, 76, 79))],
                          at(pluck(84, .9, "sq", .45, .45) + pluck(72, .9, "tri", .3, .4), .44, 1.5)))
    save("round_lose", mix(*[at(pluck(m, .35, "tri", .45, .2), i * .2, 1.4) for i, m in enumerate((67, 63, 60))], at(pluck(55, .7, "tri", .5, .4), .6, 1.4)))
    d = .5; n = int(d * SR)
    save("ability_activate", sine(np.linspace(300, 1500, n), d) * env(n, .02, .3, .3, .15) * .6 + 0.3 * bandpass(noise(d), np.linspace(500, 6000, n), 9000) * np.sin(np.linspace(0, np.pi, n)))
    save("score_count", sine(1700, .04) * expdecay(int(.04 * SR), .01) + sine(2550, .04) * .4 * expdecay(int(.04 * SR), .008))

    # ---- placeholder music: 8 bars, 120 bpm, C major bounce. Loops cleanly (16 s).
    bpm = 120; beat = 60 / bpm; bars = 8; total = bars * 4 * beat
    chords = [(48, 52, 55), (45, 48, 52), (41, 45, 48), (43, 47, 50)] * 2   # C Am F G
    melody = [72, 76, 79, 76, 77, 76, 74, 72,  69, 72, 76, 72, 74, 72, 71, 69,
              65, 69, 72, 69, 72, 74, 72, 69,  67, 71, 74, 71, 74, 76, 74, 71,
              72, 76, 79, 81, 79, 76, 74, 72,  69, 72, 76, 79, 76, 74, 72, 69,
              65, 69, 72, 77, 76, 72, 69, 72,  67, 71, 74, 79, 77, 74, 71, 67]
    parts = []
    for b, ch in enumerate(chords):
        t0 = b * 4 * beat
        for k in range(4):   # bass on each beat (root, fifth)
            parts.append(at(pluck(ch[0] - 12 + (7 if k % 2 else 0), beat * .9, "tri", .55, .2), t0 + k * beat, total))
        for k in range(4):   # off-beat chord stabs
            for m in ch:
                parts.append(at(pluck(m + 12, beat * .4, "sq", .07, .05), t0 + k * beat + beat / 2, total))
        for k in range(8):   # eighth-note melody
            m = melody[b * 8 + k]
            parts.append(at(pluck(m, beat * .5, "sq", .16, .12), t0 + k * beat / 2, total))
        for k in range(4):   # hats + kick
            hat = highpass(noise(.03), 7000) * expdecay(int(.03 * SR), .008) * .12
            parts.append(at(hat, t0 + k * beat + beat / 2, total))
            kick = sine(np.linspace(140, 45, int(.12 * SR)), .12) * expdecay(int(.12 * SR), .04) * .5
            parts.append(at(kick, t0 + k * beat, total))
    save("music_placeholder_loop", mix(*parts), folder="Music", peak=0.7)

def make_coop():
    """Co-op sounds (GDD 4 and 20). Made after make() so the shared random stream, and every older file, stays the same."""
    d = .32; n = int(d * SR)  # pass a part to a team-mate: short rising whoosh + soft pluck
    save("pass_part", mix(bandpass(noise(d), np.linspace(600, 3000, n), 7000) * np.sin(np.linspace(0, np.pi, n)) * .6,
                          at(pluck(79, .18, "sin", .5, .07), .16, d)))
    d = .22; n = int(d * SR)  # park a part on the table: wooden knock
    save("table_put", sine(np.linspace(420, 260, n), d) * expdecay(n, .03) + .4 * bandpass(noise(d), 800, 3000) * expdecay(n, .012))
    d = .3; n = int(d * SR)   # grab the cart handle: metal clack + wheel start
    save("cart_grab", sine(np.linspace(900, 700, n), d) * expdecay(n, .02) * .6 + .5 * lowpass(noise(d), 300) * env(n, .01, .1, .3, .1))
    save("lobby_join", mix(at(pluck(72, .14, "sin", .5, .07), 0, .45), at(pluck(79, .14, "sin", .5, .07), .09, .45),
                           at(pluck(84, .25, "sin", .5, .12), .18, .45)))
    save("ready_on", mix(at(pluck(76, .1, "sq", .35, .05), 0, .3), at(pluck(84, .18, "sq", .35, .08), .07, .3)))
    save("vote_tick", sine(1200, .06) * expdecay(int(.06 * SR), .015) + sine(1800, .06) * .4 * expdecay(int(.06 * SR), .01))

if __name__ == "__main__":
    make()
    make_coop()
    with open(os.path.join(OUT, "bob-audio-list.json"), "w") as fh:
        json.dump(LIST, fh, indent=1)
    print(len(LIST), "sounds written to", OUT)

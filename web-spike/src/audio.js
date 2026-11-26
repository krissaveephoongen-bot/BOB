let ctx = null;
let enabled = true;

function ac() {
  if (!enabled) return null;
  if (!ctx) {
    const Ctor = window.AudioContext || window.webkitAudioContext;
    if (!Ctor) return null;
    ctx = new Ctor();
  }
  if (ctx.state === 'suspended') ctx.resume();
  return ctx;
}

export function setAudioEnabled(on) {
  enabled = !!on;
}

export function unlockAudio() {
  ac();
}

function tone({ freq = 440, dur = 0.12, type = 'sine', gain = 0.16, slide = 0, delay = 0 }) {
  const a = ac();
  if (!a) return;
  const t0 = a.currentTime + delay;
  const osc = a.createOscillator();
  const g = a.createGain();
  osc.type = type;
  osc.frequency.setValueAtTime(freq, t0);
  if (slide) osc.frequency.exponentialRampToValueAtTime(Math.max(40, freq + slide), t0 + dur);
  g.gain.setValueAtTime(0.0001, t0);
  g.gain.exponentialRampToValueAtTime(gain, t0 + 0.012);
  g.gain.exponentialRampToValueAtTime(0.0001, t0 + dur);
  osc.connect(g).connect(a.destination);
  osc.start(t0);
  osc.stop(t0 + dur + 0.02);
}

function noise({ dur = 0.22, gain = 0.12, hp = 900, delay = 0 }) {
  const a = ac();
  if (!a) return;
  const t0 = a.currentTime + delay;
  const frames = Math.floor(a.sampleRate * dur);
  const buf = a.createBuffer(1, frames, a.sampleRate);
  const d = buf.getChannelData(0);
  for (let i = 0; i < frames; i++) d[i] = (Math.random() * 2 - 1) * (1 - i / frames);
  const src = a.createBufferSource();
  src.buffer = buf;
  const filt = a.createBiquadFilter();
  filt.type = 'highpass';
  filt.frequency.value = hp;
  const g = a.createGain();
  g.gain.setValueAtTime(gain, t0);
  g.gain.exponentialRampToValueAtTime(0.0001, t0 + dur);
  src.connect(filt).connect(g).connect(a.destination);
  src.start(t0);
}

// Names mirror the Unity SFX table so a real asset drop-in is a rename, not a redesign.
export const SFX = {
  ui_tap: () => tone({ freq: 620, dur: 0.07, type: 'triangle', gain: 0.1 }),
  ui_confirm: () => { tone({ freq: 520, dur: 0.09, type: 'triangle' }); tone({ freq: 780, dur: 0.1, type: 'triangle', delay: 0.07 }); },
  ui_cancel: () => tone({ freq: 300, dur: 0.11, type: 'sawtooth', gain: 0.08, slide: -120 }),
  ready_on: () => tone({ freq: 880, dur: 0.12, type: 'square', gain: 0.08 }),
  broom_sweep: () => noise({ dur: 0.2, gain: 0.09, hp: 1600 }),
  scrub_loop: () => noise({ dur: 0.1, gain: 0.06, hp: 700 }),
  bubble_pop: () => tone({ freq: 900, dur: 0.07, type: 'sine', gain: 0.12, slide: 500 }),
  pass_part: () => tone({ freq: 700, dur: 0.08, type: 'triangle', gain: 0.11, slide: 260 }),
  drop_invalid: () => tone({ freq: 240, dur: 0.14, type: 'sawtooth', gain: 0.07, slide: -80 }),
  coin: () => { tone({ freq: 1180, dur: 0.07, type: 'square', gain: 0.07 }); tone({ freq: 1560, dur: 0.09, type: 'square', gain: 0.06, delay: 0.05 }); },
  pickup: () => tone({ freq: 520, dur: 0.06, type: 'triangle', gain: 0.09, slide: 200 }),
  timer_tick: () => tone({ freq: 1000, dur: 0.04, type: 'square', gain: 0.05 }),
  timer_warning: () => tone({ freq: 1320, dur: 0.08, type: 'square', gain: 0.08 }),
  enemy_spawn: () => tone({ freq: 180, dur: 0.2, type: 'sawtooth', gain: 0.07, slide: 90 }),
  enemy_defeat: () => { noise({ dur: 0.16, gain: 0.09, hp: 500 }); tone({ freq: 420, dur: 0.16, type: 'triangle', gain: 0.09, slide: 320 }); },
  round_start: () => { tone({ freq: 440, dur: 0.1, type: 'triangle' }); tone({ freq: 660, dur: 0.12, type: 'triangle', delay: 0.1 }); },
  round_win: () => [523, 659, 784, 1047].forEach((f, i) => tone({ freq: f, dur: 0.16, type: 'triangle', gain: 0.11, delay: i * 0.11 })),
  round_lose: () => [440, 392, 330].forEach((f, i) => tone({ freq: f, dur: 0.2, type: 'sine', gain: 0.1, delay: i * 0.14 })),
  reaction_good: () => tone({ freq: 990, dur: 0.1, type: 'sine', gain: 0.1, slide: 300 }),
  reaction_bad: () => tone({ freq: 260, dur: 0.16, type: 'sine', gain: 0.07, slide: -60 })
};

export function play(name) {
  const fn = SFX[name];
  if (fn) { try { fn(); } catch { /* audio is never a reason to break the loop */ } }
}

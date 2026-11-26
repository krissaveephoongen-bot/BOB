import { PAL, PX_PER_UNIT, WALK_SPEED_U } from './data.js';

const R = PX_PER_UNIT;

function roundRect(c, x, y, w, h, r) {
  const rr = Math.min(r, w / 2, h / 2);
  c.beginPath();
  c.moveTo(x + rr, y);
  c.arcTo(x + w, y, x + w, y + h, rr);
  c.arcTo(x + w, y + h, x, y + h, rr);
  c.arcTo(x, y + h, x, y, rr);
  c.arcTo(x, y, x + w, y, rr);
  c.closePath();
}

function blob(c, x, y, rx, ry, seed = 1) {
  c.beginPath();
  const steps = 26;
  for (let i = 0; i <= steps; i++) {
    const a = (i / steps) * Math.PI * 2;
    const wob = 1 + 0.09 * Math.sin(a * 3 + seed) + 0.05 * Math.cos(a * 5 - seed * 2);
    const px = x + Math.cos(a) * rx * wob;
    const py = y + Math.sin(a) * ry * wob;
    if (i === 0) c.moveTo(px, py); else c.lineTo(px, py);
  }
  c.closePath();
}

export class Island {
  constructor(canvas, { onStation, onStain } = {}) {
    this.cv = canvas;
    this.c = canvas.getContext('2d');
    this.onStation = onStation || (() => {});
    this.onStain = onStain || (() => {});
    this.hero = { x: 520, y: 700, tx: 520, y0: 700, ty: 700, bob: 0 };
    this.rival = { x: 1400, y: 700, bob: 0 };
    this.reduced = false;
    this.resize(1);
    canvas.addEventListener('pointerdown', (e) => this.tap(e));
  }

  resize(k) {
    const q = Math.min(2, Math.max(1, (window.devicePixelRatio || 1) * k));
    this.q = q;
    this.cv.width = Math.round(1920 * q);
    this.cv.height = Math.round(1080 * q);
  }

  toStage(e) {
    const r = this.cv.getBoundingClientRect();
    return { x: ((e.clientX - r.left) / r.width) * 1920, y: ((e.clientY - r.top) / r.height) * 1080 };
  }

  tap(e) {
    const p = this.toStage(e);
    for (const s of this.stations || []) {
      if (Math.hypot(s.x - p.x, s.y - p.y) < 1.5 * R) { this.onStation(s); return; }
    }
    for (const st of this.stains || []) {
      if (!st.done && Math.hypot(st.x - p.x, st.y - p.y) < 1.3 * R) { this.onStain(st); return; }
    }
    // Walking is clamped to the player's own island until the bridge drops.
    const open = this.bridgePhase === 'open';
    const lo = open ? 240 : 250;
    const hi = open ? 1680 : 900;
    this.hero.tx = Math.max(lo, Math.min(hi, p.x));
    this.hero.ty = Math.max(560, Math.min(880, p.y));
  }

  layout(plan, chapter) {
    const anchors = [
      { x: 400, y: 640 }, { x: 660, y: 590 }, { x: 560, y: 790 },
      { x: 300, y: 790 }, { x: 760, y: 720 }, { x: 420, y: 520 }
    ];
    const perStation = Math.ceil(plan.tasks.length / 3);
    this.stations = anchors.slice(0, 3).map((a, i) => ({
      key: `S${i + 1}_A`,
      x: a.x + (chapter >= 5 ? ((i * 37) % 60) - 30 : 0),
      y: a.y + (chapter >= 5 ? ((i * 53) % 40) - 20 : 0),
      tasks: plan.tasks.filter((_, k) => Math.floor(k / perStation) === i)
    }));
    this.stations.forEach((s) => { s.open = true; });
  }

  step(dt, clock) {
    const h = this.hero;
    const dx = h.tx - h.x;
    const dy = h.ty - h.y;
    const d = Math.hypot(dx, dy);
    const speed = WALK_SPEED_U * R * dt;
    if (d > 2) {
      const m = Math.min(1, speed / d);
      h.x += dx * m;
      h.y += dy * m;
      h.bob += dt * 9;
    } else {
      h.bob *= 0.86;
    }
    this.rival.bob += dt * (2 + Math.sin(clock * 0.7));
    for (const st of this.stains || []) {
      if (!st.done) st.life -= dt;
    }
  }

  draw(state) {
    const c = this.c;
    const { clock, bridgePhase, plan, rivalProgress, heroColor, rivalColor, band } = state;
    this.bridgePhase = bridgePhase;
    c.setTransform(this.q, 0, 0, this.q, 0, 0);
    c.clearRect(0, 0, 1920, 1080);

    const sky = c.createLinearGradient(0, 0, 0, 1080);
    sky.addColorStop(0, '#BFE4FF');
    sky.addColorStop(0.55, '#EAF6FF');
    sky.addColorStop(1, '#FFF3D8');
    c.fillStyle = sky;
    c.fillRect(0, 0, 1920, 1080);

    c.fillStyle = 'rgba(255,255,255,0.75)';
    for (let i = 0; i < 5; i++) {
      const x = ((i * 420 + clock * (12 + i * 3)) % 2200) - 200;
      blob(c, x, 150 + i * 46, 150, 34, i + 2);
      c.fill();
    }

    this.island(c, 1420, 700, 300, 108, '#9BD08A', rivalProgress, rivalColor, true, clock);
    this.bridge(c, bridgePhase, clock);
    this.island(c, 520, 720, 360, 132, '#ABDE98', rivalProgress, heroColor, false, clock);

    for (const s of this.stations || []) this.station(c, s, state);
    for (const st of this.stains || []) if (!st.done) this.stain(c, st);

    this.chibi(c, this.hero.x, this.hero.y, heroColor, this.hero.bob, 1);
    this.chibi(c, this.rival.x, this.rival.y - 20, rivalColor, Math.sin(this.rival.bob) * 2, 0.86);

    if (band >= 3) {
      c.fillStyle = 'rgba(4,30,79,0.5)';
      c.font = '500 26px Mitr, sans-serif';
      c.textAlign = 'left';
      c.fillText('ยาก · ทุกงาน +30% เวลา', 40, 1040);
    }
  }

  island(c, cx, cy, rx, ry, grass, progress, accent, isRival, clock) {
    c.save();
    c.fillStyle = 'rgba(40,30,60,0.16)';
    c.beginPath();
    c.ellipse(cx, cy + ry * 0.9, rx * 0.92, ry * 0.35, 0, 0, Math.PI * 2);
    c.fill();

    c.fillStyle = '#C89A6B';
    blob(c, cx, cy + 26, rx * 0.96, ry * 0.95, isRival ? 5 : 3);
    c.fill();
    c.fillStyle = '#A9784F';
    for (let i = 0; i < 4; i++) {
      c.beginPath();
      c.ellipse(cx - rx * 0.5 + i * rx * 0.33, cy + 54 + (i % 2) * 16, 40, 16, 0, 0, Math.PI * 2);
      c.fill();
    }

    const g = c.createLinearGradient(0, cy - ry, 0, cy + 30);
    g.addColorStop(0, grass);
    g.addColorStop(1, isRival ? '#7FBF76' : '#8FCE85');
    c.fillStyle = g;
    c.beginPath();
    c.ellipse(cx, cy - 6, rx, ry * 0.62, 0, 0, Math.PI * 2);
    c.fill();
    c.strokeStyle = 'rgba(74,46,34,0.35)';
    c.lineWidth = 3;
    c.stroke();

    // cloud sea under the floating island
    c.fillStyle = 'rgba(255,255,255,0.85)';
    for (let i = 0; i < 4; i++) {
      const wob = this.reduced ? 0 : Math.sin(clock * 0.8 + i) * 8;
      blob(c, cx - rx * 0.7 + i * rx * 0.47, cy + ry * 0.95 + wob, 92, 26, i + 7);
      c.fill();
    }

    // waterfall
    c.fillStyle = 'rgba(120,196,255,0.75)';
    roundRect(c, cx + rx * 0.42, cy + 20, 30, 150, 14);
    c.fill();
    c.fillStyle = 'rgba(255,255,255,0.6)';
    for (let i = 0; i < 4; i++) {
      const yy = cy + 30 + ((clock * 160 + i * 40) % 130);
      roundRect(c, cx + rx * 0.44, yy, 22, 8, 4);
      c.fill();
    }

    c.fillStyle = isRival ? 'rgba(251,47,68,0.9)' : 'rgba(246,196,49,0.95)';
    roundRect(c, cx - 130, cy - ry - 58, 260, 26, 13);
    c.fill();
    c.fillStyle = 'rgba(4,30,79,0.22)';
    roundRect(c, cx - 130, cy - ry - 58, 260 * (1 - Math.min(1, progress)), 26, 13);
    c.fill();
    c.fillStyle = PAL.ink;
    c.font = '600 24px Mitr, sans-serif';
    c.textAlign = 'center';
    c.fillText(isRival ? 'ทีมสะอาดเอี่ยม' : 'ทีมเรา', cx, cy - ry - 66);
    c.restore();
  }

  bridge(c, phase, clock) {
    const x0 = 830;
    const x1 = 1160;
    const y = 690;
    if (phase === 'down') {
      c.save();
      c.globalAlpha = 0.28;
      c.fillStyle = '#B79067';
      roundRect(c, x0, y - 6, x1 - x0, 12, 6);
      c.fill();
      c.restore();
      return;
    }
    const shake = phase === 'shake' && !this.reduced ? Math.sin(clock * 42) * 5 : 0;
    const drop = phase === 'shake' ? 0.55 : 1;
    c.save();
    c.translate(0, shake + (1 - drop) * 40);
    c.fillStyle = '#D9A76C';
    roundRect(c, x0, y - 22, x1 - x0, 44, 12);
    c.fill();
    c.strokeStyle = 'rgba(74,46,34,0.5)';
    c.lineWidth = 3;
    for (let i = 0; i < 8; i++) {
      c.beginPath();
      c.moveTo(x0 + i * 42, y - 22);
      c.lineTo(x0 + i * 42, y + 22);
      c.stroke();
    }
    c.fillStyle = '#8A5A3B';
    roundRect(c, x0, y - 30, x1 - x0, 8, 4);
    c.fill();
    c.restore();
  }

  station(c, s, state) {
    const near = Math.hypot(this.hero.x - s.x, this.hero.y - s.y) < 1.25 * 60;
    const left = s.tasks.filter((t) => !t.done).length;
    c.save();
    c.fillStyle = 'rgba(40,30,60,0.18)';
    c.beginPath();
    c.ellipse(s.x, s.y + 26, 58, 20, 0, 0, Math.PI * 2);
    c.fill();

    c.fillStyle = '#E7D3A8';
    c.beginPath();
    c.ellipse(s.x, s.y, 58, 26, 0, 0, Math.PI * 2);
    c.fill();
    c.strokeStyle = near ? PAL.gold : 'rgba(74,46,34,0.4)';
    c.lineWidth = near ? 6 : 3;
    c.stroke();

    // a stand with the tool family colour
    const catColor = left === 0 ? PAL.success : state.stationColor;
    c.fillStyle = catColor;
    roundRect(c, s.x - 30, s.y - 62, 60, 44, 12);
    c.fill();
    c.fillStyle = 'rgba(255,255,255,0.85)';
    c.font = '600 26px Mitr, sans-serif';
    c.textAlign = 'center';
    c.fillText(left === 0 ? '✓' : String(left), s.x, s.y - 30);

    c.fillStyle = PAL.ink;
    c.font = '500 22px Mitr, sans-serif';
    c.fillText(s.key, s.x, s.y + 56);
    if (near && left > 0) {
      const pulse = 1 + (this.reduced ? 0 : Math.sin(state.clock * 6) * 0.05);
      c.strokeStyle = 'rgba(246,196,49,0.9)';
      c.lineWidth = 4;
      c.beginPath();
      c.ellipse(s.x, s.y, 74 * pulse, 34 * pulse, 0, 0, Math.PI * 2);
      c.stroke();
    }
    c.restore();
    s.near = near && left > 0;
  }

  stain(c, st) {
    const frac = Math.max(0, Math.min(1, st.life / 25));
    c.save();
    c.fillStyle = 'rgba(138,82,38,0.9)';
    blob(c, st.x, st.y, 46, 26, st.seed);
    c.fill();
    c.strokeStyle = frac > 0.35 ? PAL.gold : PAL.danger;
    c.lineWidth = 7;
    c.beginPath();
    c.arc(st.x, st.y, 58, -Math.PI / 2, -Math.PI / 2 + Math.PI * 2 * frac);
    c.stroke();
    c.restore();
  }

  chibi(c, x, y, color, bob, scale = 1) {
    const lift = Math.abs(Math.sin(bob)) * (this.reduced ? 0 : 5);
    c.save();
    c.translate(x, y - lift);
    c.scale(scale, scale);
    c.fillStyle = 'rgba(40,30,60,0.2)';
    c.beginPath();
    c.ellipse(0, 44 + lift, 26, 9, 0, 0, Math.PI * 2);
    c.fill();

    c.lineWidth = 3;
    c.strokeStyle = 'rgba(74,46,34,0.55)';

    c.fillStyle = color;
    roundRect(c, -19, 2, 38, 34, 12);
    c.fill();
    c.stroke();

    c.fillStyle = PAL.skin;
    c.beginPath();
    c.arc(0, -22, 30, 0, Math.PI * 2);
    c.fill();
    c.stroke();

    c.fillStyle = '#4B2E1F';
    c.beginPath();
    c.arc(0, -34, 30, Math.PI, 0);
    c.fill();
    c.fillStyle = PAL.red;
    roundRect(c, -30, -40, 60, 11, 5);
    c.fill();

    c.fillStyle = PAL.ink;
    c.beginPath();
    c.arc(-10, -20, 4.4, 0, Math.PI * 2);
    c.arc(10, -20, 4.4, 0, Math.PI * 2);
    c.fill();
    c.fillStyle = 'rgba(255,255,255,0.9)';
    c.beginPath();
    c.arc(-8.4, -21.6, 1.6, 0, Math.PI * 2);
    c.arc(11.6, -21.6, 1.6, 0, Math.PI * 2);
    c.fill();
    c.strokeStyle = 'rgba(216,30,54,0.6)';
    c.lineWidth = 2.4;
    c.beginPath();
    c.arc(0, -12, 7, 0.2, Math.PI - 0.2);
    c.stroke();
    c.restore();
  }
}

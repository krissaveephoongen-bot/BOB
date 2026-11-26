import { PAL, TASKS, CAT, BINS } from './data.js';
import { play } from './audio.js';

const W = 1400;
const H = 860;
const AREA = { x: 80, y: 170, w: 1240, h: 620 };

function rr(c, x, y, w, h, r) {
  const k = Math.min(r, w / 2, h / 2);
  c.beginPath();
  c.moveTo(x + k, y);
  c.arcTo(x + w, y, x + w, y + h, k);
  c.arcTo(x + w, y + h, x, y + h, k);
  c.arcTo(x, y + h, x, y, k);
  c.arcTo(x, y, x + w, y, k);
  c.closePath();
}

class CardBase {
  constructor(task, opts) {
    this.task = task;
    this.o = opts;
    this.t = 0;
    this.left = task.seconds;
    this.done = false;
    this.parts = [];
    this.layer = opts.layer;
    this.root = document.createElement('div');
    this.root.className = 'card';
    this.root.innerHTML = `
      <div class="card-head">
        <div class="chip" style="--chip:${CAT[task.cat].color}">${CAT[task.cat].th}</div>
        <div class="card-title">${task.th} <span class="tid">${task.id}</span></div>
        <div class="card-goal"></div>
        <button class="ghost exit">ออก</button>
      </div>
      <canvas class="card-cv" width="${W}" height="${H}"></canvas>
      <div class="card-foot"><div class="bar"><i></i></div><div class="clock"></div></div>`;
    this.layer.appendChild(this.root);
    this.cv = this.root.querySelector('.card-cv');
    this.c = this.cv.getContext('2d');
    this.bar = this.root.querySelector('.bar i');
    this.clockEl = this.root.querySelector('.clock');
    this.goalEl = this.root.querySelector('.card-goal');
    this.root.querySelector('.exit').addEventListener('click', () => this.close(false, 'ออกกลางคัน'));
    this.p = { x: 0, y: 0, down: false };
    this.bind('pointerdown');
    this.layout();
    this.requestFit();
  }

  requestFit() {
    const q = Math.min(2, Math.max(1, window.devicePixelRatio || 1));
    this.cv.width = W * q;
    this.cv.height = H * q;
    this.q = q;
  }

  bind(downType) {
    const pos = (e) => {
      const r = this.cv.getBoundingClientRect();
      return { x: ((e.clientX - r.left) / r.width) * W, y: ((e.clientY - r.top) / r.height) * H };
    };
    this.cv.addEventListener(downType, (e) => {
      // Capture is best-effort: scripted input has no active pointer, and losing capture
      // must not lose the touch itself.
      try { this.cv.setPointerCapture(e.pointerId); } catch (_) {}
      this.p = { ...pos(e), down: true };
      this.onDown && this.onDown(this.p);
    });
    this.cv.addEventListener('pointermove', (e) => {
      this.p = { ...pos(e), down: this.p.down };
      this.onMove && this.onMove(this.p);
    });
    const up = (e) => {
      this.p.down = false;
      this.onUp && this.onUp({ ...pos(e), down: false });
    };
    this.cv.addEventListener('pointerup', up);
    this.cv.addEventListener('pointercancel', up);
  }

  spark(x, y, color = PAL.gold, n = 12) {
    if (this.o.reduced) n = Math.min(4, n);
    for (let i = 0; i < n; i++) {
      const a = Math.random() * Math.PI * 2;
      this.parts.push({ x, y, vx: Math.cos(a) * 220, vy: Math.sin(a) * 220 - 60, life: 0.55, max: 0.55, color });
    }
  }

  toast(text, tone = 'good') {
    this.msg = { text, tone, life: 1.5 };
  }

  progress() { return 0; }

  chrome() {
    const c = this.c;
    c.setTransform(this.q || 1, 0, 0, this.q || 1, 0, 0);
    c.clearRect(0, 0, W, H);
    const g = c.createLinearGradient(0, 0, 0, H);
    g.addColorStop(0, '#FFFDF5');
    g.addColorStop(1, '#FEEECF');
    c.fillStyle = g;
    rr(c, 0, 0, W, H, 44);
    c.fill();
    c.strokeStyle = 'rgba(46,68,132,0.18)';
    c.lineWidth = 5;
    c.stroke();
  }

  frame(dt) {
    this.t += dt;
    if (!this.done) {
      this.left -= dt;
      if (this.left <= 0) { this.left = 0; this.close(false, 'หมดเวลา'); return; }
      if (Math.ceil(this.left) !== this.lastSec) {
        this.lastSec = Math.ceil(this.left);
        if (this.left <= 5) play('timer_warning');
      }
    }
    const pr = Math.max(0, Math.min(1, this.progress()));
    this.bar.style.width = `${(pr * 100).toFixed(1)}%`;
    this.clockEl.textContent = `${this.left.toFixed(1)} วิ`;
    this.clockEl.classList.toggle('low', this.left <= 5);
    this.step && this.step(dt);
    this.paint(dt);
    if (pr >= 0.995 && !this.done) { this.done = true; play('round_win'); this.flash = 0.7; this.finalize && this.finalize(); }
    if (this.flash > 0) {
      this.flash -= dt;
      if (this.flash <= 0) this.close(true);
    }
  }

  paint(dt) {
    const c = this.c;
    this.chrome();
    this.draw && this.draw(c, dt);
    for (const p of this.parts) {
      p.life -= dt;
      p.x += p.vx * dt;
      p.y += p.vy * dt;
      p.vy += 520 * dt;
    }
    this.parts = this.parts.filter((p) => p.life > 0);
    for (const p of this.parts) {
      c.globalAlpha = Math.max(0, p.life / p.max);
      c.fillStyle = p.color;
      c.beginPath();
      c.arc(p.x, p.y, 7 * (p.life / p.max) + 2, 0, Math.PI * 2);
      c.fill();
    }
    c.globalAlpha = 1;
    if (this.flash > 0) {
      c.fillStyle = `rgba(61,220,132,${0.35 * (this.flash / 0.7)})`;
      rr(c, 0, 0, W, H, 44);
      c.fill();
      c.fillStyle = PAL.ink;
      c.font = '600 84px Mitr, sans-serif';
      c.textAlign = 'center';
      c.fillText('เรียบร้อย!', W / 2, H / 2);
    }
    if (this.msg && this.msg.life > 0) {
      this.msg.life -= dt;
      c.fillStyle = this.msg.tone === 'good' ? 'rgba(23,138,79,0.95)' : 'rgba(4,30,79,0.9)';
      const wpx = Math.max(320, this.msg.text.length * 26);
      rr(c, W / 2 - wpx / 2, H - 150, wpx, 86, 26);
      c.fill();
      c.fillStyle = '#fff';
      c.font = '500 40px Mitr, sans-serif';
      c.textAlign = 'center';
      c.fillText(this.msg.text, W / 2, H - 92);
    }
  }

  close(ok, why) {
    if (this.closed) return;
    this.closed = true;
    this.o.onDone({ task: this.task, ok, why });
    this.root.remove();
  }
}

class SweepPath extends CardBase {
  layout() {
    const n = this.task.goal;
    this.paths = [];
    for (let i = 0; i < n; i++) {
      const y0 = AREA.y + 120 + i * (AREA.h - 200) / Math.max(1, n);
      const pts = [];
      const segs = 64;
      const amp = 90 + i * 24;
      for (let s = 0; s <= segs; s++) {
        const u = s / segs;
        pts.push({
          x: AREA.x + 90 + u * (AREA.w - 180),
          y: y0 + Math.sin(u * Math.PI * (1.6 + i * 0.7)) * amp,
          clean: false
        });
      }
      this.paths.push(pts);
    }
    this.all = this.paths.flat();
    this.goalEl.textContent = `${n} เส้นทาง`;
  }

  sweep(p) {
    let hit = 0;
    for (const pt of this.all) {
      if (!pt.clean && Math.hypot(pt.x - p.x, pt.y - p.y) < 62) { pt.clean = true; hit++; }
    }
    if (hit) {
      this.spark(p.x, p.y, PAL.gold, 3);
      if (!this.noise || this.t - this.noise > 0.18) { this.noise = this.t; play('broom_sweep'); }
    }
  }

  onDown(p) { this.sweep(p); }
  onMove(p) { if (p.down) this.sweep(p); }
  progress() { return this.all.filter((p) => p.clean).length / this.all.length; }

  draw(c) {
    c.fillStyle = '#F3E3B5';
    rr(c, AREA.x, AREA.y, AREA.w, AREA.h, 28);
    c.fill();
    for (const pt of this.all) {
      c.fillStyle = pt.clean ? 'rgba(171,222,152,0.5)' : 'rgba(138,82,38,0.55)';
      c.beginPath();
      c.arc(pt.x, pt.y, pt.clean ? 26 : 34, 0, Math.PI * 2);
      c.fill();
    }
    for (const pt of this.all) {
      if (pt.clean) continue;
      c.fillStyle = 'rgba(122,63,184,0.35)';
      c.beginPath();
      c.arc(pt.x + 8, pt.y - 6, 6, 0, Math.PI * 2);
      c.fill();
    }
    c.strokeStyle = 'rgba(4,30,79,0.35)';
    c.setLineDash([16, 16]);
    c.lineWidth = 4;
    for (const path of this.paths) {
      c.beginPath();
      path.forEach((pt, i) => (i ? c.lineTo(pt.x, pt.y) : c.moveTo(pt.x, pt.y)));
      c.stroke();
    }
    c.setLineDash([]);
    if (this.p.x) {
      c.save();
      c.translate(this.p.x, this.p.y);
      c.rotate(-0.5);
      c.fillStyle = '#8A5A3B';
      rr(c, -8, -110, 16, 110, 8);
      c.fill();
      c.fillStyle = PAL.gold;
      rr(c, -52, -8, 104, 34, 12);
      c.fill();
      c.restore();
    }
  }
}

class ScrubCircle extends CardBase {
  layout() {
    this.cols = 22;
    this.rows = 12;
    this.cw = (AREA.w - 200) / this.cols;
    this.ch = (AREA.h - 160) / this.rows;
    this.x0 = AREA.x + 100;
    this.y0 = AREA.y + 80;
    this.cells = [];
    const cx = AREA.x + AREA.w / 2;
    const cy = AREA.y + AREA.h / 2;
    for (let r = 0; r < this.rows; r++) {
      for (let cIdx = 0; cIdx < this.cols; cIdx++) {
        const x = this.x0 + (cIdx + 0.5) * this.cw;
        const y = this.y0 + (r + 0.5) * this.ch;
        const e = Math.hypot((x - cx) / (AREA.w * 0.36), (y - cy) / (AREA.h * 0.38));
        if (e < 1) this.cells.push({ x, y, clean: false, wobble: (cIdx * 7 + r * 13) % 5 });
      }
    }
    this.moved = 0;
    this.need = this.task.goal;
    this.goalEl.textContent = `เป้า ${this.need} u · ${this.cells.length} จุด`;
  }

  scrub(p) {
    if (!this.prev) { this.prev = p; return; }
    const d = Math.hypot(p.x - this.prev.x, p.y - this.prev.y);
    if (d > 1) {
      this.moved += d / 3; // 3 design px = 1 world unit, same mapping as the island
      let hit = false;
      for (const cell of this.cells) {
        if (!cell.clean && Math.hypot(cell.x - p.x, cell.y - p.y) < 62) { cell.clean = true; hit = true; }
      }
      if (hit) this.spark(p.x, p.y, PAL.chem, 4);
      if (!this.noise || this.t - this.noise > 0.16) { this.noise = this.t; play('scrub_loop'); }
    }
    this.prev = p;
  }

  onDown(p) { this.prev = p; }
  onMove(p) { if (p.down) this.scrub(p); }
  progress() {
    const covered = this.cells.filter((x) => x.clean).length / this.cells.length;
    const dist = Math.min(1, this.moved / this.need);
    return Math.min(covered, dist);
  }

  draw(c) {
    c.fillStyle = '#EAF2FF';
    rr(c, AREA.x, AREA.y, AREA.w, AREA.h, 28);
    c.fill();
    c.strokeStyle = 'rgba(46,68,132,0.12)';
    c.lineWidth = 2;
    for (let x = AREA.x; x < AREA.x + AREA.w; x += 60) { c.beginPath(); c.moveTo(x, AREA.y); c.lineTo(x, AREA.y + AREA.h); c.stroke(); }
    for (let y = AREA.y; y < AREA.y + AREA.h; y += 60) { c.beginPath(); c.moveTo(AREA.x, y); c.lineTo(AREA.x + AREA.w, y); c.stroke(); }
    for (const cell of this.cells) {
      c.fillStyle = cell.clean ? 'rgba(171,222,152,0.75)' : `rgba(138,82,38,${0.62 + cell.wobble * 0.04})`;
      c.beginPath();
      c.arc(cell.x, cell.y, cell.clean ? this.cw * 0.5 : this.cw * 0.62, 0, Math.PI * 2);
      c.fill();
    }
    c.strokeStyle = 'rgba(26,111,196,0.5)';
    c.lineWidth = 6;
    c.setLineDash([18, 22]);
    c.beginPath();
    c.arc(AREA.x + AREA.w / 2, AREA.y + AREA.h / 2, AREA.h * 0.34, 0, Math.PI * 2);
    c.stroke();
    c.setLineDash([]);
    c.fillStyle = PAL.ink;
    c.font = '500 30px Mitr, sans-serif';
    c.textAlign = 'left';
    c.fillText(`ระยะถู ${Math.round(this.moved)} / ${this.need} u`, AREA.x + 24, AREA.y + 46);
    if (this.p.x) {
      c.fillStyle = PAL.secondary;
      c.beginPath();
      c.arc(this.p.x, this.p.y, 44, 0, Math.PI * 2);
      c.fill();
      c.fillStyle = 'rgba(255,255,255,0.7)';
      c.beginPath();
      c.arc(this.p.x - 12, this.p.y - 12, 14, 0, Math.PI * 2);
      c.fill();
    }
  }
}

class SortBins extends CardBase {
  layout() {
    const binCount = this.task.band >= 3 ? 4 : 3;
    this.bins = BINS.slice(0, binCount);
    const pool = [];
    for (const b of this.bins) for (const it of b.items) pool.push({ th: it, bin: b.key });
    const want = this.task.goal;
    const picked = [];
    for (let i = 0; i < want; i++) picked.push(pool[(i * 7 + this.task.band * 3) % pool.length]);
    this.items = picked.map((p, i) => ({ ...p, id: i, x: 0, y: 0, home: i, done: false, drag: false }));
    this.placeHome();
    this.goalEl.textContent = `${want} ชิ้น · ${binCount} ถัง`;
  }

  placeHome() {
    const live = this.items.filter((i) => !i.done);
    const gap = Math.min(210, (AREA.w - 120) / Math.max(1, live.length));
    live.forEach((it, idx) => {
      if (!it.drag) {
        it.x = AREA.x + 80 + idx * gap + gap * 0.5;
        it.y = AREA.y + 120;
        it.hx = it.x;
        it.hy = it.y;
      }
    });
  }

  hitItem(p) {
    return this.items.find((it) => !it.done && Math.abs(it.x - p.x) < 100 && Math.abs(it.y - p.y) < 62);
  }

  onDown(p) {
    const it = this.hitItem(p);
    if (!it) return;
    it.drag = true;
    this.dragId = it.id;
    play('pickup');
  }

  onMove(p) {
    const it = this.items.find((x) => x.id === this.dragId && x.drag);
    if (it) { it.x = p.x; it.y = p.y; }
  }

  onUp() {
    const it = this.items.find((x) => x.id === this.dragId && x.drag);
    if (!it) return;
    it.drag = false;
    this.dragId = null;
    const n = this.bins.length;
    let idx = -1;
    if (it.y > AREA.y + AREA.h - 260) {
      idx = this.bins.findIndex((_, i) => {
        const cx = AREA.x + AREA.w / 2 + (i - (n - 1) / 2) * 340;
        return Math.abs(cx - it.x) < 170;
      });
    }
    if (idx === -1) { it.x = it.hx; it.y = it.hy; return; }
    const target = this.bins[idx];
    if (target.key === it.bin) {
      it.done = true;
      this.spark(it.x, it.y, target.color, 16);
      play('pass_part');
      this.placeHome();
    } else {
      play('drop_invalid');
      this.toast('ยังไม่ถูก · ลองใหม่', 'bad');
      it.x = it.hx;
      it.y = it.hy;
    }
  }

  progress() { return this.items.filter((i) => i.done).length / this.items.length; }

  draw(c) {
    c.fillStyle = '#FFF8E7';
    rr(c, AREA.x, AREA.y, AREA.w, AREA.h, 28);
    c.fill();
    const n = this.bins.length;
    this.bins.forEach((b, i) => {
      const cx = AREA.x + AREA.w / 2 + (i - (n - 1) / 2) * 340;
      const by = AREA.y + AREA.h - 210;
      c.fillStyle = b.color;
      rr(c, cx - 150, by, 300, 180, 26);
      c.fill();
      c.fillStyle = 'rgba(255,255,255,0.28)';
      rr(c, cx - 150, by, 300, 34, 18);
      c.fill();
      c.fillStyle = '#fff';
      c.font = '600 34px Mitr, sans-serif';
      c.textAlign = 'center';
      c.fillText(b.th, cx, by + 106);
      const left = this.items.filter((x) => !x.done && x.bin === b.key).length;
      c.fillStyle = 'rgba(4,30,79,0.75)';
      c.font = '500 26px Mitr, sans-serif';
      c.fillText(`เหลือ ${left}`, cx, by + 148);
    });
    for (const it of this.items) {
      if (it.done) continue;
      c.save();
      c.translate(it.x, it.y);
      c.fillStyle = it.drag ? PAL.gold2 : '#fff';
      c.strokeStyle = 'rgba(46,68,132,0.35)';
      c.lineWidth = 4;
      rr(c, -100, -50, 200, 100, 22);
      c.fill();
      c.stroke();
      c.fillStyle = PAL.ink;
      c.font = '500 30px Mitr, sans-serif';
      c.textAlign = 'center';
      c.fillText(it.th, 0, 10);
      c.restore();
    }
  }
}

const IMPL = { T01: SweepPath, T02: ScrubCircle, T05: SortBins };

export function openCard(task, opts) {
  const Ctor = IMPL[task.id] || IMPL.T01;
  return new Ctor({ ...task, cat: task.cat || TASKS[task.id].cat }, opts);
}

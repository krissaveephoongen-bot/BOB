import { STR, HEROES, CHAPTERS, TASKS, CAT, REF, PAL } from './data.js';
import * as Rules from './rules.js';
import { loadSave, writeSave } from './save.js';
import { Island } from './island.js';
import { openCard } from './cards.js';
import { play, setAudioEnabled, unlockAudio } from './audio.js';

const $ = (s) => document.querySelector(s);
const stage = $('#stage');
const screens = {
  splash: $('#s-splash'),
  home: $('#s-home'),
  chapters: $('#s-chapters'),
  heroes: $('#s-heroes'),
  race: $('#s-race')
};
const overlay = $('#overlay');
const cardLayer = $('#card-layer');

const app = {
  save: loadSave(),
  screen: 'splash',
  chapter: null,
  hero: HEROES.find((h) => h.id === loadSave().char) || HEROES[0],
  race: null,
  card: null,
  paused: false,
  clock: 0
};

function fit() {
  const k = Math.min(window.innerWidth / REF.w, window.innerHeight / REF.h);
  document.documentElement.style.setProperty('--k', String(k));
  $('#rotate').hidden = window.innerWidth >= window.innerHeight;
  if (island) island.resize(k);
  lastK = k;
}
let lastK = 1;

const island = new Island($('#scene'), {
  onStation: (s) => { if (app.screen === 'race' && !app.paused && !app.card) tryWork(s); },
  onStain: (st) => {
    if (st.done) return;
    st.done = true;
    app.race.stainsCleared += 1;
    play('enemy_defeat');
    toast('เคลียร์คราบฉุกเฉินได้');
  }
});

function show(name) {
  app.screen = name;
  Object.entries(screens).forEach(([k, el]) => el.classList.toggle('on', k === name));
  overlay.hidden = true;
  overlay.innerHTML = '';
}

function toast(text, tone = 'good') {
  const el = document.createElement('div');
  el.className = `toast ${tone}`;
  el.textContent = text;
  $('#toasts').appendChild(el);
  setTimeout(() => el.classList.add('out'), 1400);
  setTimeout(() => el.remove(), 1900);
}

/* ---------- menus ---------- */

function renderHome() {
  const s = app.save;
  $('#home-stats').innerHTML = `
    <div class="pill coins">เหรียญ <b>${s.coins}</b></div>
    <div class="pill lv">LV <b>${s.level}</b><span class="xpbar"><i style="width:${(s.xp % Rules.XP_PER_LEVEL) / Rules.XP_PER_LEVEL * 100}%"></i></span></div>
    <div class="pill">คะแนนสูงสุด <b>${s.taskRace.best}</b></div>`;
  $('#home-hero').textContent = `มือกวาด: ${app.hero.th} (${CAT[app.hero.cat].th})`;
  $('#play').onclick = () => { unlockAudio(); play('ui_confirm'); renderChapters(); show('chapters'); };
  $('#to-heroes').onclick = () => { renderHeroes(); show('heroes'); };
  $('#dev').onclick = () => openDev();
  $('#sound').onclick = (e) => {
    app.save.sound = app.save.sound ? 0 : 1;
    setAudioEnabled(!!app.save.sound);
    e.currentTarget.textContent = app.save.sound ? 'เสียง: เปิด' : 'เสียง: ปิด';
    writeSave(app.save);
  };
  $('#sound').textContent = app.save.sound ? 'เสียง: เปิด' : 'เสียง: ปิด';
}

function renderChapters() {
  const grid = $('#chapter-grid');
  grid.innerHTML = '';
  CHAPTERS.forEach((ch) => {
    const open = Rules.chapterUnlocked(ch, app.save);
    const stars = app.save.stars[ch.n] || 0;
    const el = document.createElement('button');
    el.className = `chap${open ? '' : ' locked'}${app.save.cleared[ch.n] ? ' cleared' : ''}`;
    el.innerHTML = `
      <span class="chip" style="--chip:${['#F4A340', '#4EA8DE', '#9B7EDE'][ch.band - 1]}">D${ch.band}</span>
      <span class="cnum">บท ${ch.n}</span>
      <span class="ctitle">${ch.th}</span>
      <span class="cnote">${ch.note || `เวลา ${Rules.roundSeconds(ch.n)} วิ`}</span>
      <span class="cstars">${'★'.repeat(stars)}${'☆'.repeat(3 - stars)}</span>
      ${open ? '' : `<span class="lock">ต้องใช้ LV ${ch.lv} + ≥2 ดาวบท ${ch.n - 1}</span>`}`;
    if (open) el.onclick = () => { play('ui_confirm'); startMatch(ch); };
    grid.appendChild(el);
  });
  $('#ch-back').onclick = () => { renderHome(); show('home'); };
}

function renderHeroes() {
  const row = $('#hero-row');
  row.innerHTML = '';
  HEROES.forEach((h) => {
    const b = document.createElement('button');
    b.className = `hero${h.id === app.hero.id ? ' sel' : ''}`;
    b.style.setProperty('--h', h.color);
    b.innerHTML = `<span class="face"></span><span class="hname">${h.th}</span><span class="hid">${h.en} · ${CAT[h.cat].th}</span>
      <span class="hab">${h.ability}<em>${h.abilityNote}</em></span>`;
    b.onclick = () => {
      app.hero = h;
      app.save.char = h.id;
      writeSave(app.save);
      play('ui_tap');
      renderHeroes();
    };
    row.appendChild(b);
  });
  $('#hero-back').onclick = () => { renderHome(); show('home'); };
}

/* ---------- match ---------- */

function startMatch(chapter) {
  app.chapter = chapter;
  app.race = {
    chapter,
    band: chapter.band,
    seconds: Rules.roundSeconds(chapter.n),
    baseSeed: (Date.now() % 100000) + chapter.n * 7,
    roundIndex: 0,
    wins: { me: 0, rival: 0 },
    roundsLost: 0,
    bestMargin: 0,
    totalTasks: 0,
    stainsCleared: 0,
    stainsMissed: 0,
    lastRound: null
  };
  $('#hud').hidden = false;
  beginRound();
  show('race');
}

function beginRound() {
  const r = app.race;
  r.roundIndex += 1;
  r.seed = r.baseSeed + r.roundIndex * 131;
  const rng = Rules.mulberry32(r.seed);
  r.plan = Rules.buildPlan({ seed: r.seed, band: r.band, players: 1 });
  r.rivalAt = Rules.buildRivalSchedule({ seed: r.seed, planCount: r.plan.tasks.length, seconds: r.seconds, band: r.band });
  r.clock = 0;
  r.myTasks = 0;
  r.tailwindUsed = false;
  r.stains = [];
  r.stainIdx = 0;
  r.open = false;
  r.sabUsed = false;
  r.ended = false;
  island.layout(r.plan, r.chapter.n);
  island.hero.x = 520; island.hero.y = 780; island.hero.tx = 520; island.hero.ty = 780;
  island.stains = r.stains;
  play('round_start');
}

function rivalProgress() {
  const r = app.race;
  return r.rivalAt.filter((t) => t <= r.clock).length / r.plan.tasks.length;
}

// A stain on top of a station is unreachable: station hit-testing runs first in island.tap().
function stainSpot(rng, stations) {
  let p = { x: 520, y: 700 };
  for (let i = 0; i < 24; i++) {
    p = { x: 300 + rng() * 460, y: 560 + rng() * 280 };
    if (!stations.some((s) => Math.hypot(s.x - p.x, s.y - p.y) < 130)) break;
  }
  return p;
}

function tryWork(station) {
  const next = station.tasks.find((t) => !t.done);
  if (!next) { toast('สถานีนี้เสร็จแล้ว'); return; }
  openTask(next);
}

function openTask(task) {
  const r = app.race;
  if (app.card) return;
  const gap = rivalProgress() - (r.myTasks / r.plan.tasks.length);
  let seconds = task.seconds;
  if (gap >= Rules.TAILWIND_GAP && !r.tailwindUsed) {
    r.tailwindUsed = true;
    seconds += 6;
    toast('ลมช่วย · +6 วิ งานนี้');
  }
  const t = { ...task, seconds, cat: TASKS[task.id].cat, th: TASKS[task.id].th, band: r.band, goal: TASKS[task.id].goal[r.band - 1] };
  play('ui_confirm');
  app.card = openCard(t, {
    layer: cardLayer,
    reduced: !!app.save.reduceMotion,
    onDone: ({ ok }) => {
      app.card = null;
      if (ok) {
        task.done = true;
        r.myTasks += 1;
        r.totalTasks += 1;
        play('coin');
      } else {
        play('reaction_bad');
        toast('หมดเวลา · เสียแค่เวลา ลองใหม่');
      }
      renderHud();
    }
  });
}

function endRound() {
  const r = app.race;
  if (r.ended) return;
  r.ended = true;
  const mine = r.myTasks / r.plan.tasks.length;
  const theirs = rivalProgress();
  const margin = r.myTasks - Math.round(theirs * r.plan.tasks.length);
  const outcome = mine > theirs ? 'win' : mine < theirs ? 'loss' : 'draw';
  if (outcome === 'win') { r.wins.me += 1; r.bestMargin = Math.max(r.bestMargin, margin); }
  else if (outcome === 'loss') { r.wins.rival += 1; r.roundsLost += 1; }
  else { r.wins.me += 1; }
  const series = Rules.seriesOutcome(r.wins.me, r.wins.rival);
  const secs = Math.max(0, r.seconds - r.clock);
  const points = Rules.scoreOf({ tasks: r.myTasks, cartConditions: 0, secondsLeft: Math.round(secs), outcome, band: r.band });
  r.lastRound = { round: r.roundIndex, outcome, margin, points, mine, theirs };
  play(outcome === 'win' ? 'round_win' : 'round_lose');
  if (series === 'ongoing') return roundResultCard(series);
  return chapterResultCard(series);
}

function panel(html, buttons) {
  overlay.hidden = false;
  overlay.innerHTML = `<div class="panel">${html}</div>`;
  const p = overlay.querySelector('.panel');
  buttons.forEach(([label, fn, kind]) => {
    const b = document.createElement('button');
    b.className = `big ${kind || ''}`;
    b.textContent = label;
    b.onclick = () => { play('ui_confirm'); overlay.hidden = true; overlay.innerHTML = ''; fn(); };
    p.appendChild(b);
  });
}

function roundResultCard() {
  const r = app.race;
  const L = r.lastRound;
  panel(`
    <div class="kicker">${STR.round} ${r.roundIndex}/3</div>
    <h2>${L.outcome === 'win' ? 'รอบนี้เราชนะ' : 'รอบนี้ยังแพ้'}</h2>
    <p class="lead">${STR.bestOf3} · ${r.wins.me} – ${r.wins.rival}</p>
    <p>ความคืบหน้าเรา ${(L.mine * 100) | 0}% · ทีมสะอาดเอี่ยม ${(L.theirs * 100) | 0}% · คะแนนรอบนี้ ${L.points}</p>`,
    [['รอบถัดไป', () => beginRound()]]);
}

function chapterResultCard(series) {
  const r = app.race;
  const won = series === 'win';
  const stars = Rules.starsOf({ wonChapter: won, roundsLost: r.roundsLost, bestTaskMargin: r.bestMargin });
  const prevStars = app.save.stars[r.chapter.n] || 0;
  const firstClear = won && !app.save.cleared[r.chapter.n];
  const points = r.lastRound.points;
  const coins = Rules.coinsOf({ outcome: won ? 'win' : 'loss', points, tasks: r.totalTasks });
  const xp = Rules.xpOf({ outcome: won ? 'win' : 'loss', tasks: r.totalTasks, firstClear });
  const beforeLevel = app.save.level;
  app.save.coins += coins;
  app.save.xp += xp;
  app.save.level = Rules.levelOf(app.save.xp);
  app.save.stars[r.chapter.n] = Math.max(prevStars, stars);
  if (won) app.save.cleared[r.chapter.n] = true;
  app.save.taskRace.best = Math.max(app.save.taskRace.best, points);
  app.save.taskRace.matches += 1;
  if (won) app.save.taskRace.wins += 1;
  app.save.board = [...app.save.board, { p: points, ch: r.chapter.n, d: new Date().toISOString().slice(0, 10) }]
    .sort((a, b) => b.p - a.p).slice(0, 10);
  writeSave(app.save);

  panel(`
    <div class="kicker">บท ${r.chapter.n} · ${r.chapter.th}</div>
    <h2>${won ? STR.winChapter : STR.notYet}</h2>
    <p class="stars">${'★'.repeat(stars)}${'☆'.repeat(3 - stars)}</p>
    <ul class="rows">
      <li>ซีรี่ส์<b>${r.wins.me} – ${r.wins.rival}</b></li>
      <li>งานที่เสร็จ<b>${r.totalTasks}</b></li>
      <li>คราบฉุกเฉิน: เคลียร์ ${r.stainsCleared} · หลุด ${r.stainsMissed}</li>
      <li>เหรียญ<b>+${coins}</b></li>
      <li>XP<b>+${xp}</b></li>
      <li>เลเวล<b>${beforeLevel} → ${app.save.level}</b></li>
      <li>คะแนนรอบสุดท้าย<b>${points}</b></li>
    </ul>
    <p class="fine">ดาว = ผ่านบท + ไม่แพ้รอบไหน + ชนะรอบขาด ≥3 งาน · ยังไม่แตะกติกาจริงของ T04/T13</p>`,
    [
      [STR.back, () => { renderHome(); show('home'); }, 'ghost'],
      ...(won && r.chapter.n < 6 ? [['บทต่อไป', () => { startMatch(CHAPTERS[r.chapter.n]); }, '']] : []),
      [STR.retry, () => startMatch(r.chapter)]
    ]);
}

/* ---------- hud ---------- */

function renderHud() {
  const r = app.race;
  if (!r) return;
  const mine = r.myTasks / r.plan.tasks.length;
  const theirs = rivalProgress();
  $('#hud-me b').textContent = `${(mine * 100) | 0}%`;
  $('#hud-rival b').textContent = `${(theirs * 100) | 0}%`;
  $('#bar-me').style.width = `${mine * 100}%`;
  $('#bar-rival').style.width = `${theirs * 100}%`;
  $('#clock').textContent = `${Math.max(0, Math.ceil(r.seconds - r.clock))}`;
  $('#series').textContent = `ซีรี่ส์ ${r.wins.me}–${r.wins.rival} · รอบ ${r.roundIndex}`;
  $('#stains').textContent = `คราบฉุกเฉิน ${r.stainsCleared}✓ ${r.stainsMissed}✗`;
  const left = r.plan.tasks.filter((t) => !t.done);
  $('#queue').innerHTML = left.slice(0, 6).map((t) =>
    `<div class="qrow"><span class="dot" style="--chip:${CAT[TASKS[t.id].cat].color}"></span>${t.id} ${TASKS[t.id].th}<em>${t.station + 1}</em></div>`).join('')
    || '<div class="qrow done">งานเสร็จครบแล้ว!</div>';
  const near = island.stations?.find((s) => s.near);
  $('#work').hidden = !near || !!app.card;
  $('#work').textContent = near ? `ทำงาน · ${near.key}` : '';
  $('#work').dataset.key = near ? near.key : '';
  $('#sab').hidden = !r.open || !!app.card || r.sabUsed;
}

/* ---------- loop ---------- */

let prev = performance.now();
function frame(now) {
  const dt = Math.min(0.05, (now - prev) / 1000);
  prev = now;
  app.clock += dt;
  if (app.screen === 'race' && !app.paused) tick(dt);
  if (app.card && !app.paused) app.card.frame(dt);
  if (app.screen === 'race') {
    const r = app.race;
    island.step(app.card ? 0 : dt, app.clock);
    island.draw({
      clock: app.clock,
      bridgePhase: !r ? 'down' : r.clock >= Rules.bridgeShakeAt(r.chapter.n) && !r.open ? 'shake' : r.open ? 'open' : 'down',
      plan: r?.plan,
      rivalProgress: r ? rivalProgress() : 0,
      heroColor: app.hero.color,
      rivalColor: '#FB2F44',
      stationColor: PAL.gold,
      band: r?.band || 1
    });
  }
  requestAnimationFrame(frame);
}

function tick(dt) {
  const r = app.race;
  if (!r || !overlay.hidden) return;
  const before = r.clock;
  r.clock += dt;
  const shake = Rules.bridgeShakeAt(r.chapter.n);
  const drop = Rules.bridgeDropAt(r.chapter.n);
  if (!r.open && r.clock >= drop) {
    r.open = true;
    play('ready_on');
    toast(STR.bridgeDown);
  } else if (r.open === false && before < shake && r.clock >= shake) {
    play('timer_warning');
    toast(STR.bridgeShake, 'warn');
  }
  while (r.stainIdx < r.plan.stainAt.length && r.clock >= r.plan.stainAt[r.stainIdx]) {
    const rng = Rules.mulberry32(r.seed + r.stainIdx * 17);
    const spot = stainSpot(rng, island.stations || []);
    r.stains.push({
      x: spot.x,
      y: spot.y,
      life: 25,
      seed: rng() * 6,
      done: false
    });
    island.stains = r.stains;
    play('enemy_spawn');
    r.stainIdx += 1;
  }
  for (const st of r.stains) {
    if (!st.done && st.life <= 0) { st.done = true; r.stainsMissed += 1; play('reaction_bad'); }
  }
  if (Math.ceil(r.seconds - r.clock) <= 10 && Math.ceil(r.seconds - r.clock) !== r.lastTick) {
    r.lastTick = Math.ceil(r.seconds - r.clock);
    if (r.lastTick > 0) play('timer_tick');
  }
  renderHud();
  if (r.myTasks >= r.plan.tasks.length || rivalProgress() >= 1 || r.clock >= r.seconds) endRound();
}

/* ---------- dev / self check ---------- */

function openDev() {
  const checks = [
    ['จำนวนงานต่อรอบ 1/2/3/4 คน = 6/7/9/11',
      [1, 2, 3, 4].map((p) => Rules.buildPlan({ seed: 1, band: 1, players: p }).tasks.length).join(',') === '6,7,9,11'],
    ['เวลางาน ×(1+0.15·(band−1)) เหมือน DifficultyStep → T02 = 24/28/31 (Unity 24/27.6/31.2 ปัดเป็นวินาที)',
      [1, 2, 3].map((b) => Rules.taskSeconds('T02', b)).join(',') === '24,28,31'],
    ['เวลา รอบ 120 · บท 5 = 90',
      Rules.roundSeconds(1) === 120 && Rules.roundSeconds(5) === 90],
    ['สะพาน 65/60 · บท 5 = 50/45',
      Rules.bridgeShakeAt(1) === 65 && Rules.bridgeDropAt(1) === 60 && Rules.bridgeShakeAt(5) === 50 && Rules.bridgeDropAt(5) === 45],
    ['คะแนน 8 งาน/4 เงื่อนไข/60 วิ/ชนะ/D1 = 1620',
      Rules.scoreOf({ tasks: 8, cartConditions: 4, secondsLeft: 60, outcome: 'win', band: 1 }) === 1620],
    ['ดาว 3 = ผ่านบท + ไม่แพ้รอบ + ชนะขาด ≥3',
      Rules.starsOf({ wonChapter: true, roundsLost: 0, bestTaskMargin: 3 }) === 3
      && Rules.starsOf({ wonChapter: true, roundsLost: 1, bestTaskMargin: 1 }) === 1
      && Rules.starsOf({ wonChapter: false, roundsLost: 1, bestTaskMargin: 0 }) === 0],
    ['best-of-3 ปิดที่ 2 ชิง',
      Rules.seriesOutcome(2, 0) === 'win' && Rules.seriesOutcome(1, 1) === 'ongoing'],
    ['แผนรอบ seed เดิม → งานชุดเดิม',
      JSON.stringify(Rules.buildPlan({ seed: 42, band: 2 }).tasks.map((t) => t.id))
      === JSON.stringify(Rules.buildPlan({ seed: 42, band: 2 }).tasks.map((t) => t.id))],
    ['คราบฉุกเฉิน 2 ที่ D1–D2, 3 ที่ D3',
      Rules.buildPlan({ seed: 3, band: 1 }).stains === 2 && Rules.buildPlan({ seed: 3, band: 3 }).stains === 3]
  ];
  panel(`
    <div class="kicker">Rules self-check</div>
    <h2>ตัวเลขตามสเปก</h2>
    <ul class="rows checks">${checks.map(([label, ok]) => `<li class="${ok ? 'ok' : 'no'}">${ok ? 'ผ่าน' : 'ไม่ผ่าน'}<b>${label}</b></li>`).join('')}</ul>
    <p class="fine">รันในเบราว์เซอร์ ไม่ต้องมีเซิร์ฟเวอร์ — ใช้ยืนยันว่า logic ย้ายจาก Unity มาได้ตรงตัวเลข
    · สิ่งที่ยังไม่มีในสไปค์: T04 รถเข็น, T13 สองคน, ออนไลน์, UGS, เสียงไฟล์จริง (ใช้ WebAudio สังเคราะห์)</p>`,
    [['ปิด', () => {}]]);
}

/* ---------- boot ---------- */

$('#work').onclick = (e) => {
  const s = island.stations?.find((x) => x.key === e.currentTarget.dataset.key);
  if (s) tryWork(s);
};
$('#sab').onclick = () => {
  const r = app.race;
  if (!r?.open || r.sabUsed) return;
  r.sabUsed = true;
  r.rivalAt.push(Math.min(r.seconds - 0.5, r.clock + 12));
  r.rivalAt.sort((a, b) => a - b);
  play('reaction_good');
  toast('ส่งคราบไปฝั่งโน้น · คู่แข่งช้าลง 1 จังหวะ');
  renderHud();
};
$('#pause').onclick = () => {
  app.paused = true;
  panel('<div class="kicker">พัก</div><h2>พักเกม</h2><p class="lead">เวลาแข่งหยุดนิ่ง ไม่มีอะไรเสียระหว่างพัก</p>',
    [['เล่นต่อ', () => { app.paused = false; }],
     ['เริ่มรอบใหม่', () => { app.paused = false; beginRound(); }],
     ['ออกจากบท', () => { app.paused = false; app.race = null; $('#hud').hidden = true; renderHome(); show('home'); }]]);
};
document.addEventListener('keydown', (e) => {
  if (e.key === 'Escape' && app.card) { app.card.close(false, 'ออก'); }
});
window.addEventListener('resize', fit);

setAudioEnabled(!!app.save.sound);
fit();
window.BOB = { app, island, rules: Rules, startMatch, openTask, toast, chapters: CHAPTERS };
renderHome();
show('splash');
setTimeout(() => { if (app.screen === 'splash') show('home'); }, 1600);
requestAnimationFrame(frame);

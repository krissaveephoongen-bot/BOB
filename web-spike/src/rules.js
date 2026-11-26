import { TASKS, TASK_POOL } from './data.js';

export function mulberry32(seed) {
  let a = seed >>> 0;
  return function next() {
    a = (a + 0x6D2B79F5) >>> 0;
    let t = a;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

export function shuffle(rng, arr) {
  const out = arr.slice();
  for (let i = out.length - 1; i > 0; i--) {
    const j = Math.floor(rng() * (i + 1));
    [out[i], out[j]] = [out[j], out[i]];
  }
  return out;
}

export const bandScale = (band) => 1 + 0.15 * (band - 1);
export const roundSeconds = (chapter) => (chapter === 5 ? 90 : 120);
export const bridgeDropAt = (chapter) => (chapter === 5 ? 45 : 60);
export const bridgeShakeAt = (chapter) => (chapter === 5 ? 50 : 65);
export const taskSeconds = (taskId, band) => Math.round(TASKS[taskId].base * bandScale(band));
export const taskGoal = (taskId, band) => TASKS[taskId].goal[band - 1];

export const TASKS_BY_PLAYERS = { 1: 6, 2: 7, 3: 9, 4: 11 };
export const STATIONS_BY_PLAYERS = { 1: 3, 2: 4, 3: 6 };
export const SPOTS_BY_PLAYERS = { 1: 6, 2: 8, 3: 12 };

export const BAND_MULT = [1.0, 1.2, 1.4];

// T04 (the cart) owns the first slot in the real game; the spike has no cart card, so
// the rule is kept as a named constant instead of being silently dropped.
export function buildPlan({ seed, band, players = 1 }) {
  const count = TASKS_BY_PLAYERS[players] || 6;
  const rng = mulberry32(seed);
  const perStation = Math.ceil(count / (STATIONS_BY_PLAYERS[players] || 3));
  const tasks = [];
  for (let i = 0; i < count; i++) {
    const id = TASK_POOL[Math.floor(rng() * TASK_POOL.length)];
    tasks.push({
      uid: `t${i}`,
      id,
      band,
      station: Math.floor(i / perStation),
      seconds: taskSeconds(id, band),
      goal: taskGoal(id, band),
      done: false
    });
  }
  const stains = band >= 3 ? 3 : 2;
  return { tasks, stains, stainAt: [40, 75, 100].slice(0, stains), seed };
}

export const weightOf = (plan) => 1 / plan.tasks.length;

export function progressOf(plan) {
  const done = plan.tasks.filter((t) => t.done).length;
  return done / plan.tasks.length;
}

// Score = (tasks<=99 *100 + cart<=4 *75 + secondsLeft<=120 *2 + win400/draw150) * band mult, floored.
export function scoreOf({ tasks = 0, cartConditions = 0, secondsLeft = 0, outcome = 'loss', band = 1 }) {
  const base =
    Math.min(tasks, 99) * 100 +
    Math.min(cartConditions, 4) * 75 +
    Math.max(0, Math.min(secondsLeft, 120)) * 2 +
    (outcome === 'win' ? 400 : outcome === 'draw' ? 150 : 0);
  return Math.floor(base * (BAND_MULT[band - 1] ?? 1));
}

export const RIVAL_PACE = 1.12;
export const BOT_TEAMMATE_PACE = 2.2;
export const TAILWIND_GAP = 0.25;

// Rival is a schedule, not a random walk: it lands at ~92% of the clock so the last
// stretch of every round is the interesting part.
export function buildRivalSchedule({ seed, planCount, seconds, band }) {
  const rng = mulberry32(seed ^ 0x9e3779b9);
  const target = seconds * 0.92 / RIVAL_PACE * (1 + 0.06 * (band - 1));
  const gap = target / planCount;
  const at = [];
  for (let i = 0; i < planCount; i++) at.push(Math.min(seconds - 1, gap * (i + 1) * (0.85 + rng() * 0.3)));
  return at.sort((a, b) => a - b);
}

export function coinsOf({ outcome, points, tasks }) {
  if (tasks === 0) return 0;
  const bonus = outcome === 'win' ? 50 : outcome === 'draw' ? 20 : 10;
  return 25 + bonus + Math.floor(points / 20);
}

export function xpOf({ outcome, tasks, firstClear }) {
  if (tasks === 0) return 0;
  return 30 + (outcome === 'win' ? 25 : 0) + tasks * 8 + (firstClear ? 100 : 0);
}

export const XP_PER_LEVEL = 200;
export const MAX_LEVEL = 50;
export const levelOf = (xp) => Math.min(MAX_LEVEL, Math.floor(xp / XP_PER_LEVEL) + 1);

// Stars: (a) win the chapter, (b) lose no round, (c) win a round by >= 3 tasks.
export function starsOf({ wonChapter, roundsLost, bestTaskMargin }) {
  let s = 0;
  if (wonChapter) s += 1;
  if (roundsLost === 0) s += 1;
  if (bestTaskMargin >= 3) s += 1;
  return s;
}

export function seriesOutcome(winsA, winsB, bestOf = 3) {
  const need = Math.ceil(bestOf / 2);
  if (winsA >= need) return 'win';
  if (winsB >= need) return 'loss';
  return 'ongoing';
}

export function chapterUnlocked(chapter, save) {
  if (chapter.n === 1) return true;
  const prev = CHAPTER_PREV[chapter.n - 2];
  return save.level >= chapter.lv && (save.stars[prev] ?? 0) >= 2;
}

export const CHAPTER_PREV = [1, 2, 3, 4, 5, 6];

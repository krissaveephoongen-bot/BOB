import { levelOf } from './rules.js';

const K = (s) => `bob.${s}`;

const DEFAULTS = {
  guest: true,
  char: 'sweep',
  coins: 0,
  xp: 0,
  chapter: 1,
  mode: 'solo',
  band: 1,
  sound: 1,
  reduceMotion: 0,
  taskRace: { best: 0, matches: 0, wins: 0 }
};

function num(key, fallback = 0) {
  const raw = localStorage.getItem(K(key));
  if (raw === null) return fallback;
  const v = Number(raw);
  return Number.isFinite(v) ? v : fallback;
}

export function loadSave() {
  const stars = {};
  const cleared = {};
  for (let i = 1; i <= 6; i++) {
    stars[i] = num(`story.stars.${i}`);
    cleared[i] = num(`story.cleared.${i}`) === 1;
  }
  const best = num('taskRace.best');
  const xp = num('xp');
  return {
    guest: localStorage.getItem(K('guest')) !== '0',
    char: localStorage.getItem(K('char')) || DEFAULTS.char,
    coins: num('coins'),
    xp,
    level: levelOf(xp),
    chapter: num('chapter', 1) || 1,
    mode: localStorage.getItem(K('mode')) || DEFAULTS.mode,
    band: num('band', 1),
    sound: num('sound', 1),
    reduceMotion: num('reduceMotion', 0),
    stars,
    cleared,
    lossStreak: num('story.lossStreak'),
    taskRace: { best, matches: num('taskRace.matches'), wins: num('taskRace.wins') },
    board: JSON.parse(localStorage.getItem(K('board')) || '[]')
  };
}

export function writeSave(save) {
  localStorage.setItem(K('guest'), save.guest ? '1' : '0');
  localStorage.setItem(K('char'), save.char);
  localStorage.setItem(K('coins'), String(save.coins));
  localStorage.setItem(K('xp'), String(save.xp));
  localStorage.setItem(K('chapter'), String(save.chapter));
  localStorage.setItem(K('mode'), save.mode);
  localStorage.setItem(K('band'), String(save.band));
  localStorage.setItem(K('sound'), String(save.sound));
  localStorage.setItem(K('reduceMotion'), String(save.reduceMotion));
  localStorage.setItem(K('story.lossStreak'), String(save.lossStreak || 0));
  localStorage.setItem(K('taskRace.best'), String(save.taskRace.best));
  localStorage.setItem(K('taskRace.matches'), String(save.taskRace.matches));
  localStorage.setItem(K('taskRace.wins'), String(save.taskRace.wins));
  for (let i = 1; i <= 6; i++) {
    localStorage.setItem(K(`story.stars.${i}`), String(save.stars[i] || 0));
    localStorage.setItem(K(`story.cleared.${i}`), save.cleared[i] ? '1' : '0');
  }
  localStorage.setItem(K('board'), JSON.stringify(save.board.slice(0, 10)));
}

export function resetSave() {
  Object.keys(localStorage).filter((k) => k.startsWith('bob.')).forEach((k) => localStorage.removeItem(k));
}

export { DEFAULTS };

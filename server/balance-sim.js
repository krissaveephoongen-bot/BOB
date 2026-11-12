// Hero balance check without a server: plays many rounds with the shared rules (prototype/index.html) and compares
// what each hero's ability is worth against a player with no ability. Run: node balance-sim.js [rounds=20000]
// Player model (an assumption, tune it): the player tries `skill` random stacks and judges each by its real score
// plus noise (they cannot see the exact number), then sends the one that looks best.
//   sweep  - after choosing, also tries every legal swap of two neighbouring layers and keeps the best-looking one
//   bubble - scores with the shield on (the worst bad reaction is cancelled)
//   spray  - one exact preview: if the chosen stack fails the test, they switch to the next-best-looking one
//   press  - 8 s more on the clock (ROUND_TIME 100 s), so the time part of the score is higher
const fs = require('fs'), path = require('path'), vm = require('vm');
const N = parseInt(process.argv[2] || '20000', 10);
const src = fs.readFileSync(path.join(__dirname, '../prototype/index.html'), 'utf8');
const rules = src.slice(src.indexOf('const STATS='), src.indexOf('/* ============ STATE ============ */'));
const R = vm.runInNewContext(rules + ';({PARTS,CHARS,ROUND_BUDGET,ROUND_TIME,makeRound,evalStack,totalScore,randomStack,countLayers,isMethod,mulberry})', {});
const NOISE = 12, FREEZE = 8;
const heroes = ['none', 'sweep', 'bubble', 'spray', 'press'];

function gauss(rng) { return Math.sqrt(-2 * Math.log(rng() + 1e-12)) * Math.cos(2 * Math.PI * rng()); }
function play(hero, Rd, rng, skill, timeFrac) {
  const shield = hero === 'bubble', tf = Math.min(1, timeFrac + (hero === 'press' ? FREEZE / R.ROUND_TIME : 0));
  const score = st => { const ev = R.evalStack(st, Rd.enemies, shield); return R.totalScore(ev.stats, Rd.spec, R.ROUND_BUDGET - ev.cost, R.ROUND_BUDGET, tf); };
  const tried = [];
  for (let i = 0; i < skill; i++) {
    const st = R.randomStack(Rd.offer, R.ROUND_BUDGET, rng); if (!R.countLayers(st)) continue;
    tried.push({ st, sc: score(st), seen: 0 });
  }
  tried.forEach(t => t.seen = t.sc.total + NOISE * gauss(rng));
  tried.sort((a, b) => b.seen - a.seen);
  let pick = tried[0]; if (!pick) return null;
  if (hero === 'sweep') {
    for (let i = 0; i < pick.st.length - 1; i++) {
      const a = pick.st.slice(); [a[i], a[i + 1]] = [a[i + 1], a[i]];
      if (R.isMethod(a[0]) || a.some((id, k) => k && R.isMethod(id) && R.isMethod(a[k - 1]))) continue;
      const sc = score(a), seen = sc.total + NOISE * gauss(rng);
      if (seen > pick.seen) pick = { st: a, sc, seen };
    }
  }
  if (hero === 'spray' && !pick.sc.pass && tried[1]) pick = tried[1].sc.total > pick.sc.total ? tried[1] : pick; // they see the real number once
  return pick.sc;
}

const stat = {}; heroes.forEach(h => stat[h] = { total: 0, pass: 0, n: 0, win: 0, games: 0 });
const master = R.mulberry(20261112);
for (let k = 0; k < N; k++) {
  const round = 1 + (k % 3), seed = Math.floor(master() * 1e9), Rd = R.makeRound(round, seed);
  const skill = 3 + Math.floor(master() * 20), timeFrac = 0.05 + master() * 0.6;
  const res = {};
  heroes.forEach((h, i) => { res[h] = play(h, Rd, R.mulberry(seed ^ (k * 31 + 7)), skill, timeFrac); }); // same rng per hero = same stacks tried
  heroes.forEach(h => { const r = res[h]; if (!r) return; const s = stat[h]; s.total += r.total; s.pass += r.pass ? 1 : 0; s.n++;
    if (h !== 'none' && res.none) { s.games++; if (r.total > res.none.total) s.win++; else if (r.total === res.none.total) s.win += 0.5; } });
}
const base = stat.none.total / stat.none.n;
console.log(`| Hero | Avg score | vs no ability | Pass rate | Win vs no ability |`);
console.log(`|---|---|---|---|---|`);
heroes.forEach(h => { const s = stat[h], avg = s.total / s.n;
  console.log(`| ${h === 'none' ? 'no ability' : h} | ${avg.toFixed(1)} | ${h === 'none' ? '-' : (avg - base >= 0 ? '+' : '') + (avg - base).toFixed(1)} | ${(100 * s.pass / s.n).toFixed(1)}% | ${h === 'none' ? '-' : (100 * s.win / s.games).toFixed(1) + '%'} |`); });
console.log(`\n${N} rounds, rounds 1-3 evenly, skill 3-22 tries, time used 35-95%, judgement noise ±${NOISE}.`);

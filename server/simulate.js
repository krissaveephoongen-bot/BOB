// Bot playtest: plays N matches against the local Nakama the same way the prototype does, so the
// analytics dashboard has data and balance questions can be checked without human players.
// Run: node simulate.js [matches=30] [server=http://127.0.0.1:7350]
// Bots use the prototype's own rival AI with a random skill level, and a random character.
const fs = require('fs'), path = require('path'), vm = require('vm');
const N = parseInt(process.argv[2] || '30', 10), H = process.argv[3] || 'http://127.0.0.1:7350';
const src = fs.readFileSync(path.join(__dirname, '../prototype/index.html'), 'utf8');
const rules = src.slice(src.indexOf('const STATS='), src.indexOf('/* ============ STATE ============ */'));
const R = vm.runInNewContext(rules + ';({PARTS,METHODS,CHARS,ROUND_BUDGET,ROUND_TIME,makeRound,evalStack,totalScore,aiBest,mulberry})', {});

async function post(url, body, auth) {
  const r = await fetch(H + url, { method: 'POST', headers: { 'Content-Type': 'application/json', Authorization: auth }, body: JSON.stringify(body) });
  const t = await r.text();
  if (!r.ok) throw new Error(url + ' ' + r.status + ' ' + t);
  return JSON.parse(t);
}
async function bot(i) {
  const s = await post('/v2/account/authenticate/device?create=true', { id: 'sim-bot-' + String(i % 12).padStart(4, '0') },
    'Basic ' + Buffer.from('defaultkey:').toString('base64'));
  const auth = 'Bearer ' + s.token;
  return { rpc: (id, body) => post('/v2/rpc/' + id + '?unwrap', body, auth) };
}
async function playMatch(i) {
  const b = await bot(i), rng = R.mulberry(1000 + i * 7919);
  const chars = Object.keys(R.CHARS), char = chars[Math.floor(rng() * chars.length)];
  const seed = Math.floor(rng() * 1e6), match = 'sim' + Date.now().toString(36) + i;
  const skill = 3 + Math.floor(rng() * 20); // how many stacks the bot tries before picking its best
  const log = (event, data) => b.rpc('bob_log_event', { event, match, data: Object.assign({ char }, data) });
  await log('match_start', { seed });
  let wins = 0, losses = 0, carry = 0, rivalCarry = 0, round = 0;
  while (wins < 2 && losses < 2 && round < 3) {
    round++;
    const Rd = R.makeRound(round, seed), budget = R.ROUND_BUDGET + Math.floor(carry / 2);
    const timeFrac = 0.05 + rng() * 0.6;
    const me = R.aiBest(Rd.offer, Rd.spec, Rd.enemies, budget, skill, rng, timeFrac);
    const abilityUsed = rng() < 0.6, shield = char === 'bubble' && abilityUsed;
    if (abilityUsed) await log('ability_used', { round, ability: R.CHARS[char].ab });
    const sc = await b.rpc('bob_score_stack', { seed, round, stack: me.stack, budget, timeFrac, shield, match, char });
    const rr = R.mulberry(seed * 31 + round), rb = R.ROUND_BUDGET + Math.floor(rivalCarry / 2);
    const rv = R.aiBest(Rd.offer, Rd.spec, Rd.enemies, rb, 5 + round * 4, rr, 0.1 + rr() * 0.25);
    const left = budget - sc.cost;
    const res = sc.score.total > rv.sc.total ? 'win' : sc.score.total < rv.sc.total ? 'lose' : (left >= rb - rv.ev.cost ? 'win' : 'lose');
    res === 'win' ? wins++ : losses++;
    carry = Math.max(0, left); rivalCarry = Math.max(0, rb - rv.ev.cost);
    await log('round_result', { round, seed, result: res, stack: me.stack, my_total: sc.score.total, rival_total: rv.sc.total,
      pass: sc.score.pass, rival_pass: rv.sc.pass, rival_stack: rv.stack, cost: sc.cost, budget,
      time_left: Math.round(timeFrac * R.ROUND_TIME), ability_used: abilityUsed, enemies: Rd.enemies, bot_skill: skill });
  }
  await log('match_end', { wins, losses, rounds: round, coins: wins > losses ? 30 : 20, bot_skill: skill });
  return wins > losses;
}
(async () => {
  let won = 0;
  for (let i = 0; i < N; i++) {
    // Leave a few matches unfinished so "completion rate" on the dashboard has something to show.
    if (i % 10 === 9) { const b = await bot(i); await b.rpc('bob_log_event', { event: 'match_start', match: 'simquit' + Date.now().toString(36) + i, data: { char: 'press', seed: i } }); continue; }
    if (await playMatch(i)) won++;
  }
  console.log('played ' + N + ' matches, bots won ' + won);
})().catch(e => { console.error(e.message); process.exit(1); });

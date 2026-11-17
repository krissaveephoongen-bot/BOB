// End-to-end playthrough of a Task Race match, offline: no Nakama, no Unity, no docker.
// Run: node e2e-taskrace-test.js
//
// Unlike task-race-test.js (which checks one rule at a time), this file plays a whole round the way the phone does:
// ready up, open every card of the seeded list, finish every step against the server's own clock, play the T04 cart
// with the real shared functions (shelf -> four parts -> worksheet), collect the three emergency stains, and read the
// RESULT the server broadcasts. It then checks what the server wrote to Postgres (bob_events) so the cart lock is
// proven to have reached analytics with its stack, not just to have "not errored".
const fs = require('fs'), path = require('path'), vm = require('vm');
const SRC = fs.readFileSync(path.join(__dirname, 'modules/index.js'), 'utf8');

const OP = { READY: 5, STATE: 10, POS: 11, RESULT: 12, ERROR: 13, TASK_BEGIN: 16, TASK_DONE: 17, TASK_CANCEL: 18,
  PROGRESS: 19, EMERGENCY: 20, TASK_LOCK: 21 };

const checks = [];
function check(name, ok, info) {
  checks.push({ name, ok: !!ok });
  console.log((ok ? '  ok   ' : '  FAIL ') + name + (info !== undefined && info !== '' ? '  ' + info : ''));
}

// A fake Nakama: one module call writes one row, so the harness can read what the server logged.
const rows = [];
function fakeNk() {
  return { sqlExec(sql, params) { rows.push({ sql, params }); }, binaryToString(b) { return Buffer.from(b).toString('utf8'); } };
}
const sent = [];
const d = { broadcasts: sent, broadcastMessage(op, data, to) { sent.push({ op, data: JSON.parse(data), to: to || null }); }, matchLabelUpdate() { } };
const logger = { info() { }, error() { }, warn() { } };

function load() {
  const ctx = { console };
  vm.createContext(ctx);
  vm.runInContext(SRC + ';this.API={matchInit,matchJoin,matchJoinAttempt,matchLoop,taskPlan,taskMinMs,taskCartNeed,' +
    'taskCartStats,taskCartScore,taskCartShelf,taskCartStar,TASK_MINIGAMES,TASK_CART_KEYS,TASK_CART_PARTS,' +
    'TASK_PART_ORDER,TASK_ROUND_TIME,OP};', ctx);
  return ctx.API;
}
const A = load();

function player(id, team) {
  const presence = { userId: id, username: id, sessionId: id };
  return { presence, name: id, team, char: 'sweep', ready: false, x: 3.4, y: 0.6, hand: [], vote: false };
}
function msg(p, op, obj) { return { opCode: op, sender: p.presence, data: Buffer.from(JSON.stringify(obj || {})) }; }

const trail = [];
function tick(state, msgs) {
  sent.length = 0;
  const r = A.matchLoop({}, logger, fakeNk(), d, state.tick || 0, state.s, msgs || []);
  if (r && r.state) state.s = r.state;
  const box = sent.slice();
  trail.push(...box);
  return box;
}
function last(box, op) { for (let i = box.length - 1; i >= 0; i--) if (box[i].op === op) return box[i].data; return null; }
function lastTo(box, op, userId) {
  for (let i = box.length - 1; i >= 0; i--) {
    const b = box[i];
    if (b.op === op && b.to && b.to.some(t => t.userId === userId)) return b.data;
  }
  return null;
}
function errors(box) { return box.filter(b => b.op === OP.ERROR).map(b => b.data.t); }
function count(box, op) { return box.filter(b => b.op === op).length; }

// ---------------------------------------------------------------- boot the room
const init = A.matchInit({}, logger, fakeNk(), { mode: 'taskrace', seed: '1975', difficulty: '3' });
const state = { s: init.state, tick: 0 };
const a = player('a0', 0), b = player('b0', 1);
A.matchJoin({}, logger, fakeNk(), d, 0, state.s, [a.presence, b.presence]);
state.s.players = { a0: a, b0: b };
check('e2e: a task race room starts in the lobby at D3', state.s.mode === 'taskrace' && state.s.phase === 'lobby' && state.s.difficulty === 3,
  'mode ' + state.s.mode + ' phase ' + state.s.phase + ' D' + state.s.difficulty);

let box = tick(state, [msg(a, OP.READY, { ready: true }), msg(b, OP.READY, { ready: true })]);
check('e2e: readying up opens the race', state.s.phase === 'race', 'phase ' + state.s.phase);
check('e2e: the race clock is the Task Race clock', Math.abs(state.s.time - A.TASK_ROUND_TIME) < 0.01, 'time ' + state.s.time);

const mine = state.s.teams[0].entries, rival = state.s.teams[1].entries;
check('e2e: both islands race the same six cards', mine.map(e => e.id).join() === rival.map(e => e.id).join(),
  mine.map(e => e.id).join(' '));
check('e2e: the cart card opens the round', mine[0].id === 'T04' && mine[0].st === 2, mine[0].id + ' steps ' + mine[0].st);
check('e2e: the list is the seeded plan at D3', mine.map(e => e.id).join(' ') === A.taskPlan(1975, 1, 3, state.s.cats).map(g => g.id).join(' '),
  mine.map(e => e.id).join(' '));

// ---------------------------------------------------------------- the refusal rules a real round meets
box = tick(state, [msg(a, OP.TASK_BEGIN, { task: 'T04', step: 2, spot: 'S1_A' })]);
check('e2e: step 2 before step 1 is refused', errors(box).includes('ต้องทำขั้น 1 ก่อน'), errors(box)[0]);

box = tick(state, [msg(a, OP.TASK_BEGIN, { task: 'T04', step: 1, spot: 'S2_B' })]);
check('e2e: the rival island\'s station is refused', errors(box).includes('จุดงานไม่ใช่ฝั่งของผู้เล่น'), errors(box)[0]);

box = tick(state, [msg(a, OP.TASK_BEGIN, { task: 'T99', step: 1, spot: 'S1_A' })]);
check('e2e: a card outside the list is refused', errors(box).includes('งานนี้ไม่อยู่ในรายการของรอบ'), errors(box)[0]);

box = tick(state, [msg(a, OP.TASK_BEGIN, { task: 'T04', step: 1, spot: 'S1_A' })]);
check('e2e: opening the cart card locks the station', !!last(box, OP.TASK_LOCK) && last(box, OP.TASK_LOCK).by === 'a0');

box = tick(state, [msg(a, OP.TASK_BEGIN, { task: 'T09', step: 1, spot: 'S1_A' })]);
check('e2e: one station carries one open card', errors(box).includes('จุดงานนี้มีคนอื่นใช้อยู่'), errors(box)[0]);

box = tick(state, [msg(a, OP.TASK_DONE, { task: 'T04', step: 1, ms: 99999 })]);
check('e2e: a step faster than TaskMinMs is refused', errors(box).includes('เร็วเกินไปสำหรับขั้นนี้'), errors(box)[0]);

box = tick(state, [msg(a, OP.TASK_CANCEL, { task: 'T04' })]);
check('e2e: walking away frees the station', !!last(box, OP.TASK_LOCK) && last(box, OP.TASK_LOCK).free === true);

// ---------------------------------------------------------------- play the T04 cart card for real
// The phone asks the shared rules for the shelf, picks four, and locks the stack it built. This harness does the same
// and sends the numbers the card would send, so the round is played with the shipped algorithm, not a fake stack.
const shelf = A.taskCartShelf(1975, 3);
const perms = (ids) => {
  const out = [];
  for (let i = 0; i < ids.length; i++) for (let j = 0; j < ids.length; j++) {
    if (j === i) continue;
    for (let k = 0; k < ids.length; k++) {
      if (k === i || k === j) continue;
      for (let l = 0; l < ids.length; l++) {
        if (l === i || l === j || l === k) continue;
        out.push([ids[i], ids[j], ids[k], ids[l]]);
      }
    }
  }
  return out;
};
const shelfNeed = A.taskCartNeed(1975, 3);
let cartSolution = null;
for (const p4 of perms(shelf)) {
  const sc = A.taskCartScore(1975, 3, p4);
  if (sc.passed && sc.pass >= 3) { cartSolution = { stack: p4, score: sc }; break; }
}
check('e2e: the D3 shelf holds a four-part formula that passes', !!cartSolution,
  cartSolution ? cartSolution.stack.join('+') + ' -> ' + cartSolution.score.stats.join('/') + ' pass ' + cartSolution.score.pass
    : 'shelf ' + shelf.join('+') + ' need ' + JSON.stringify(shelfNeed));
check('e2e: the worksheet the card draws matches the seed', shelfNeed.speed === 9 && shelfNeed.clean === 5,
  JSON.stringify(shelfNeed));

function minMsOf(id, step) { return A.taskMinMs(id, step); }
// T04 step 1: the shelf pick. Step 2: the lock, which is the moment the formula reaches the server.
box = tick(state, [msg(a, OP.TASK_BEGIN, { task: 'T04', step: 1, spot: 'S1_A' })]);
check('e2e: the cart card re-opens', !!last(box, OP.TASK_LOCK) && last(box, OP.TASK_LOCK).step === 1);
state.s.teams[0].locks['T04'].at -= minMsOf('T04', 1) + 200;
box = tick(state, [msg(a, OP.TASK_DONE, { task: 'T04', step: 1, ms: 1700, sample: { taken: A.TASK_CART_PARTS, shelf } })]);
check('e2e: the shelf pick counts', state.s.teams[0].entries[0].step === 1 && errors(box).length === 0, errors(box)[0]);
check('e2e: half the cart card is half its weight', Math.abs((lastTo(box, OP.PROGRESS, 'a0') || { team: {} }).team.dw - 1) < 0.001,
  'dw ' + (lastTo(box, OP.PROGRESS, 'a0') || { team: {} }).team.dw);

box = tick(state, [msg(a, OP.TASK_BEGIN, { task: 'T04', step: 2, spot: 'S1_A' })]);
check('e2e: step 2 of the cart card opens', errors(box).length === 0 && !!last(box, OP.TASK_LOCK), errors(box)[0]);
state.s.teams[0].locks['T04'].at -= minMsOf('T04', 2) + 200;
const cartSample = { stack: cartSolution.stack, stats: cartSolution.score.stats, pass: cartSolution.score.pass,
  passed: cartSolution.score.passed, need: cartSolution.score.need, buffs: cartSolution.score.buffs };
box = tick(state, [msg(a, OP.TASK_DONE, { task: 'T04', step: 2, ms: 800, sample: cartSample })]);
check('e2e: the locked formula finishes the cart card', state.s.teams[0].entries[0].done === true && errors(box).length === 0, errors(box)[0]);
check('e2e: the cart card paid its whole weight', Math.abs((lastTo(box, OP.PROGRESS, 'a0') || { team: {} }).team.dw - 2) < 0.001,
  'dw ' + (lastTo(box, OP.PROGRESS, 'a0') || { team: {} }).team.dw);

// ---------------------------------------------------------------- play the rest of the list
// One helper drives every remaining step the way the phone does: BEGIN, the server's clock passes, DONE.
function playAll(st, p, entries) {
  for (const entry of entries) {
    for (let step = entry.step + 1; step <= entry.st; step++) {
      let bx = tick(st, [msg(p, OP.TASK_BEGIN, { task: entry.id, step, spot: 'S1_A' })]);
      if (!last(bx, OP.TASK_LOCK) || errors(bx).length) return entry.id + ' step ' + step + ': ' + (errors(bx)[0] || 'no lock');
      st.s.teams[0].locks[entry.id].at -= minMsOf(entry.id, step) + 200;
      bx = tick(st, [msg(p, OP.TASK_DONE, { task: entry.id, step, ms: minMsOf(entry.id, step) + 250, sample: { taken: 1 } })]);
      if (errors(bx).length) return entry.id + ' step ' + step + ': ' + errors(bx)[0];
    }
  }
  return null;
}
const played = ['T04'];
const restErr = playAll(state, a, mine.slice(1));
if (restErr) check('e2e: the rest of the list plays', false, restErr);
for (const entry of mine.slice(1)) played.push(entry.id);
const sumA = state.s.teams[0].entries.reduce((n, e) => n + e.w, 0);
check('e2e: every card of the list is finished', state.s.teams[0].entries.every(e => e.done),
  played.join(' ') + ' (' + state.s.teams[0].entries.filter(e => e.done).length + '/' + mine.length + ')');
check('e2e: the team is at 100 %', Math.abs(state.s.teams[0].entries.reduce((n, e) => n + e.w * Math.min(e.st, e.step) / e.st, 0) - sumA) < 0.001);
check('e2e: the rival island is still at 0 %', state.s.teams[1].entries.every(e => e.step === 0));

// ---------------------------------------------------------------- the three emergency stains
const stains = [];
let guard = 0;
while (state.s.phase === 'race' && guard++ < 1400) {
  box = tick(state, []);
  for (const ev of box.filter(x => x.op === OP.EMERGENCY)) stains.push(ev.data);
}
check('e2e: three stains spawn in a D3 round', stains.length === 3, stains.map(s => s.enemy + '@' + s.spot + '/' + s.at).join(' '));
check('e2e: the stains land at 40 / 75 / 100 s', stains.map(s => s.at).join(',') === '40,75,100', stains.map(s => s.at).join(','));
check('e2e: each stain names an enemy and a station', stains.every(s => s.enemy && /^S[0-9]+$/.test(s.spot) && s.graceSec > 0),
  stains.map(s => s.enemy + '/' + s.spot).join(' '));

// ---------------------------------------------------------------- the result the phones read
const result = trail.filter(x => x.op === OP.RESULT).pop();
check('e2e: the race ends with a RESULT', !!result, 'phase ' + state.s.phase);
if (result) {
  const r = result.data;
  check('e2e: the winning island is the one that finished', r.winner === 0 && r.mode === 'taskrace', 'winner ' + r.winner);
  check('e2e: the winner finished every card', r.teams[0].done === 6 && r.teams[0].pct === 100, JSON.stringify(r.teams[0]));
  check('e2e: the idle island keeps its zeros', r.teams[1].done === 0 && r.teams[1].pct === 0, JSON.stringify(r.teams[1]));
  check('e2e: the result is final', r.over === true && r.wins[1] === 0);
}

// ---------------------------------------------------------------- what the server wrote down
const done = rows.filter(r => /task_done/.test(String(r.params && r.params[2])));
const withStack = done.filter(r => r.params && /"stack"/.test(String(r.params[3] || '')));
check('e2e: every finished step is one analytics row', done.length === mine.reduce((n, e) => n + e.st, 0),
  done.length + ' rows');
check('e2e: the cart lock reached analytics with its stack',
  withStack.some(r => cartSolution.stack.every(id => String(r.params[3]).includes(id))), cartSolution.stack.join('+'));
const starts = rows.filter(r => /match_start/.test(String(r.params && r.params[2])));
check('e2e: the match start was logged once', starts.length === 1, starts.length + ' rows');

// ---------------------------------------------------------------- the same round at D1 and D2
// The flow, not the worksheet: a fresh room per difficulty, every card played, two stains, one result.
for (const diff of [1, 2]) {
  const i2 = A.matchInit({}, logger, fakeNk(), { mode: 'taskrace', seed: '1975', difficulty: String(diff) });
  const st2 = { s: i2.state, tick: 0 };
  const me2 = player('d' + diff, 0), them2 = player('r' + diff, 1);
  A.matchJoin({}, logger, fakeNk(), d, 0, st2.s, [me2.presence, them2.presence]);
  st2.s.players = {}; st2.s.players[me2.presence.userId] = me2; st2.s.players[them2.presence.userId] = them2;
  tick(st2, [msg(me2, OP.READY, { ready: true }), msg(them2, OP.READY, { ready: true })]);
  // the cart card is step 1 + step 2; the helper plays step 2 only if step 1 already counted, so open it first.
  const cart = st2.s.teams[0].entries[0];
  tick(st2, [msg(me2, OP.TASK_BEGIN, { task: cart.id, step: 1, spot: 'S1_A' })]);
  st2.s.teams[0].locks[cart.id].at -= minMsOf(cart.id, 1) + 200;
  tick(st2, [msg(me2, OP.TASK_DONE, { task: cart.id, step: 1, ms: 1700, sample: { taken: A.TASK_CART_PARTS } })]);
  const err2 = playAll(st2, me2, st2.s.teams[0].entries);
  check('e2e: D' + diff + ' plays every card of the list', !err2 && st2.s.teams[0].entries.every(e => e.done), err2 || '');
  const stains2 = [];
  let g2 = 0;
  while (st2.s.phase === 'race' && g2++ < 1400) {
    for (const ev of tick(st2, []).filter(x => x.op === OP.EMERGENCY)) stains2.push(ev.data);
  }
  const res2 = trail.filter(x => x.op === OP.RESULT).pop();
  check('e2e: D' + diff + ' ends with two stains and a result',
    stains2.length === 2 && st2.s.phase === 'result' && !!res2 && res2.data.winner === 0,
    stains2.length + ' stains, winner ' + (res2 && res2.data.winner));
}

// ---------------------------------------------------------------- verdict
const failed = checks.filter(c => !c.ok);
console.log('');
console.log((failed.length ? 'E2E FAILED: ' : 'E2E PASSED: ') + (checks.length - failed.length) + '/' + checks.length + ' checks');
if (failed.length) process.exitCode = 1;

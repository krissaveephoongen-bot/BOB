// Offline test of the Task Race contract in 'bob_coop' (docs 16 section 8, opcodes 16-21): no Nakama, no docker.
// Run: node task-race-test.js
// Loads the generated module in a sandbox and drives matchInit / matchJoin / matchLoop exactly like the server does,
// so every refusal and every broadcast is checked against the same code that ships.
const fs = require('fs'), path = require('path'), vm = require('vm');
const SRC = fs.readFileSync(path.join(__dirname, 'modules/index.js'), 'utf8');

const OP = { READY: 5, STATE: 10, POS: 11, RESULT: 12, ERROR: 13, TASK_BEGIN: 16, TASK_DONE: 17, TASK_CANCEL: 18,
  PROGRESS: 19, EMERGENCY: 20, TASK_LOCK: 21 };
const checks = [];
function check(name, ok, info) { checks.push({ name, ok: !!ok }); console.log((ok ? '  ok   ' : '  FAIL ') + name + (info ? '  ' + info : '')); }

// A fake Nakama: the module only needs sqlExec (analytics), binaryToString and the dispatcher's broadcastMessage.
const sent = [];
function fakeNk() {
  return { sqlExec() { }, binaryToString(b) { return Buffer.from(b).toString('utf8'); } };
}
const d = {
  broadcasts: sent,
  broadcastMessage(op, data, to) { sent.push({ op, data: JSON.parse(data), to: to || null }); },
  matchLabelUpdate() { }
};
const logger = { info() { }, error() { }, warn() { } };

function load() {
  const ctx = { console };
  vm.createContext(ctx);
  vm.runInContext(SRC + ';this.API={matchInit,matchJoin,matchJoinAttempt,matchLoop,matchTerminate,taskPlan,taskMinMs,' +
    'taskComposition,taskTotalWeight,TASK_MINIGAMES,TASK_ROUND_TIME,OP};', ctx);
  const A = ctx.API;
  A.matchLoop = A.matchLoop;
  return A;
}
const A = load();

function player(id, team) {
  const presence = { userId: id, username: id, sessionId: id };
  return { presence, name: id, team, char: 'sweep', ready: false, x: 3.4, y: 0.6, hand: [], vote: false };
}
function msg(p, op, obj) { return { opCode: op, sender: p.presence, data: Buffer.from(JSON.stringify(obj || {})) }; }
function tick(state, msgs) {
  sent.length = 0;
  const r = A.matchLoop({}, logger, fakeNk(), d, state.tick || 0, state.s, msgs || []);
  if (r && r.state) state.s = r.state;
  const box = sent.slice();
  trail.push(...box);
  return box;
}
const trail = []; // every broadcast of the run, so a message sent inside the tick that ended the race is still readable
function last(box, op) { for (let i = box.length - 1; i >= 0; i--) if (box[i].op === op) return box[i].data; return null; }
// PROGRESS is sent once per team, so read the copy addressed to a given player.
function lastTo(box, op, userId) {
  for (let i = box.length - 1; i >= 0; i--) {
    const b = box[i];
    if (b.op === op && b.to && b.to.some(t => t.userId === userId)) return b.data;
  }
  return null;
}
function errors(box) { return box.filter(b => b.op === OP.ERROR).map(b => b.data.t); }

// ---------------------------------------------------------------- the match
const state = { tick: 0 };
const init = A.matchInit({}, logger, fakeNk(), { seed: '1975', mode: 'taskrace', difficulty: '1' });
state.s = init.state;
check('init: a task race room starts in the lobby', state.s.phase === 'lobby' && state.s.mode === 'taskrace', 'phase ' + state.s.phase);

const p0 = player('u0', 0), p1 = player('u1', 1);
A.matchJoin({}, logger, fakeNk(), d, 0, state.s, [p0.presence, p1.presence]);
state.s.players = { u0: p0, u1: p1 };

// Ready both: the race must start with the seeded list, not the cart rounds.
let box = tick(state, [msg(p0, OP.READY, { char: 'sweep', ready: true }), msg(p1, OP.READY, { char: 'bubble', ready: true })]);
check('start: phase becomes race', state.s.phase === 'race', 'phase ' + state.s.phase);
check('start: the clock is the 120 s Task Race clock', Math.abs(state.s.time - A.TASK_ROUND_TIME) < 0.01, 'time ' + state.s.time);
const plan = A.taskPlan(1975, 1, 1, ['sweep', 'wash', 'scrub', 'spray']);
const ids0 = state.s.teams[0].entries.map(e => e.id), ids1 = state.s.teams[1].entries.map(e => e.id);
check('start: both islands get the same list', ids0.join() === ids1.join(), ids0.join(' '));
check('start: the list is the seeded plan', ids0.join() === plan.map(g => g.id).join(), ids0.join(' '));
check('start: the cart card opens every round', ids0[0] === 'T04', ids0[0]);
check('start: 6 cards for a solo-sized side', ids0.length === 6, ids0.length + ' cards');
check('start: total weight 8', state.s.teams[0].entries.reduce((a, e) => a + e.w, 0) === 8);
const firstProgress = last(box, OP.PROGRESS);
check('start: PROGRESS goes out to each team', firstProgress && firstProgress.team.pct === 0 && firstProgress.team.total === 6, JSON.stringify(firstProgress && firstProgress.team));

// ---------------------------------------------------------------- begin: order, duplicates, locks
const step1 = 1;
box = tick(state, [msg(p0, OP.TASK_DONE, { task: 'T04', step: 1, ms: 9999 })]);
check('done: without BEGIN is refused', errors(box).includes('ยังไม่ได้เริ่มงานนี้'), errors(box)[0]);

box = tick(state, [msg(p0, OP.TASK_BEGIN, { task: 'T04', step: 2, spot: 'S1' })]);
check('begin: step 2 before step 1 is refused', errors(box).includes('ต้องทำขั้น 1 ก่อน'), errors(box)[0]);

box = tick(state, [msg(p0, OP.TASK_BEGIN, { task: 'T99', step: 1 })]);
check('begin: a card outside the round list is refused', errors(box).includes('งานนี้ไม่อยู่ในรายการของรอบ'), errors(box)[0]);

box = tick(state, [msg(p0, OP.TASK_BEGIN, { task: 'T04', step: step1, spot: 'S1' })]);
const lock = last(box, OP.TASK_LOCK);
check('begin: the step locks and the team is told', lock && lock.task === 'T04' && lock.by === 'u0' && lock.free === false, JSON.stringify(lock));

box = tick(state, [msg(p1, OP.TASK_BEGIN, { task: 'T04', step: step1, spot: 'S1' })]);
check('begin: the other island can take the same card (separate copies)', errors(box).length === 0 && !!last(box, OP.TASK_LOCK), errors(box)[0]);

// ---------------------------------------------------------------- min ms is the server's clock
box = tick(state, [msg(p0, OP.TASK_DONE, { task: 'T04', step: 1, ms: 99999 })]);
check('done: a step faster than TaskMinMs is refused', errors(box).includes('เร็วเกินไปสำหรับขั้นนี้'), errors(box)[0]);
check('done: a refused step does not score', state.s.teams[0].entries[0].step === 0);
state.s.teams[0].locks['T04'].at -= A.taskMinMs('T04', 1) + 50; // pretend the 1.5 s of step 1 really passed
box = tick(state, [msg(p0, OP.TASK_DONE, { task: 'T04', step: 1, ms: 1600, sample: { taken: 3 } })]);
check('done: after the minimum the step counts', state.s.teams[0].entries[0].step === 1, 'step ' + state.s.teams[0].entries[0].step);
const prog = lastTo(box, OP.PROGRESS, 'u0');
check('progress: half of a 2-step cart card is half its weight', prog && Math.abs(prog.team.dw - 1) < 0.001 && prog.team.pct === 12.5,
  'dw ' + (prog && prog.team.dw) + ' pct ' + (prog && prog.team.pct));
check('progress: the rival is still at 0 %', prog && prog.rival.pct === 0);

// ---------------------------------------------------------------- cancel keeps the work
box = tick(state, [msg(p0, OP.TASK_BEGIN, { task: 'T04', step: 2 })]);
check('begin: step 2 is allowed after step 1', errors(box).length === 0, errors(box)[0]);
box = tick(state, [msg(p0, OP.TASK_CANCEL, { task: 'T04' })]);
const free = last(box, OP.TASK_LOCK);
check('cancel: the lock is released', free && free.free === true && free.by === '', JSON.stringify(free));
check('cancel: finished steps are kept', state.s.teams[0].entries[0].step === 1 && !state.s.teams[0].entries[0].done);

// ---------------------------------------------------------------- the rest of the list, then the result
function fly(side, who) { // finish every remaining step of one island, honouring the minimum time
  return state.s.teams[side].entries.map(e => {
    while (!e.done) {
      const step = e.step + 1;
      tick(state, [msg(who, OP.TASK_BEGIN, { task: e.id, step })]);
      const held = state.s.teams[side].locks[e.id];
      if (!held) return e.id + ' never locked at step ' + step;
      held.at -= A.taskMinMs(e.id, step) + 50;
      tick(state, [msg(who, OP.TASK_DONE, { task: e.id, step, ms: A.taskMinMs(e.id, step) + 60 })]);
      if (!e.done && e.step !== step) return e.id + ' stuck at step ' + step;
    }
    return null;
  }).filter(Boolean);
}
const stuck0 = fly(0, p0);
check('done: one island can finish its whole list', stuck0.length === 0, stuck0.join(', '));
check('done: the race is not over while the rival still has work', state.s.phase === 'race', 'phase ' + state.s.phase);
const stuck1 = fly(1, p1);
check('done: the other island finishes too', stuck1.length === 0, stuck1.join(', '));
check('done: finishing the list ends the race', state.s.phase === 'result', 'phase ' + state.s.phase);
const result = last(trail, OP.RESULT) || null;
check('result: both islands report 100 %', result && result.teams[0].pct === 100 && result.teams[1].pct === 100,
  result ? JSON.stringify(result.teams) : 'no RESULT');
check('result: an equal finish is a draw, not a win', result && result.winner === -1 && result.over === true, result ? 'winner ' + result.winner : 'no RESULT');

// ---------------------------------------------------------------- emergencies + the clock
const st2 = { tick: 0 };
st2.s = A.matchInit({}, logger, fakeNk(), { seed: '777', mode: 'taskrace' }).state;
const q0 = player('v0', 0), q1 = player('v1', 1);
A.matchJoin({}, logger, fakeNk(), d, 0, st2.s, [q0.presence, q1.presence]);
st2.s.players = { v0: q0, v1: q1 };
tick(st2, [msg(q0, OP.READY, { ready: true }), msg(q1, OP.READY, { ready: true })]);
const times = st2.s.emergencies.map(e => e.at);
check('emergency: 2-3 moments from the seed', times.length >= 2 && times.length <= 3, times.join(','));
// Jump the clock to just past the first moment: the tick that crosses it must be the one that spawns the stain.
st2.s.time = A.TASK_ROUND_TIME - times[0] - 0.05;
const b3 = tick(st2, []);
const seen = b3.filter(x => x.op === OP.EMERGENCY).map(x => x.data);
check('emergency: the first stain spawns on the seeded second', seen.length === 1 && seen[0].at === times[0] && seen[0].graceSec === 25,
  seen.length ? 'at ' + seen[0].at + ' enemy ' + seen[0].enemy : 'none');
check('emergency: an unknown card is never used', seen.every(e => e.id === 'T16' && !!e.enemy && !!e.spot));
const again = tick(st2, []).filter(x => x.op === OP.EMERGENCY);
check('emergency: it never spawns twice', again.length === 0);

st2.s.time = 0.05;
const b2 = tick(st2, []);
check('clock: time up ends the race with a result', st2.s.phase === 'result' && !!last(b2, OP.RESULT), 'phase ' + st2.s.phase);

// ---------------------------------------------------------------- cart rounds are untouched
const cart = A.matchInit({}, logger, fakeNk(), { seed: '11' });
check('cart: a room without the taskrace flag keeps the build rounds', cart.state.mode === 'cart' && cart.state.phase === 'lobby');
const c0 = player('c0', 0), c1 = player('c1', 1);
A.matchJoin({}, logger, fakeNk(), d, 0, cart.state, [c0.presence, c1.presence]);
cart.state.players = { c0, c1 };
const cbox = tick({ s: cart.state }, [msg(c0, OP.READY, { ready: true }), msg(c1, OP.READY, { ready: true })]);
check('cart: readying up still starts a build round with no task list', cart.state.phase === 'build' && cart.state.teams[0].entries.length === 0,
  'phase ' + cart.state.phase);
check('cart: nothing from the Task Race is broadcast', cbox.every(b => b.op < OP.TASK_BEGIN));

const bad = checks.filter(c => !c.ok).length;
console.log('\n' + (checks.length - bad) + '/' + checks.length + ' checks passed');
process.exit(bad ? 1 : 0);

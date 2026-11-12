// End-to-end test of the 'bob_coop' match against a local Nakama (node 22+, no packages).
// Run: node mp-test.js [players=2]   Each bot joins, readies, walks to a station, picks a part, carries it to its
// team's cart, places it and submits. Passes when all 3 rounds are scored and every bot sees the same results.
const HOST = process.env.BOB_HOST || '127.0.0.1:7350', KEY = 'defaultkey';
const N = Math.max(2, Math.min(4, +process.argv[2] || 2));
const OP = { MOVE: 1, PICK: 2, PLACE: 3, LIFT: 4, READY: 5, SUBMIT: 6, STATE: 10, POS: 11, RESULT: 12, ERROR: 13 };
const STATION = { tool: [1.2, 3.75], chem: [6.8, 3.75], energy: [1.2, 1.25], method: [6.8, 1.25] };
const FAM = { cloth: 'tool', sponge: 'tool', brush: 'tool', broom: 'tool', soap: 'chem', vinegar: 'chem', soda: 'chem', polish: 'chem',
  hot: 'energy', steam: 'energy', scrub: 'energy', air: 'energy', soak: 'method', spray: 'method', circle: 'method' };
const CART = [3.3, 2.5]; // left of the cart, within reach
// Bots walk in lanes so they never press into a station, the cart or the sofa: x = 2.6 / 5.4 up and down, y = 3.3 across.
const sleep = ms => new Promise(r => setTimeout(r, ms));
const b64 = o => Buffer.from(JSON.stringify(o)).toString('base64');

async function auth(id) {
  const r = await fetch(`http://${HOST}/v2/account/authenticate/device?create=true&username=bot${id}`, {
    method: 'POST', headers: { Authorization: 'Basic ' + Buffer.from(KEY + ':').toString('base64'), 'Content-Type': 'application/json' },
    body: JSON.stringify({ id: 'bob-mp-test-device-' + id })
  });
  if (!r.ok) throw new Error('auth ' + r.status + ' ' + await r.text());
  return (await r.json()).token;
}

class Bot {
  constructor(i, token) { this.i = i; this.token = token; this.results = []; this.errors = []; this.state = null; this.cid = 0; this.wait = {}; }
  open() {
    return new Promise((ok, bad) => {
      this.ws = new WebSocket(`ws://${HOST}/ws?lang=en&status=true&format=json&token=${this.token}`);
      this.ws.onopen = ok; this.ws.onerror = e => bad(new Error('socket error'));
      this.ws.onmessage = ev => this.onMsg(JSON.parse(ev.data));
    });
  }
  onMsg(m) {
    if (m.cid && this.wait[m.cid]) { const w = this.wait[m.cid]; delete this.wait[m.cid]; m.error ? w.bad(new Error(JSON.stringify(m.error))) : w.ok(m); return; }
    if (!m.match_data) return;
    const op = +m.match_data.op_code, d = JSON.parse(Buffer.from(m.match_data.data || '', 'base64').toString() || '{}');
    if (op === OP.STATE) { const pm = {}; d.players.forEach(p => pm[p.id] = p); d.players = pm; this.state = d; }
    if (op === OP.RESULT) this.results.push(d);
    if (op === OP.ERROR) this.errors.push(d.t);
    if (op === OP.POS && this.state) { this.state.time = d.time; const me = d.p.find(q => q.id === this.me); if (me) this.server = [me.x, me.y]; }
  }
  req(body) { const cid = String(++this.cid); return new Promise((ok, bad) => { this.wait[cid] = { ok, bad }; this.ws.send(JSON.stringify({ cid, ...body })); }); }
  send(op, obj) { this.ws.send(JSON.stringify({ match_data_send: { match_id: this.matchId, op_code: String(op), data: b64(obj) } })); }
  async join(matchId) {
    this.matchId = matchId;
    const r = await this.req({ match_join: { match_id: matchId } });
    this.me = r.match.self.user_id;
  }
  async route(x, y) {
    const lane = this.pos[0] < 4 ? 2.6 : 5.4;
    for (const [wx, wy] of [[lane, this.pos[1]], [lane, 3.3], [x, 3.3], [x, y]]) await this.walkTo(wx, wy);
  }
  async walkTo(x, y) {
    for (let k = 0; k < 60; k++) {
      const [px, py] = this.pos, dx = x - px, dy = y - py, d = Math.hypot(dx, dy);
      if (d < 0.05) return;
      const st = Math.min(d, 0.25); this.pos = [px + dx / d * st, py + dy / d * st];
      this.send(OP.MOVE, { x: this.pos[0], y: this.pos[1] });
      await sleep(100);
    }
  }
  async playRound(round, submit = true) {
    const s = this.state, team = s.players[this.me].team;
    // Each bot carries one part; team-mates pick different ones so the shared cart gets two layers.
    const mates = Object.keys(s.players).filter(k => s.players[k].team === team).sort();
    const offer = s.room.offer.filter(id => FAM[id] !== 'method');
    const id = offer[(mates.indexOf(this.me) + team * 2 + round) % offer.length];
    this.pos = [s.players[this.me].x, s.players[this.me].y];
    await this.route(...STATION[FAM[id]]); this.send(OP.PICK, { id }); await sleep(200);
    await this.route(...CART); this.send(OP.PLACE, { i: 0 }); await sleep(300);
    if (submit && mates.indexOf(this.me) === mates.length - 1) { await sleep(400); this.send(OP.SUBMIT, {}); }
  }
}

module.exports = { Bot, auth, OP, sleep, HOST };
if (require.main === module) (async () => {
  const bots = [];
  for (let i = 0; i < N; i++) { const b = new Bot(i, await auth(i)); await b.open(); bots.push(b); }
  const r = await fetch(`http://${HOST}/v2/rpc/bob_create_match?unwrap`, {
    method: 'POST', headers: { Authorization: 'Bearer ' + bots[0].token }, body: JSON.stringify({ seed: 4242 }) });
  const { match_id } = await r.json();
  console.log('match', match_id);
  for (const b of bots) await b.join(match_id);
  await sleep(300);
  const teams = Object.values(bots[0].state.players).map(p => p.team);
  console.log('teams', teams.join(','));
  bots.forEach((b, i) => b.send(OP.READY, { char: ['sweep', 'bubble', 'spray', 'press'][i] }));
  for (let round = 1; round <= 3; round++) {
    for (let k = 0; k < 100 && !(bots.every(b => b.state && b.state.phase === 'build' && b.state.round === round)); k++) await sleep(100);
    if (bots[0].state.phase !== 'build') break;
    const s = bots[0].state;
    console.log(`round ${round}: enemies ${s.room.enemies.join('+')}, offer ${s.room.offer.join(',')}`);
    await Promise.all(bots.map(b => b.playRound(round)));
    for (let k = 0; k < 100 && bots.some(b => b.results.length < round); k++) await sleep(100);
    const res = bots[0].results[round - 1];
    if (!res) throw new Error('no result for round ' + round);
    console.log(`  stacks ${res.teams.map(t => '[' + t.stack.join(',') + ']=' + t.score.total).join(' vs ')} winner team ${res.winner} wins ${res.wins}`);
    if (res.over) break;
  }
  const same = bots.every(b => JSON.stringify(b.results) === JSON.stringify(bots[0].results));
  const errs = bots.flatMap(b => b.errors);
  const last = bots[0].results[bots[0].results.length - 1];
  bots.forEach(b => b.ws.close());
  console.log('errors', errs.length ? errs.join(' | ') : 'none');
  if (!last || !last.over || !same || errs.length) { console.log('FAIL', { over: last && last.over, same }); process.exit(1); }
  console.log('PASS: match finished, every client saw the same results');
})().catch(e => { console.error('FAIL', e); process.exit(1); });

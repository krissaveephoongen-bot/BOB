// End-to-end test of the co-op moves in 'bob_coop' against a local Nakama (node 22+, no packages): 4 bots, 2 teams of 2.
// Run: node coop-test.js   Checks, per team: walls (stations are solid), passing a part to a team-mate, parking a part on the
// team's table and taking it back, pushing the shared cart to a new spot, placing on the moved cart, and that a player
// who drops mid-round leaves their parts on the table. Passes when every check holds and the match ends the same for all.
const HOST = process.env.BOB_HOST || '127.0.0.1:7350', KEY = 'defaultkey';
const OP = { MOVE: 1, PICK: 2, PLACE: 3, LIFT: 4, READY: 5, SUBMIT: 6, PUSH: 7, PASS: 8, TABLE: 9, STATE: 10, POS: 11, RESULT: 12, ERROR: 13 };
const STATION = { tool: [1.15, 3.75], chem: [6.85, 3.75], energy: [1.15, 1.6], method: [6.85, 1.6] };
const FAM = { cloth: 'tool', sponge: 'tool', brush: 'tool', broom: 'tool', soap: 'chem', vinegar: 'chem', soda: 'chem', polish: 'chem',
  hot: 'energy', steam: 'energy', scrub: 'energy', air: 'energy', soak: 'method', spray: 'method', circle: 'method' };
const TABLE_SPOT = [4.0, 3.85];       // just under the parking table [3.5, 4.2, 4.5, 4.7]
const sleep = ms => new Promise(r => setTimeout(r, ms));
const b64 = o => Buffer.from(JSON.stringify(o)).toString('base64');
const checks = [];
function check(name, ok, info) { checks.push({ name, ok: !!ok }); console.log((ok ? '  ok   ' : '  FAIL ') + name + (info ? '  ' + info : '')); }

async function auth(id) {
  const r = await fetch(`http://${HOST}/v2/account/authenticate/device?create=true&username=coop${id}`, {
    method: 'POST', headers: { Authorization: 'Basic ' + Buffer.from(KEY + ':').toString('base64'), 'Content-Type': 'application/json' },
    body: JSON.stringify({ id: 'bob-coop-test-device-' + id })
  });
  if (!r.ok) throw new Error('auth ' + r.status + ' ' + await r.text());
  return (await r.json()).token;
}

class Bot {
  constructor(i, token) { this.i = i; this.token = token; this.results = []; this.errors = []; this.state = null; this.cid = 0; this.wait = {}; this.pos = {}; this.carts = null; }
  open() {
    return new Promise((ok, bad) => {
      this.ws = new WebSocket(`ws://${HOST}/ws?lang=en&status=true&format=json&token=${this.token}`);
      this.ws.onopen = ok; this.ws.onerror = () => bad(new Error('socket error'));
      this.ws.onmessage = ev => this.onMsg(JSON.parse(ev.data));
    });
  }
  onMsg(m) {
    if (m.cid && this.wait[m.cid]) { const w = this.wait[m.cid]; delete this.wait[m.cid]; m.error ? w.bad(new Error(JSON.stringify(m.error))) : w.ok(m); return; }
    if (!m.match_data) return;
    const op = +m.match_data.op_code, d = JSON.parse(Buffer.from(m.match_data.data || '', 'base64').toString() || '{}');
    if (op === OP.STATE) { const pm = {}; d.players.forEach(p => { pm[p.id] = p; this.pos[p.id] = [p.x, p.y]; }); d.players = pm; this.state = d; this.carts = d.teams.map(t => [t.cx, t.cy]); }
    if (op === OP.RESULT) this.results.push(d);
    if (op === OP.ERROR) this.errors.push(d.t);
    if (op === OP.POS) { d.p.forEach(q => this.pos[q.id] = [q.x, q.y]); if (d.c) this.carts = d.c.map(c => [c.x, c.y]); if (this.state) this.state.time = d.time; }
  }
  req(body) { const cid = String(++this.cid); return new Promise((ok, bad) => { this.wait[cid] = { ok, bad }; this.ws.send(JSON.stringify({ cid, ...body })); }); }
  send(op, obj) { this.ws.send(JSON.stringify({ match_data_send: { match_id: this.matchId, op_code: String(op), data: b64(obj) } })); }
  async join(matchId) { this.matchId = matchId; const r = await this.req({ match_join: { match_id: matchId } }); this.me = r.match.self.user_id; }
  get at() { return this.pos[this.me]; }
  get team() { return this.state.players[this.me].team; }
  get hand() { return this.state.players[this.me].hand; }
  // Walk using the server's position as the truth; returns the distance left.
  async walkTo(x, y, tries = 80) {
    for (let k = 0; k < tries; k++) {
      const [px, py] = this.at, dx = x - px, dy = y - py, d = Math.hypot(dx, dy);
      if (d < 0.08) return d;
      const st = Math.min(d, 0.25);
      this.send(OP.MOVE, { x: px + dx / d * st, y: py + dy / d * st });
      await sleep(110);
    }
    const [px, py] = this.at; return Math.hypot(x - px, y - py);
  }
  // Players spawn under the sofa; step out to the side first (real players steer around it themselves).
  async leaveSpawn() { const [x] = this.at; const sx = x < 4 ? 2.6 : 5.4; await this.walkTo(sx, 0.5); await this.walkTo(sx, 2.0); }
  async until(f, ms = 3000) { for (let t = 0; t < ms; t += 50) { if (f()) return true; await sleep(50); } return false; }
}

(async () => {
  const bots = [];
  for (let i = 0; i < 4; i++) { const b = new Bot(i, await auth(i)); await b.open(); bots.push(b); }
  const r = await fetch(`http://${HOST}/v2/rpc/bob_create_match?unwrap`, {
    method: 'POST', headers: { Authorization: 'Bearer ' + bots[0].token }, body: JSON.stringify({ seed: 777 }) });
  const { match_id } = await r.json();
  console.log('match', match_id);
  for (const b of bots) await b.join(match_id);
  await bots[0].until(() => bots[0].state && Object.keys(bots[0].state.players).length === 4);

  // Lobby: a full side refuses a switch
  const t0 = bots.filter(b => bots[0].state.players[b.me].team === 0);
  const t1 = bots.filter(b => bots[0].state.players[b.me].team === 1);
  t0[0].send(OP.READY, { char: 'sweep', ready: false, team: 1 });
  check('lobby: switching to a full team is refused', await t0[0].until(() => t0[0].errors.includes('ทีมนั้นเต็มแล้ว')));
  t0[0].errors.length = 0;
  bots.forEach((b, i) => b.send(OP.READY, { char: ['sweep', 'bubble', 'spray', 'press'][i] }));
  await bots[0].until(() => bots.every(b => b.state && b.state.phase === 'build' && b.state.round === 1), 5000);
  console.log('round 1 offer', bots[0].state.room.offer.join(','));

  // ---- Round 1: every co-op move, both teams at once
  async function teamPlay([a, b]) {
    const T = a.team, offer = a.state.room.offer.filter(id => FAM[id] !== 'method');
    const part = offer[T % offer.length], fam = FAM[part];
    await Promise.all([a.leaveSpawn(), b.leaveSpawn()]);
    // walls: walking into the middle of the tool shelf ends outside it
    await a.walkTo(1.15, 4.4, 25);
    const [wx, wy] = a.at;
    check(`team ${T}: shelves are solid`, !(wx > 0.3 - 0.15 && wx < 2.0 + 0.15 && wy > 4.06 - 0.15 && wy < 4.7 + 0.15), `at ${wx},${wy}`);
    // a picks, walks to b, passes
    await a.walkTo(...STATION[fam]); a.send(OP.PICK, { id: part });
    await a.until(() => a.hand.length === 1);
    await b.walkTo(4.0, 3.2); await a.walkTo(4.5, 3.2);
    a.send(OP.PASS, { i: 0, to: b.me });
    check(`team ${T}: pass a part to a team-mate`, await b.until(() => b.hand[0] === part && a.hand.length === 0));
    // b parks it on the table, a takes it back
    await b.walkTo(...TABLE_SPOT); b.send(OP.TABLE, { put: 0 });
    check(`team ${T}: park a part on the table`, await b.until(() => b.state.teams[T].table[0] === part && b.hand.length === 0));
    await a.walkTo(...TABLE_SPOT); a.send(OP.TABLE, { take: 0 });
    check(`team ${T}: take it from the table`, await a.until(() => a.hand[0] === part && a.state.teams[T].table.length === 0));
    // b pushes the cart 1.2 to the left
    await b.walkTo(4.6, 2.53); b.send(OP.PUSH, { on: true });
    await b.until(() => b.state.teams[T].pusher === b.me);
    await b.walkTo(3.4, 2.53);
    b.send(OP.PUSH, { on: false });
    await b.until(() => b.state.teams[T].pusher === '');
    const [cx] = b.carts[T];
    check(`team ${T}: push the cart`, cx < 3.0, `cart x ${cx}`);
    // a places on the cart where it is now
    await a.walkTo(cx, 1.95); a.send(OP.PLACE, { i: 0 });
    check(`team ${T}: place on the moved cart`, await a.until(() => a.state.teams[T].stack[0] === part));
    a.send(OP.SUBMIT, {});
  }
  await Promise.all([teamPlay(t0), teamPlay(t1)]);
  await bots[0].until(() => bots.every(b => b.results.length >= 1), 8000);
  const r1 = bots[0].results[0];
  console.log(`round 1: ${r1.teams.map(t => '[' + t.stack.join(',') + ']=' + t.score.total).join(' vs ')} winner ${r1.winner}`);
  check('round 1 scored the moved carts', r1.teams.every(t => t.stack.length === 1));

  // ---- Round 2: a player drops while holding a part -> it lands on the team's table
  await bots[0].until(() => bots.every(b => b.state.phase === 'build' && b.state.round === 2), 15000);
  const quitter = t1[1], stay = t1[0], off = stay.state.room.offer.filter(id => FAM[id] !== 'method');
  const qpart = off[0];
  await quitter.leaveSpawn();
  await quitter.walkTo(...STATION[FAM[qpart]]); quitter.send(OP.PICK, { id: qpart });
  await quitter.until(() => quitter.hand.length === 1);
  quitter.ws.close();
  check('a dropped player leaves their part on the table', await stay.until(() => stay.state.teams[1].table.includes(qpart), 4000));
  // the rest finish the match quickly: everyone left submits
  bots.filter(b => b !== quitter).forEach(b => b.send(OP.SUBMIT, {}));
  for (let round = 2; round <= 3; round++) {
    await bots[0].until(() => bots[0].results.length >= round || (bots[0].results.length && bots[0].results[bots[0].results.length - 1].over), 15000);
    const last = bots[0].results[bots[0].results.length - 1];
    if (last.over) break;
    await bots[0].until(() => bots[0].state.phase === 'build' && bots[0].state.round === round + 1, 15000);
    bots.filter(b => b !== quitter).forEach(b => b.send(OP.SUBMIT, {}));
  }
  const live = bots.filter(b => b !== quitter);
  await sleep(300);
  const last = live[0].results[live[0].results.length - 1];
  check('match ends', last && last.over);
  check('every client saw the same results', live.every(b => JSON.stringify(b.results) === JSON.stringify(live[0].results)));
  const errs = bots.flatMap(b => b.errors);
  check('no refused moves on the happy path', errs.length === 0, errs.join(' | '));
  live.forEach(b => b.ws.close());
  const failed = checks.filter(c => !c.ok);
  if (failed.length) { console.log('FAIL', failed.map(c => c.name).join('; ')); process.exit(1); }
  console.log(`PASS: ${checks.length} checks`);
})().catch(e => { console.error('FAIL', e); process.exit(1); });

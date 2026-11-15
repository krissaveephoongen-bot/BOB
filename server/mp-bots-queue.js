// Fills the public matchmaker with bots, so a real player who taps "ออนไลน์ 2-4 คน" (Game scene > hero select) lands
// in a match with them. One bot holds the queue ticket; when the match is made, the rest join it by id and play.
// Run: node mp-bots-queue.js [bots=2] [waitSec=600]   (BOB_HOST, BOB_TLS, BOB_KEY like mp-test.js)
// Bots on the human's team never submit - the human does (same rule as mp-host.js).
const { Bot, auth, OP, sleep } = require('./mp-test');
const N = Math.max(1, Math.min(3, +process.argv[2] || 2));
const WAIT = Math.max(30, +process.argv[3] || 600);
const ALL = process.argv.includes('--all'); // self-test: every bot queues (no human needed)

(async () => {
  const bots = [];
  for (let i = 0; i < N; i++) { const b = new Bot(i + 20, await auth(i + 20)); await b.open(); bots.push(b); }
  const lead = bots[0];
  let matched = null;
  lead.ws.addEventListener('message', ev => {
    let m; try { m = JSON.parse(ev.data); } catch (_) { return; }
    if (m.matchmaker_matched) matched = m.matchmaker_matched;
  });
  // Same ticket the client uses: BobOnline.FindAsync -> AddMatchmakerAsync("*", 2, 4)
  for (const b of (ALL ? bots : [lead]))
    b.ws.send(JSON.stringify({ matchmaker_add: { min_count: 2, max_count: 4, query: '*', string_properties: {}, numeric_properties: {} } }));
  console.log(`${ALL ? N : 1} bot(s) queued; ${ALL ? 0 : N - 1} more follow into the match. Waiting for a player (${WAIT}s)...`);
  for (let k = 0; k < WAIT * 10 && !matched; k++) await sleep(100);
  if (!matched) { console.log('TIMEOUT: nobody joined the queue'); process.exit(0); }

  const matchId = matched.match_id;
  console.log('match', matchId);
  for (const b of bots) await b.join(matchId);
  for (let k = 0; k < 600 && Object.keys(bots[0].state?.players || {}).length < N; k++) await sleep(100);
  await sleep(2000); // let a late presence land before the first ready

  const ids = bots.map(b => b.me);
  bots.forEach((b, i) => b.send(OP.READY, { char: ['bubble', 'spray', 'press'][i] }));
  for (let round = 1; round <= 3; round++) {
    for (let k = 0; k < 600 && !(bots[0].state && bots[0].state.phase === 'build' && bots[0].state.round === round); k++) await sleep(100);
    if (!bots[0].state || bots[0].state.phase !== 'build') break;
    const state = bots[0].state;
    const human = Object.keys(state.players).find(id => !ids.includes(id));
    const humanTeam = human ? state.players[human].team : -1;
    if (round === 1) console.log(human ? `player joined on team ${humanTeam}` : 'no player: bots play both sides');
    await Promise.all(bots.map(b => b.playRound(round, humanTeam >= 0 ? b.state.players[b.me].team !== humanTeam : true)));
    for (let k = 0; k < 1500 && bots[0].results.length < round; k++) await sleep(100);
    const res = bots[0].results[round - 1];
    if (!res) { console.log('FAIL no result for round', round); process.exit(1); }
    console.log(`round ${round}: ${res.teams.map(t => '[' + t.stack.join(',') + ']=' + t.score.total).join(' vs ')} winner ${res.winner}`);
    if (res.over) break;
  }
  await sleep(1200);
  bots.forEach(b => b.ws.close());
  console.log('QUEUE DONE');
})().catch(e => { console.error('FAIL', e); process.exit(1); });

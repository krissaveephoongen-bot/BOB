// Hosts a private 'bob_coop' match for a real client (the Unity online test, or a phone) and fills it with bots.
// Run: node mp-host.js [bots=2] [matchIdFile]   Writes the match id to matchIdFile, waits for one more player,
// then every bot readies and plays like in mp-test.js. Bots on the human's team never submit: the human does.
const fs = require('fs');
const { Bot, auth, OP, sleep, HOST } = require('./mp-test');
const N = Math.max(1, Math.min(3, +process.argv[2] || 2));
const FILE = process.argv[3] || 'mp-match.txt';

(async () => {
  const bots = [];
  for (let i = 0; i < N; i++) { const b = new Bot(i + 10, await auth(i + 10)); await b.open(); bots.push(b); }
  const r = await fetch(`http://${HOST}/v2/rpc/bob_create_match?unwrap`, {
    method: 'POST', headers: { Authorization: 'Bearer ' + bots[0].token }, body: JSON.stringify({ seed: 5151 }) });
  const { match_id } = await r.json();
  for (const b of bots) await b.join(match_id);
  fs.writeFileSync(FILE, match_id);
  console.log('match', match_id, '-> written to', FILE, '; waiting for the player');
  for (let k = 0; k < 1200 && Object.keys(bots[0].state.players).length < N + 1; k++) await sleep(100);
  const ids = bots.map(b => b.me);
  const human = Object.keys(bots[0].state.players).find(id => !ids.includes(id));
  if (!human) { console.log('FAIL no player joined'); process.exit(1); }
  const humanTeam = bots[0].state.players[human].team;
  console.log('player joined on team', humanTeam);
  bots.forEach((b, i) => b.send(OP.READY, { char: ['bubble', 'spray', 'press'][i] }));
  for (let round = 1; round <= 3; round++) {
    for (let k = 0; k < 600 && !(bots[0].state.phase === 'build' && bots[0].state.round === round); k++) await sleep(100);
    if (bots[0].state.phase !== 'build') break;
    await Promise.all(bots.map(b => b.playRound(round, b.state.players[b.me].team !== humanTeam)));
    for (let k = 0; k < 1500 && bots[0].results.length < round; k++) await sleep(100);
    const res = bots[0].results[round - 1];
    if (!res) { console.log('FAIL no result for round', round); process.exit(1); }
    console.log(`round ${round}: ${res.teams.map(t => '[' + t.stack.join(',') + ']=' + t.score.total).join(' vs ')} winner ${res.winner}`);
    if (res.over) break;
  }
  await sleep(1500);
  bots.forEach(b => b.ws.close());
  console.log('HOST DONE');
})().catch(e => { console.error('FAIL', e); process.exit(1); });

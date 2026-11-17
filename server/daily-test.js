// Offline test of the daily challenge seed (docs 31 section 2.2). Two halves, both without Nakama or Docker:
//   1. the rule: day numbers, the seed table, the UTC+7 turn of the day, and the three mirrors agreeing
//      (server/daily.js, prototype/index.html, and the numbers BobDaily.cs must print in unity-check);
//   2. the endpoint: a real HTTP server on an ephemeral port, asked with a real request, checks the status codes.
// Run: node server/daily-test.js
const fs = require('fs'), path = require('path'), vm = require('vm'), http = require('http');
const D = require('./daily.js');

let fails = 0, checks = 0;
function check(name, ok, info) {
  checks++;
  console.log((ok ? '  ok   ' : '  FAIL ') + name + (info !== undefined ? '  ' + JSON.stringify(info) : ''));
  if (!ok) fails++;
}

// ------------------------------------------------------------------ 1. the rule
console.log('== day and seed ==');

// 00:00 UTC+7 on 2026-11-17 is 17:00 UTC on 2026-11-16.
const NOON_TH = Date.UTC(2026, 10, 17, 5, 0, 0);   // 12:00 in Thailand
check('day number of 2026-11-17 is 20774', D.dailyDayNumber(NOON_TH) === 20774, D.dailyDayNumber(NOON_TH));
check('label of that day', D.dailyDayLabel(20774) === '2026-11-17', D.dailyDayLabel(20774));

// UTC+7 midnight, not UTC: 23:59:59 local is still today, 00:00:00 local is tomorrow.
const lateNight = Date.UTC(2026, 10, 17, 16, 59, 59);   // 23:59:59 on the 17th, in Thailand
const newDay = Date.UTC(2026, 10, 17, 17, 0, 0);        // 00:00:00 on the 18th, in Thailand
check('23:59:59 local is still the 17th', D.dailyDayNumber(lateNight) === 20774, D.dailyDayNumber(lateNight));
check('00:00:00 local turns the day', D.dailyDayNumber(newDay) === 20775, D.dailyDayNumber(newDay));
check('next day is a whole day away at midnight', D.dailyNextDayMs(newDay) === 86400000, D.dailyNextDayMs(newDay));
check('next day shrinks through the day', D.dailyNextDayMs(NOON_TH) === 43200000, D.dailyNextDayMs(NOON_TH));

// The table unity-check prints from BobDaily.cs: same days, same seeds. A drift on either side fails one gate.
const SEED_ROWS = [[20774, 8131275], [20775, 1364361], [20776, 8028718], [0, 716302], [1, 4737385], [365, 3934344]];
for (const [day, expect] of SEED_ROWS) {
  const got = D.dailySeed(day);
  check('seed of day ' + day + ' is ' + expect, got === expect, got);
}
check('seed stays in 1..8388607', [0, 1, 20774, 20775, 99999, 1234567].every(d => {
  const s = D.dailySeed(d); return s >= 1 && s <= 8388607;
}));
check('30 consecutive days give 30 different seeds', (() => {
  const seen = new Set();
  for (let d = 20774; d < 20804; d++) seen.add(D.dailySeed(d));
  return seen.size === 30;
})());

// dailyInfo pins the clock, so a test can ask what the endpoint would answer at a given moment.
const info = D.dailyInfo(NOON_TH);
check('info carries the day, the seed and one round',
  info.ok === true && info.day === '2026-11-17' && info.dayNumber === 20774 && info.seed === 8131275 && info.rounds === 1, info);
check('info says how long today lasts', info.nextDayInSec === 43200, info.nextDayInSec);

// The prototype mirror is a copy; this refuses to let the copy drift.
console.log('== prototype mirror ==');
const html = fs.readFileSync(path.join(__dirname, '../prototype/index.html'), 'utf8');
const a = html.indexOf('// DAILY RULES BEGIN'), b = html.indexOf('// DAILY RULES END');
check('prototype carries a daily block', a > 0 && b > a, [a, b]);
if (a > 0 && b > a) {
  const block = html.slice(a, b);
  // the block calls the prototype's own mulberry32, so that one line is copied in with it
  const gen = html.match(/function mulberry\(a\)\{[^\n]*\}/);
  check('prototype still carries mulberry32', !!gen, gen && gen[0].length);
  const sandbox = {};
  vm.createContext(sandbox);
  vm.runInContext((gen ? gen[0] : '') + block + ';this.M={DAILY_SALT:DAILY_SALT,DAILY_ROUNDS:DAILY_ROUNDS,DAILY_TZ_MS:DAILY_TZ_MS,' +
    'dailyDayNumber:dailyDayNumber,dailySeed:dailySeed,dailyDayLabel:dailyDayLabel};', sandbox);
  check('prototype salt and rounds match the server',
    sandbox.M.DAILY_SALT === D.DAILY_SALT && sandbox.M.DAILY_ROUNDS === D.DAILY_ROUNDS && sandbox.M.DAILY_TZ_MS === D.DAILY_TZ_MS,
    [sandbox.M.DAILY_SALT, D.DAILY_SALT]);
  check('prototype day numbers match', sandbox.M.dailyDayNumber(NOON_TH) === D.dailyDayNumber(NOON_TH));
  check('prototype labels match', sandbox.M.dailyDayLabel(20774) === '2026-11-17');
  check('prototype seeds match the server on every table row',
    SEED_ROWS.every(([day, expect]) => sandbox.M.dailySeed(day) === expect),
    SEED_ROWS.map(([day]) => sandbox.M.dailySeed(day)));
}

// ------------------------------------------------------------------ 2. the endpoint
console.log('== GET /daily/seed over HTTP ==');

function get(port, urlPath, method) {
  return new Promise((resolve, reject) => {
    const req = http.request({ host: '127.0.0.1', port: port, path: urlPath, method: method || 'GET' }, (res) => {
      let body = '';
      res.on('data', (c) => { body += c; });
      res.on('end', () => {
        let json = null;
        try { json = JSON.parse(body); } catch (e) { json = null; }
        resolve({ status: res.statusCode, json: json, headers: res.headers });
      });
    });
    req.on('error', reject);
    req.end();
  });
}

const server = http.createServer((req, res) => {
  const out = D.dailyHandle(req.method, req.url);
  const body = JSON.stringify(out.body);
  res.writeHead(out.status, { 'content-type': 'application/json; charset=utf-8', 'cache-control': 'no-store' });
  res.end(body);
});

server.listen(0, '127.0.0.1', async () => {
  const port = server.address().port;
  try {
    const one = await get(port, '/daily/seed');
    check('endpoint answers 200', one.status === 200, one.status);
    check('endpoint names today', one.json && one.json.ok === true && typeof one.json.dayNumber === 'number', one.json && one.json.day);
    check('endpoint seed is the rule\'s seed', one.json && one.json.seed === D.dailySeed(one.json.dayNumber), one.json && one.json.seed);
    check('endpoint sends one round', one.json && one.json.rounds === 1);
    check('endpoint is not cacheable', one.headers['cache-control'] === 'no-store', one.headers['cache-control']);

    const two = await get(port, '/daily/seed');
    check('two calls in a row give the same seed', two.json && two.json.seed === one.json.seed);

    const same = await get(port, '/daily/seed?day=' + one.json.dayNumber);
    check('asking for today by number is allowed', same.status === 200 && same.json.seed === one.json.seed, same.status);

    const stale = await get(port, '/daily/seed?day=' + (one.json.dayNumber - 1));
    check('asking for yesterday is refused', stale.status === 409 && stale.json.code === 'stale_day', [stale.status, stale.json && stale.json.code]);
    check('the refusal still says what today is', stale.json && stale.json.today && stale.json.today.seed === one.json.seed);

    const nope = await get(port, '/daily/other');
    check('unknown path is 404', nope.status === 404 && nope.json.code === 'not_found', nope.status);
    const post = await get(port, '/daily/seed', 'POST');
    check('POST is 405', post.status === 405, post.status);
    const health = await get(port, '/health');
    check('health answers', health.status === 200 && health.json.ok === true);
  } catch (e) {
    check('endpoint call failed: ' + e.message, false);
  } finally {
    server.close();
  }

  console.log('\n' + (fails ? 'DAILY FAILED: ' + fails + ' of ' + checks : 'DAILY PASSED: ' + checks + '/' + checks + ' checks'));
  process.exit(fails ? 1 : 0);
});

// Prints the leaderboard ids the daily rounds write to, so the Unity Cloud dashboard can create them ahead of time
// (docs 32 section 3, docs 31 section 2.2). There is no second clock here: the day number and its label come from
// server/daily.js, the same functions the endpoint and its tests use. The only new line is the "daily_" prefix, which
// mirrors BobDaily.DailyBoardName and BobDaily.ValidBoardName (gated by unity-check's `daily board checked`).
//
//   node server/daily-boards.js                        # today + the next 6 days (UTC+7)
//   node server/daily-boards.js 14                     # today + the next 13
//   node server/daily-boards.js 7 --ms 1794873600000   # pin the clock (the tests use this)
//   node server/daily-boards.js --self-test            # asserts only, no table
//
// Nothing here talks to UGS: creating a board is a dashboard (or admin API) step and needs credentials this repo does
// not hold. The script only removes the arithmetic mistakes — the id is derived, never typed by hand.
var daily = require('./daily');

var MAX_DAYS = 60;

function boardName(dayNumber) { return 'daily_' + Math.max(0, Math.trunc(dayNumber)); }

// The same rules as BobDaily.ValidBoardName: lowercase letters, digits, "_" and "-", 4..50 characters.
function legal(id) {
  if (typeof id !== 'string' || id.length < 4 || id.length > 50) return false;
  for (var i = 0; i < id.length; i++) {
    var c = id.charAt(i);
    var ok = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c === '_' || c === '-';
    if (!ok) return false;
  }
  return true;
}

// One row per day, starting with the server's day for `ms`. Capped so a typo cannot print thousands of lines.
function plan(ms, days) {
  var n = Math.max(1, Math.min(MAX_DAYS, Math.trunc(days) || 7));
  var first = daily.dailyDayNumber(ms === undefined ? Date.now() : ms);
  var out = [];
  for (var i = 0; i < n; i++) {
    var day = first + i;
    out.push({ day: day, date: daily.dailyDayLabel(day), board: boardName(day), seed: daily.dailySeed(day) });
  }
  return out;
}

function format(rows) {
  var lines = ['day          board            seed', '-----------  ---------------  --------'];
  rows.forEach(function (r) {
    lines.push(pad(r.day, 11) + '  ' + pad(r.board, 15) + '  ' + pad(r.seed, 8) + '  ' + r.date);
  });
  return lines.join('\n');
}

function pad(v, width) {
  var s = String(v);
  while (s.length < width) s += ' ';
  return s;
}

function selfTest() {
  var fails = 0, checks = 0;
  function check(name, ok) { checks++; if (!ok) { fails++; console.log('FAIL ' + name); } }

  // 2026-11-17 (UTC+7) is day 20774, the number docs 31 and the C# mirror both pin.
  var pinned = 1794901200000; // 2026-11-17T13:00:00Z, i.e. 20:00 in Thailand
  var rows = plan(pinned, 3);
  check('the pinned day is 20774', rows[0].day === 20774);
  check('the id is derived, not typed', rows[0].board === 'daily_20774');
  check('the label is the UTC+7 date', rows[0].date === '2026-11-17');
  check('the next days are consecutive', rows[1].day === 20775 && rows[2].day === 20776 &&
    rows[1].board === 'daily_20775' && rows[2].board === 'daily_20776');
  check('the seed matches daily.js', rows[0].seed === daily.dailySeed(20774));
  check('every id the plan prints is legal', plan(pinned, 30).every(function (r) { return legal(r.board); }));
  check('a negative day clamps to zero', boardName(-5) === 'daily_0');
  check('the day count is capped', plan(pinned, 9999).length === MAX_DAYS && plan(pinned, 0).length === 7);
  check('illegal ids are refused', !legal('') && !legal('ab') && !legal('daily 1') && !legal('DAILY_1') &&
    !legal(new Array(52).join('a')));
  console.log(fails === 0 ? 'DAILY BOARDS PASSED (' + checks + ' checks)' : 'DAILY BOARDS FAILED: ' + fails);
  return fails === 0 ? 0 : 1;
}

if (require.main === module) {
  if (process.argv.indexOf('--self-test') >= 0) process.exit(selfTest());

  var days = 7, ms;
  var args = process.argv.slice(2);
  for (var i = 0; i < args.length; i++) {
    if (args[i] === '--ms') { ms = Number(args[i + 1]); i++; }
    else if (/^\d+$/.test(args[i])) days = Number(args[i]);
  }
  var rows = plan(ms, days);
  console.log('Unity Cloud leaderboards to create for the daily rounds (docs 32 section 3)');
  console.log('clock: ' + (ms === undefined ? 'now' : new Date(ms).toISOString()) + ' · each board: sort high-to-low, keep best');
  console.log('');
  console.log(format(rows));
}

module.exports = { boardName: boardName, legal: legal, plan: plan, format: format, MAX_DAYS: MAX_DAYS };

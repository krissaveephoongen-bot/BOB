// The daily challenge (docs 31 section 2.2): the day number and its round seed, decided by the server.
//
// Why the server: the seed must be the same for every player in the same room and impossible to re-roll by moving the
// phone's clock. The client sends nothing but asks; the answer here is a pure function of the clock the server keeps, so
// a phone with a wrong date still gets today's list. The client mirrors the same maths (BobDaily.cs) and rejects a reply
// whose seed does not belong to the day it names — a mangled reply cannot start a private list.
//
// This file has no Nakama dependency: server/daily-server.js serves it over HTTP, and build-module.js also appends it
// to modules/index.js so a Nakama deployment can answer bob_daily with the same numbers.
//
// Day boundary is UTC+7 midnight (the owner's time zone), not UTC: a "day" starts at 00:00 in Thailand, so a player at
// 23:30 local does not get tomorrow's list five hours early.

var DAILY_SALT = 0x02f6e2b1;          // must match BobDaily.Salt (C#) and DAILY_SALT (prototype)
var DAILY_ROUNDS = 1;                 // one graded attempt per day
var DAILY_TZ_MS = 7 * 3600 * 1000;    // UTC+7
var DAILY_SEED_MAX = 8388607;         // seed range 1..8388607, the range the Task Race scene has always drawn from

// mulberry32, the same generator the task list uses, written out so the phone and the server agree bit for bit.
function dailyMulberry(a) {
  return function () {
    a |= 0; a = a + 0x6D2B79F5 | 0;
    var t = Math.imul(a ^ a >>> 15, 1 | a);
    t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t;
    return ((t ^ t >>> 14) >>> 0) / 4294967296;
  };
}

// 20774 for 2026-11-17 (UTC+7). Integer days, so a seed never depends on the time of day.
function dailyDayNumber(ms) { return Math.floor((ms + DAILY_TZ_MS) / 86400000); }

// The seed of one day: a pure function of the day number, 1..8388607. Same maths as BobDaily.SeedFor.
function dailySeed(dayNumber) {
  return 1 + Math.floor(dailyMulberry((dayNumber | 0) ^ DAILY_SALT)() * DAILY_SEED_MAX);
}

// "2026-11-17" in UTC+7, for the label the client prints. The client never derives the label itself.
function dailyDayLabel(dayNumber) {
  return new Date((dayNumber | 0) * 86400000).toISOString().slice(0, 10);
}

function dailyNextDayMs(ms) {
  var into = ((ms + DAILY_TZ_MS) % 86400000 + 86400000) % 86400000;
  return 86400000 - into;
}

// The answer both the HTTP endpoint and the bob_daily RPC give. Pure: pass the clock in, so a test can pin it.
function dailyInfo(ms) {
  var day = dailyDayNumber(ms);
  return {
    ok: true, version: 1,
    day: dailyDayLabel(day), dayNumber: day, seed: dailySeed(day),
    rounds: DAILY_ROUNDS, nextDayInSec: Math.floor(dailyNextDayMs(ms) / 1000)
  };
}

// GET /daily/seed -> the day's seed. GET /daily/seed?day=N pins the day a replay asks for: the same day answers, any
// other day is refused, so a client cannot ask the server to bless a seed it picked itself.
function dailyHandle(method, urlPath, ms) {
  var path = String(urlPath || '/'), query = '', q = -1;
  if ((q = path.indexOf('?')) >= 0) { query = path.slice(q + 1); path = path.slice(0, q); }
  if (method !== 'GET' && method !== undefined) return { status: 405, body: { ok: false, code: 'method', note: 'GET only' } };
  if (path === '/health') return { status: 200, body: { ok: true, service: 'bob-daily' } };
  if (path !== '/daily/seed') return { status: 404, body: { ok: false, code: 'not_found', path: path } };
  var info = dailyInfo(ms === undefined ? Date.now() : ms);
  var want = '';
  query.split('&').forEach(function (pair) {
    var parts = pair.split('=');
    if (parts[0] === 'day') want = decodeURIComponent(parts[1] || '');
  });
  if (want !== '' && want !== String(info.dayNumber)) {
    return { status: 409, body: { ok: false, code: 'stale_day', askedFor: want, today: info } };
  }
  return { status: 200, body: info };
}

// Nakama RPC: payload {"day":20734} (optional) -> the same JSON as GET /daily/seed.
function rpcDaily(ctx, logger, nk, payload) {
  var asked = '';
  try { asked = String((JSON.parse(payload || '{}').day) || ''); } catch (e) { asked = ''; }
  var info = dailyInfo(Date.now());
  if (asked !== '' && asked !== String(info.dayNumber)) {
    throw new Error('day ' + asked + ' is not today (' + info.dayNumber + ')');
  }
  return JSON.stringify(info);
}

// The same file is concatenated into modules/index.js, where `module` does not exist; the guard keeps it a no-op there.
if (typeof module !== 'undefined' && module.exports) {
  module.exports = {
    DAILY_SALT: DAILY_SALT, DAILY_ROUNDS: DAILY_ROUNDS, DAILY_TZ_MS: DAILY_TZ_MS, DAILY_SEED_MAX: DAILY_SEED_MAX,
    dailyDayNumber: dailyDayNumber, dailySeed: dailySeed, dailyDayLabel: dailyDayLabel,
    dailyNextDayMs: dailyNextDayMs, dailyInfo: dailyInfo, dailyHandle: dailyHandle, rpcDaily: rpcDaily
  };
}

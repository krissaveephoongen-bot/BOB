
/* ============ MULTIPLAYER: server-authoritative match 'bob_coop' (2-4 players, two teams) ============ */
// Every player in the match gets the same room per round (one seed). Each team shares one cart.
// Clients only send intents (move, pick, place, lift, submit); the server checks them and owns the state.
// Opcodes, client -> server: 1 MOVE {x,y}  2 PICK {id}  3 PLACE {i}  4 LIFT {}  5 READY {char}  6 SUBMIT {}
// Opcodes, server -> client: 10 STATE (full snapshot)  11 POS {time,p:[{id,x,y}]} every tick  12 RESULT (round result)  13 ERROR {t}
var OP = { MOVE: 1, PICK: 2, PLACE: 3, LIFT: 4, READY: 5, SUBMIT: 6, STATE: 10, POS: 11, RESULT: 12, ERROR: 13 };
var MP_TICK = 10, MP_MAX = 4, MP_HAND = 2, MP_TEST_SEC = 8, MP_SPEED = 2.3 * 1.25; // world units / s, with lag slack
var STATION_FAM = { tool: [0.3, 4.06, 2.0, 4.7], chem: [6.0, 4.06, 7.7, 4.7], energy: [0.3, 0.3, 2.0, 0.94], method: [6.0, 0.3, 7.7, 0.94] };
var CART = [3.55, 2.21, 4.45, 2.85], NEAR = 0.6;
// Same solids and radius as BobGame.World (stations, cart, sofa): players are pushed out of them like on the phone.
var SOLIDS = [STATION_FAM.tool, STATION_FAM.chem, STATION_FAM.energy, STATION_FAM.method, CART, [3.0, 0.85, 5.0, 1.15]], RADIUS = 0.16;
function mpPushOut(x, y) {
  for (var i = 0; i < SOLIDS.length; i++) {
    var r = SOLIDS[i], cx = Math.max(r[0], Math.min(r[2], x)), cy = Math.max(r[1], Math.min(r[3], y)), d = Math.hypot(x - cx, y - cy);
    if (d >= RADIUS) continue;
    if (d > 0.0001) { x = cx + (x - cx) / d * RADIUS; y = cy + (y - cy) / d * RADIUS; }
    else { // centre inside the rect: leave by the nearest edge
      var opts = [[r[0] - RADIUS, y, x - r[0]], [r[2] + RADIUS, y, r[2] - x], [x, r[1] - RADIUS, y - r[1]], [x, r[3] + RADIUS, r[3] - y]];
      opts.sort(function (a, b) { return a[2] - b[2]; }); x = opts[0][0]; y = opts[0][1];
    }
  }
  return [x, y];
}

function mpDist(x, y, r) { var cx = Math.max(r[0], Math.min(r[2], x)), cy = Math.max(r[1], Math.min(r[3], y)); return Math.hypot(x - cx, y - cy); }
function mpSend(d, op, obj, to) { d.broadcastMessage(op, JSON.stringify(obj), to || null, null, true); }

function mpSnapshot(s) {
  // Arrays of plain objects (no maps) so Unity's JsonUtility can read every message.
  var players = Object.keys(s.players).sort().map(function (k) { var p = s.players[k];
    return { id: k, name: p.name, team: p.team, char: p.char, ready: p.ready, online: !!p.presence, x: p.x, y: p.y, hand: p.hand }; });
  return { phase: s.phase, seed: s.seed, round: s.round, time: s.time, room: s.room, players: players,
    teams: s.teams.map(function (t) { return { stack: t.stack, budget: t.budget, wins: t.wins, submitted: t.submitted }; }) };
}
function mpBroadcast(d, s) { mpSend(d, OP.STATE, mpSnapshot(s)); }

function mpStartRound(s) {
  s.round++; s.room = makeRound(s.round, s.seed); s.phase = 'build'; s.time = ROUND_TIME;
  s.teams.forEach(function (t) { t.stack = []; t.budget = ROUND_BUDGET + Math.floor(t.carry / 2); t.submitted = false; });
  Object.keys(s.players).forEach(function (k) { var p = s.players[k]; p.hand = []; p.x = p.team ? 4.6 : 3.4; p.y = 0.6; });
}

function mpScoreRound(d, s, logger, nk) {
  var timeFrac = Math.max(0, Math.min(1, s.time / ROUND_TIME));
  var res = s.teams.map(function (t, i) {
    var ev = evalStack(t.stack, s.room.enemies, false);
    var sc = totalScore(ev.stats, s.room.spec, t.budget - ev.cost, t.budget, t.submitTime != null ? t.submitTime : timeFrac);
    return { team: i, stack: t.stack, stats: ev.stats, effects: ev.fx, cost: ev.cost, score: sc, left: t.budget - ev.cost };
  });
  var a = res[0], b = res[1];
  var winner = a.score.total !== b.score.total ? (a.score.total > b.score.total ? 0 : 1) : (a.left !== b.left ? (a.left > b.left ? 0 : 1) : -1);
  if (winner >= 0) s.teams[winner].wins++;
  s.teams.forEach(function (t, i) { t.carry = Math.max(0, res[i].left); });
  var over = s.teams[0].wins >= 2 || s.teams[1].wins >= 2 || s.round >= 3;
  s.phase = over ? 'result' : 'test'; s.testLeft = MP_TEST_SEC;
  mpSend(d, OP.RESULT, { round: s.round, winner: winner, teams: res, wins: [s.teams[0].wins, s.teams[1].wins], over: over });
  logEvent(nk, logger, null, s.label, 'round_result', { round: s.round, seed: s.seed, winner: winner, totals: [a.score.total, b.score.total], mode: 'mp' });
}

function matchInit(ctx, logger, nk, params) {
  var seed = parseInt(params && params.seed, 10) || (1 + Math.floor(Math.random() * 8388607));
  var s = { seed: seed, round: 0, phase: 'lobby', time: ROUND_TIME, room: null, players: {}, label: 'mp' + seed, testLeft: 0,
    teams: [{ stack: [], budget: ROUND_BUDGET, carry: 0, wins: 0, submitted: false }, { stack: [], budget: ROUND_BUDGET, carry: 0, wins: 0, submitted: false }] };
  return { state: s, tickRate: MP_TICK, label: JSON.stringify({ mode: 'bob_coop', open: true }) };
}
function matchJoinAttempt(ctx, logger, nk, d, tick, s, presence, metadata) {
  if (s.phase !== 'lobby' && !s.players[presence.userId]) return { state: s, accept: false, rejectMessage: 'match already started' };
  if (Object.keys(s.players).length >= MP_MAX && !s.players[presence.userId]) return { state: s, accept: false, rejectMessage: 'match full' };
  return { state: s, accept: true };
}
function matchJoin(ctx, logger, nk, d, tick, s, presences) {
  presences.forEach(function (p) {
    if (s.players[p.userId]) { s.players[p.userId].presence = p; return; } // reconnect
    var n = [0, 0]; Object.keys(s.players).forEach(function (k) { n[s.players[k].team]++; });
    var team = n[0] <= n[1] ? 0 : 1;
    s.players[p.userId] = { presence: p, name: p.username, team: team, char: 'sweep', ready: false, x: team ? 4.6 : 3.4, y: 0.6, hand: [] };
  });
  mpBroadcast(d, s);
  return { state: s };
}
function matchLeave(ctx, logger, nk, d, tick, s, presences) {
  presences.forEach(function (p) { if (s.phase === 'lobby') delete s.players[p.userId]; else if (s.players[p.userId]) s.players[p.userId].presence = null; });
  mpBroadcast(d, s);
  return { state: s };
}
function matchLoop(ctx, logger, nk, d, tick, s, messages) {
  var dirty = false, dt = 1 / MP_TICK;
  messages.forEach(function (m) {
    var p = s.players[m.sender.userId]; if (!p) return;
    var q; try { q = JSON.parse(nk.binaryToString(m.data) || '{}'); } catch (e) { q = {}; }
    var t = s.teams[p.team], err = null;
    switch (m.opCode) {
      case OP.READY:
        if (s.phase === 'lobby') { if (CHARS[q.char]) p.char = q.char; p.ready = q.ready !== false; dirty = true; }
        break;
      case OP.MOVE: {
        if (s.phase !== 'build') break;
        var x = +q.x, y = +q.y; if (!isFinite(x) || !isFinite(y)) break;
        // Allowed distance grows with the time since the last accepted move (max 1 s), so extra messages buy no speed.
        var dx = x - p.x, dy = y - p.y, dist = Math.hypot(dx, dy), max = MP_SPEED * Math.min(1, Math.max(1, tick - (p.moveTick || 0)) / MP_TICK);
        if (dist > max) { x = p.x + dx / dist * max; y = p.y + dy / dist * max; }
        var q2 = mpPushOut(Math.max(0.18, Math.min(7.82, x)), Math.max(0.18, Math.min(4.82, y))); p.x = q2[0]; p.y = q2[1];
        p.moveTick = tick;
        break;
      }
      case OP.PICK:
        if (s.phase !== 'build') break;
        if (!PARTS[q.id] || (s.room.offer.indexOf(q.id) < 0 && METHODS.indexOf(q.id) < 0)) err = 'ไม่มีชิ้นนี้ในรอบนี้';
        else if (mpDist(p.x, p.y, STATION_FAM[PARTS[q.id].f]) > NEAR) err = 'เดินไปที่จุดเก็บของก่อน';
        else if (p.hand.length >= MP_HAND) err = 'มือเต็มแล้ว ถือได้ ' + MP_HAND + ' ชิ้น';
        else { p.hand.push(q.id); dirty = true; }
        break;
      case OP.PLACE: {
        if (s.phase !== 'build' || t.submitted) break;
        var id = p.hand[q.i | 0];
        if (!id) break;
        if (mpDist(p.x, p.y, CART) > NEAR) { err = 'เดินไปที่รถเข็นก่อน'; break; }
        var next = t.stack.concat([id]);
        if (isMethod(id) && (!t.stack.length || isMethod(t.stack[t.stack.length - 1]))) err = 'วิธีใช้ต้องวางบนชิ้นส่วน';
        else if (countLayers(next) > MAX_LAYERS) err = 'รถเข็นเต็ม 5 ชั้นแล้ว';
        else if (countMethods(next) > MAX_METHODS) err = 'วิธีใช้ได้ไม่เกิน 2 ครั้ง';
        else if (stackCost(next) > t.budget) err = 'งบไม่พอ';
        else { t.stack = next; p.hand.splice(q.i | 0, 1); dirty = true; }
        break;
      }
      case OP.LIFT:
        if (s.phase !== 'build' || t.submitted || !t.stack.length) break;
        if (mpDist(p.x, p.y, CART) > NEAR) { err = 'เดินไปที่รถเข็นก่อน'; break; }
        var top = t.stack.pop(); if (p.hand.length < MP_HAND) p.hand.push(top); dirty = true;
        break;
      case OP.SUBMIT:
        if (s.phase === 'build' && !t.submitted) { t.submitted = true; t.submitTime = s.time / ROUND_TIME; dirty = true; }
        break;
    }
    if (err) mpSend(d, OP.ERROR, { t: err }, [m.sender]);
  });

  var ids = Object.keys(s.players);
  if (s.phase === 'lobby') {
    var teams = [0, 0], ready = true;
    ids.forEach(function (k) { teams[s.players[k].team]++; if (!s.players[k].ready) ready = false; });
    if (ready && ids.length >= 2 && teams[0] && teams[1]) {
      s.label = 'mp' + s.seed + '_' + tick; mpStartRound(s); dirty = true;
      d.matchLabelUpdate(JSON.stringify({ mode: 'bob_coop', open: false }));
      logEvent(nk, logger, null, s.label, 'match_start', { seed: s.seed, players: ids.length, mode: 'mp' });
    }
  } else if (s.phase === 'build') {
    s.time -= dt;
    if (s.time <= 0 || (s.teams[0].submitted && s.teams[1].submitted)) { s.time = Math.max(0, s.time); mpScoreRound(d, s, logger, nk); dirty = true; }
    mpSend(d, OP.POS, { time: s.time, p: ids.map(function (k) { return { id: k, x: +s.players[k].x.toFixed(2), y: +s.players[k].y.toFixed(2) }; }) });
  } else if (s.phase === 'test') {
    s.testLeft -= dt; if (s.testLeft <= 0) { mpStartRound(s); dirty = true; }
  } else if (s.phase === 'result') {
    s.testLeft -= dt; if (s.testLeft <= -30) return null; // end the match 30 s after the result
  }
  if (!ids.some(function (k) { return s.players[k].presence; }) && s.phase !== 'lobby') return null; // everyone left
  if (dirty) mpBroadcast(d, s);
  return { state: s };
}
function matchTerminate(ctx, logger, nk, d, tick, s, grace) { return { state: s }; }
function matchSignal(ctx, logger, nk, d, tick, s, data) { return { state: s, data: data }; }

// Matchmaker found 2-4 players: start one authoritative match for them.
function mpMatchmakerMatched(ctx, logger, nk, matches) { return nk.matchCreate('bob_coop', {}); }
// Private room: payload {"seed":123} (optional). Returns {"match_id":"..."}; share it, others join by id.
function rpcCreateMatch(ctx, logger, nk, payload) {
  var q = JSON.parse(payload || '{}');
  return JSON.stringify({ match_id: nk.matchCreate('bob_coop', { seed: String(q.seed || '') }) });
}

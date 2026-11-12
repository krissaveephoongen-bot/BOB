
/* ============ MULTIPLAYER: server-authoritative match 'bob_coop' (2-4 players, two teams) ============ */
// Every player in the match gets the same room per round (one seed). Each team shares one cart.
// Clients only send intents (move, pick, place, lift, submit); the server checks them and owns the state.
// Each team plays in its own copy of the same room (GDD 20), so both teams use the same coordinates.
// Opcodes, client -> server: 1 MOVE {x,y}  2 PICK {id}  3 PLACE {i}  4 LIFT {}  5 READY {char, ready?, team?}  6 SUBMIT {vote?} (a vote; half the team sends the cart)
//                            7 PUSH {on}  8 PASS {i, to?}  9 TABLE {put:i} or {take:k}  14 ABILITY {swap?:i} (once per player per round)
// Opcodes, server -> client: 10 STATE (full snapshot)  11 POS {time,p:[{id,x,y}],c:[{x,y}]} every tick  12 RESULT (round result)  13 ERROR {t}  15 PEEK {total, stats} (Spray's preview, sender only)
// Each team has its own clock (t.time) so Press can stop it; POS.tt carries both. s.time = the longer one, for old clients.
var OP = { MOVE: 1, PICK: 2, PLACE: 3, LIFT: 4, READY: 5, SUBMIT: 6, PUSH: 7, PASS: 8, TABLE: 9, STATE: 10, POS: 11, RESULT: 12, ERROR: 13, ABILITY: 14, PEEK: 15 };
var MP_FREEZE = 8; // Press: seconds the team's clock stops
var MP_TICK = 10, MP_MAX = 4, MP_HAND = 2, MP_TEST_SEC = 8, MP_SPEED = 2.3 * 1.25; // world units / s, with lag slack
var MP_PUSH_SPEED = 0.8, MP_PASS_NEAR = 1.0, MP_TABLE_MAX = 4, MP_RADIUS = 0.16;
// Room layout in world units [x0, y0, x1, y1], y up (the prototype's 800x500 px room / 100). Unity's Coop scene uses the same numbers.
var STATION_FAM = { tool: [0.3, 4.06, 2.0, 4.7], chem: [6.0, 4.06, 7.7, 4.7], energy: [0.3, 0.3, 2.0, 0.94], method: [6.0, 0.3, 7.7, 0.94] };
var TABLE = [3.5, 4.2, 4.5, 4.7];              // parking table (โต๊ะพักของ): leave parts for a team-mate
var SOFA = [3.0, 0.85, 5.0, 1.15];             // obstacle
var SOLIDS = [STATION_FAM.tool, STATION_FAM.chem, STATION_FAM.energy, STATION_FAM.method, TABLE, SOFA], RADIUS = MP_RADIUS;
var CART_W = 0.9, CART_H = 0.64, CART_START = [4.0, 2.53], NEAR = 0.6;
// Nakama keeps match state as Go values between ticks, where push/splice on an array can be lost: always assign a new array.
function without(a, i) { return a.slice(0, i).concat(a.slice(i + 1)); }
function cartRect(t) { return [t.cx - CART_W / 2, t.cy - CART_H / 2, t.cx + CART_W / 2, t.cy + CART_H / 2]; }
// Push a body of radius r out of one rect (same maths as BobGame.World and Coop/BobCoopLayout.cs, so phones predict it).
function mpPushRect(x, y, b, r) {
  var cx = Math.max(b[0], Math.min(b[2], x)), cy = Math.max(b[1], Math.min(b[3], y)), d = Math.hypot(x - cx, y - cy);
  if (d >= r) return [x, y];
  if (d > 0.0001) return [cx + (x - cx) / d * r, cy + (y - cy) / d * r];
  var opts = [[b[0] - r, y, x - b[0]], [b[2] + r, y, b[2] - x], [x, b[1] - r, y - b[1]], [x, b[3] + r, b[3] - y]]; // centre inside: nearest edge
  opts.sort(function (p, q) { return p[2] - q[2]; }); return [opts[0][0], opts[0][1]];
}
// A player: out of the shelves, the table, the sofa and (unless pushing it) the team's own cart, then inside the room.
// Bodies never block each other (GDD 20). extra = more rects, e.g. the team's cart.
function mpSolve(x, y, r, extra) {
  x = Math.max(0.18, Math.min(7.82, x)); y = Math.max(0.18, Math.min(4.82, y));
  var all = extra ? SOLIDS.concat(extra) : SOLIDS;
  for (var k = 0; k < all.length; k++) { var q = mpPushRect(x, y, all[k], r); x = q[0]; y = q[1]; }
  return [x, y];
}
// A pushed cart's centre stays in the room and out of the solids (its half height acts as the radius).
function mpCartSolve(x, y) {
  var p = mpSolve(x, y, CART_H / 2);
  return [Math.max(CART_W / 2, Math.min(8 - CART_W / 2, p[0])), Math.max(CART_H / 2, Math.min(5 - CART_H / 2, p[1]))];
}

function mpDist(x, y, r) { var cx = Math.max(r[0], Math.min(r[2], x)), cy = Math.max(r[1], Math.min(r[3], y)); return Math.hypot(x - cx, y - cy); }
function mpSend(d, op, obj, to) { d.broadcastMessage(op, JSON.stringify(obj), to || null, null, true); }

function mpSnapshot(s) {
  // Arrays of plain objects (no maps) so Unity's JsonUtility can read every message.
  var players = Object.keys(s.players).sort().map(function (k) { var p = s.players[k];
    return { id: k, name: p.name, team: p.team, char: p.char, ready: p.ready, online: !!p.presence, x: p.x, y: p.y, hand: p.hand, vote: !!p.vote, ab: !!p.abUsed }; });
  // cx, cy: the team's cart; pusher: user id pushing it ('' = nobody); table: parts parked on the team's table.
  return { phase: s.phase, seed: s.seed, round: s.round, time: s.time, room: s.room, players: players,
    teams: s.teams.map(function (t) { return { stack: t.stack, budget: t.budget, wins: t.wins, submitted: t.submitted,
      cx: t.cx, cy: t.cy, pusher: t.pusher || '', table: t.table,
      time: t.time, freeze: t.freeze, shield: !!t.shield }; }) };
}
function mpBroadcast(d, s) { mpSend(d, OP.STATE, mpSnapshot(s)); }

function mpStartRound(s) {
  s.round++; s.room = makeRound(s.round, s.seed); s.phase = 'build'; s.time = ROUND_TIME;
  s.teams.forEach(function (t) { t.stack = []; t.budget = ROUND_BUDGET + Math.floor(t.carry / 2); t.submitted = false;
    t.cx = CART_START[0]; t.cy = CART_START[1]; t.pusher = null; t.table = [];
    t.time = ROUND_TIME; t.freeze = 0; t.shield = false; t.submitTime = null; });
  Object.keys(s.players).forEach(function (k) { var p = s.players[k]; p.hand = []; p.vote = false; p.abUsed = false; p.x = p.team ? 4.6 : 3.4; p.y = 0.6; });
}

function mpScoreRound(d, s, logger, nk) {
  var res = s.teams.map(function (t, i) {
    var ev = evalStack(t.stack, s.room.enemies, !!t.shield);
    var sc = totalScore(ev.stats, s.room.spec, t.budget - ev.cost, t.budget, t.submitTime != null ? t.submitTime : 0);
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
    teams: [0, 1].map(function () { return { stack: [], budget: ROUND_BUDGET, carry: 0, wins: 0, submitted: false,
      cx: CART_START[0], cy: CART_START[1], pusher: null, table: [], time: ROUND_TIME, freeze: 0, shield: false }; }) };
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
  presences.forEach(function (p) {
    var pl = s.players[p.userId]; if (!pl) return;
    if (s.phase === 'lobby') { delete s.players[p.userId]; return; }
    pl.presence = null;
    // A dropped player never breaks the team (GDD 4): their parts go on the team's table for a team-mate, and the cart is let go.
    var t = s.teams[pl.team];
    if (t.pusher === p.userId) t.pusher = null;
    var room = Math.max(0, MP_TABLE_MAX - t.table.length);
    t.table = t.table.concat(pl.hand.slice(0, room)); pl.hand = pl.hand.slice(room);
  });
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
        if (s.phase === 'lobby') {
          if (CHARS[q.char]) p.char = q.char;
          p.ready = q.ready !== false;
          if ((q.team === 0 || q.team === 1) && q.team !== p.team) { // switch side in the lobby while the other side has room
            var on = 0; Object.keys(s.players).forEach(function (k) { if (s.players[k].team === q.team) on++; });
            if (on < MP_MAX / 2) p.team = q.team; else err = 'ทีมนั้นเต็มแล้ว';
          }
          dirty = true;
        }
        break;
      case OP.MOVE: {
        if (s.phase !== 'build') break;
        var x = +q.x, y = +q.y; if (!isFinite(x) || !isFinite(y)) break;
        // Allowed distance grows with the time since the last accepted move (max 1 s), so extra messages buy no speed.
        var pushing = t.pusher === m.sender.userId;
        var dx = x - p.x, dy = y - p.y, dist = Math.hypot(dx, dy),
          max = MP_SPEED * (pushing ? MP_PUSH_SPEED : 1) * Math.min(1, Math.max(1, tick - (p.moveTick || 0)) / MP_TICK);
        if (dist > max) { x = p.x + dx / dist * max; y = p.y + dy / dist * max; }
        var ox = p.x, oy = p.y, np = mpSolve(x, y, MP_RADIUS, pushing ? null : [cartRect(t)]);
        p.x = np[0]; p.y = np[1];
        p.moveTick = tick;
        if (pushing) { var c = mpCartSolve(t.cx + p.x - ox, t.cy + p.y - oy); t.cx = c[0]; t.cy = c[1]; }
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
        if (mpDist(p.x, p.y, cartRect(t)) > NEAR) { err = 'เดินไปที่รถเข็นก่อน'; break; }
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
        if (mpDist(p.x, p.y, cartRect(t)) > NEAR) { err = 'เดินไปที่รถเข็นก่อน'; break; }
        var top = t.stack.pop(); if (p.hand.length < MP_HAND) p.hand.push(top); dirty = true;
        break;
      case OP.PUSH: // grab or let go of the team's cart; while grabbed it moves with you (a little slower)
        if (s.phase !== 'build') break;
        if (q.on === false) { if (t.pusher === m.sender.userId) { t.pusher = null; dirty = true; } break; }
        if (t.submitted) { err = 'ส่งทดสอบแล้ว เข็นรถไม่ได้'; break; }
        if (t.pusher && t.pusher !== m.sender.userId && s.players[t.pusher] && s.players[t.pusher].presence) { err = 'เพื่อนกำลังเข็นอยู่'; break; }
        if (mpDist(p.x, p.y, cartRect(t)) > NEAR) { err = 'เดินไปที่รถเข็นก่อน'; break; }
        t.pusher = m.sender.userId; dirty = true;
        break;
      case OP.PASS: { // hand a part to a team-mate standing close; `to` is optional (nearest team-mate)
        if (s.phase !== 'build') break;
        var gi = q.i | 0, gid = p.hand[gi]; if (!gid) break;
        var best = null, bd = 1e9;
        Object.keys(s.players).forEach(function (k) {
          var o = s.players[k]; if (k === m.sender.userId || o.team !== p.team || !o.presence) return;
          if (q.to && q.to !== k) return;
          var dd = Math.hypot(o.x - p.x, o.y - p.y); if (dd < bd) { bd = dd; best = o; }
        });
        if (!best || bd > MP_PASS_NEAR) err = 'เดินไปใกล้เพื่อนก่อน';
        else if (best.hand.length >= MP_HAND) err = 'มือเพื่อนเต็มแล้ว';
        else { best.hand = best.hand.concat([gid]); p.hand = without(p.hand, gi); dirty = true; }
        break;
      }
      case OP.TABLE: // park a part on the team's table ({put: hand index}) or take one back ({take: table index})
        if (s.phase !== 'build') break;
        if (mpDist(p.x, p.y, TABLE) > NEAR) { err = 'เดินไปที่โต๊ะพักของก่อน'; break; }
        if (q.put != null) {
          var pi = q.put | 0; if (!p.hand[pi]) break;
          if (t.table.length >= MP_TABLE_MAX) err = 'โต๊ะเต็มแล้ว วางได้ ' + MP_TABLE_MAX + ' ชิ้น';
          else { t.table = t.table.concat([p.hand[pi]]); p.hand = without(p.hand, pi); dirty = true; }
        } else if (q.take != null) {
          var ti = q.take | 0; if (!t.table[ti]) break;
          if (p.hand.length >= MP_HAND) err = 'มือเต็มแล้ว ถือได้ ' + MP_HAND + ' ชิ้น';
          else { p.hand = p.hand.concat([t.table[ti]]); t.table = without(t.table, ti); dirty = true; }
        }
        break;
      case OP.ABILITY: { // the hero's ability, once per player per round (GDD 6)
        if (s.phase !== 'build' || t.submitted) break;
        if (p.abUsed) { err = 'ใช้ความสามารถพิเศษไปแล้วในรอบนี้'; break; }
        var used = true;
        if (p.char === 'sweep') { // swap layer i with the one above it
          var si = q.swap | 0;
          if (q.swap == null || si < 0 || si >= t.stack.length - 1) { err = 'เลือกชั้นที่จะสลับ'; used = false; }
          else if (mpDist(p.x, p.y, cartRect(t)) > NEAR) { err = 'เดินไปที่รถเข็นก่อน'; used = false; }
          else {
            var sw = t.stack.slice(); var tmp = sw[si]; sw[si] = sw[si + 1]; sw[si + 1] = tmp;
            var bad = isMethod(sw[0]); for (var k2 = 1; k2 < sw.length; k2++) if (isMethod(sw[k2]) && isMethod(sw[k2 - 1])) bad = true;
            if (bad) { err = 'สลับแล้ววิธีใช้ไม่มีชิ้นส่วนรองรับ'; used = false; } else t.stack = sw;
          }
        } else if (p.char === 'bubble') t.shield = true;
        else if (p.char === 'press') t.freeze = MP_FREEZE;
        else if (p.char === 'spray') {
          var pe = evalStack(t.stack, s.room.enemies, !!t.shield);
          var ps = totalScore(pe.stats, s.room.spec, t.budget - pe.cost, t.budget, Math.max(0, Math.min(1, t.time / ROUND_TIME)));
          mpSend(d, OP.PEEK, { total: ps.total, stats: pe.stats }, [m.sender]);
        } else { err = 'ฮีโร่นี้ไม่มีความสามารถพิเศษ'; used = false; }
        if (used) {
          p.abUsed = true; dirty = true;
          logEvent(nk, logger, m.sender.userId, s.label, 'ability_used', { round: s.round, char: p.char, ability: CHARS[p.char].ab, mode: 'mp' });
        }
        break;
      }
      case OP.SUBMIT:
        // UX spec BTN-Submit: in a team it is a vote, and the cart is sent when at least half of the online team-mates voted.
        if (s.phase === 'build' && !t.submitted) {
          p.vote = q.vote !== false; dirty = true;
          var mates = 0, votes = 0;
          Object.keys(s.players).forEach(function (k) { var o = s.players[k]; if (o.team !== p.team || !o.presence) return; mates++; if (o.vote) votes++; });
          if (votes * 2 >= mates) { t.submitted = true; t.pusher = null; t.submitTime = Math.max(0, Math.min(1, t.time / ROUND_TIME)); }
        }
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
    s.teams.forEach(function (t) {
      if (t.submitted) return;
      if (t.freeze > 0) t.freeze = Math.max(0, t.freeze - dt); else t.time = Math.max(0, t.time - dt);
      if (t.time <= 0) { t.submitted = true; t.pusher = null; t.submitTime = 0; dirty = true; } // time is up: the cart goes as it is
    });
    s.time = Math.max(s.teams[0].time, s.teams[1].time);
    if (s.teams[0].submitted && s.teams[1].submitted) { mpScoreRound(d, s, logger, nk); dirty = true; }
    mpSend(d, OP.POS, { time: s.time, tt: [+s.teams[0].time.toFixed(2), +s.teams[1].time.toFixed(2)], p: ids.map(function (k) { return { id: k, x: +s.players[k].x.toFixed(2), y: +s.players[k].y.toFixed(2) }; }),
      c: s.teams.map(function (t) { return { x: +t.cx.toFixed(2), y: +t.cy.toFixed(2) }; }) });
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

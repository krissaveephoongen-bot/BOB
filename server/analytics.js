
/* ============ ANALYTICS: play events -> Postgres (table bob_events) ============ */
// Every event is one row: who (Nakama user id), which match, what happened, and a JSON payload.
// The server writes `stack_scored` itself whenever it scores a stack, so that number cannot be faked.
// Clients add the rest (match start/end, round results, ability use) through bob_log_event.
var EVENT_SCHEMA = [
  'CREATE TABLE IF NOT EXISTS bob_events (' +
  ' id BIGSERIAL PRIMARY KEY,' +
  ' created_at TIMESTAMPTZ NOT NULL DEFAULT now(),' +
  ' user_id UUID,' +
  ' match_id TEXT,' +
  ' event TEXT NOT NULL,' +
  ' data JSONB NOT NULL DEFAULT \'{}\')',
  'CREATE INDEX IF NOT EXISTS bob_events_event_time ON bob_events (event, created_at)',
  'CREATE INDEX IF NOT EXISTS bob_events_match ON bob_events (match_id)'
];
var CLIENT_EVENTS = { match_start: 1, round_result: 1, ability_used: 1, match_end: 1 };
var MAX_EVENT_BYTES = 4096;

function cleanMatchId(m) {
  if (m == null) return null;
  m = String(m);
  if (!/^[A-Za-z0-9_-]{1,64}$/.test(m)) throw new Error('match id must be 1-64 letters, digits, _ or -');
  return m;
}
function logEvent(nk, logger, userId, matchId, event, data) {
  try {
    nk.sqlExec('INSERT INTO bob_events (user_id, match_id, event, data) VALUES ($1, $2, $3, $4::jsonb)',
      [userId || null, matchId || null, event, JSON.stringify(data || {})]);
  } catch (e) {
    // Analytics must never break gameplay: log and carry on.
    logger.error('bob analytics insert failed: %s', String(e));
  }
}

// payload: {"event":"round_result","match":"m123","data":{...}}
function rpcLogEvent(ctx, logger, nk, payload) {
  if (!ctx.userId) throw new Error('sign in first');
  if ((payload || '').length > MAX_EVENT_BYTES) throw new Error('event too large');
  var q = JSON.parse(payload || '{}');
  if (!CLIENT_EVENTS[q.event]) throw new Error('unknown event: ' + q.event);
  var data = q.data && typeof q.data === 'object' ? q.data : {};
  logEvent(nk, logger, ctx.userId, cleanMatchId(q.match), q.event, data);
  return JSON.stringify({ ok: true });
}

function q1(nk, sql, params) { return nk.sqlQuery(sql, params || []); }
function num(v) { return v == null ? null : Number(v); }

// Server-to-server only (call with ?http_key=...). Players cannot read other players' data.
// payload: {"days":7}
function rpcAnalytics(ctx, logger, nk, payload) {
  if (ctx.userId) throw new Error('analytics is server-to-server only; call with http_key');
  var q = JSON.parse(payload || '{}');
  var days = Math.max(1, Math.min(365, parseInt(q.days, 10) || 7));
  var since = 'now() - ($1 || \' days\')::interval';
  var P = [String(days)];

  var totals = q1(nk,
    'SELECT count(DISTINCT user_id) AS players,' +
    ' count(*) FILTER (WHERE event = \'match_start\') AS matches_started,' +
    ' count(*) FILTER (WHERE event = \'match_end\') AS matches_finished,' +
    ' count(*) FILTER (WHERE event = \'stack_scored\') AS stacks_scored,' +
    ' count(*) FILTER (WHERE event = \'round_result\') AS rounds_played,' +
    ' count(*) AS events' +
    ' FROM bob_events WHERE created_at > ' + since, P)[0];

  var daily = q1(nk,
    'SELECT to_char(date_trunc(\'day\', created_at), \'YYYY-MM-DD\') AS day, event, count(*) AS n' +
    ' FROM bob_events WHERE created_at > ' + since + ' GROUP BY 1, 2 ORDER BY 1, 2', P);

  // Characters: how often picked, how often the match was won, how often the ability got used.
  var chars = q1(nk,
    'WITH m AS (SELECT data->>\'char\' AS char, count(*) AS matches,' +
    '   avg(CASE WHEN (data->>\'wins\')::int > (data->>\'losses\')::int THEN 1.0 ELSE 0.0 END) AS win_rate' +
    '   FROM bob_events WHERE event = \'match_end\' AND created_at > ' + since + ' GROUP BY 1),' +
    ' a AS (SELECT data->>\'char\' AS char, count(*) AS used FROM bob_events' +
    '   WHERE event = \'ability_used\' AND created_at > ' + since + ' GROUP BY 1)' +
    ' SELECT m.char, m.matches, m.win_rate, coalesce(a.used, 0) AS abilities_used' +
    ' FROM m LEFT JOIN a ON a.char = m.char ORDER BY m.matches DESC', P);

  // Parts: from the server's own scoring, how often each part is used and what it scores.
  var parts = q1(nk,
    'SELECT p.part, count(*) AS uses, count(DISTINCT e.id) AS stacks,' +
    ' avg((e.data->\'score\'->>\'total\')::numeric) AS avg_total,' +
    ' avg(CASE WHEN (e.data->\'score\'->>\'pass\')::boolean THEN 1.0 ELSE 0.0 END) AS pass_rate' +
    ' FROM bob_events e, jsonb_array_elements_text(e.data->\'stack\') AS p(part)' +
    ' WHERE e.event = \'stack_scored\' AND e.created_at > ' + since +
    ' GROUP BY 1 ORDER BY 2 DESC', P);

  // Part win rate: rounds won against the rival when this part was in the stack.
  var partWins = q1(nk,
    'SELECT p.part, count(DISTINCT e.id) AS rounds,' +
    ' avg(CASE WHEN e.data->>\'result\' = \'win\' THEN 1.0 ELSE 0.0 END) AS win_rate' +
    ' FROM bob_events e, LATERAL (SELECT DISTINCT x FROM jsonb_array_elements_text(e.data->\'stack\') AS t(x)) AS p(part)' +
    ' WHERE e.event = \'round_result\' AND e.created_at > ' + since +
    ' GROUP BY 1', P);

  var rounds = q1(nk,
    'SELECT (data->>\'round\')::int AS round, count(*) AS n,' +
    ' avg((data->>\'my_total\')::numeric) AS avg_total,' +
    ' avg((data->>\'rival_total\')::numeric) AS avg_rival,' +
    ' avg(CASE WHEN data->>\'result\' = \'win\' THEN 1.0 ELSE 0.0 END) AS win_rate,' +
    ' avg(CASE WHEN (data->>\'pass\')::boolean THEN 1.0 ELSE 0.0 END) AS pass_rate,' +
    ' avg((data->>\'time_left\')::numeric) AS avg_time_left' +
    ' FROM bob_events WHERE event = \'round_result\' AND created_at > ' + since +
    ' GROUP BY 1 ORDER BY 1', P);

  // Enemy line-ups: which rooms are too hard (low spec pass rate).
  var enemies = q1(nk,
    'SELECT (SELECT string_agg(x, \' + \' ORDER BY x) FROM jsonb_array_elements_text(data->\'enemies\') AS t(x)) AS enemies,' +
    ' count(*) AS stacks,' +
    ' avg(CASE WHEN (data->\'score\'->>\'pass\')::boolean THEN 1.0 ELSE 0.0 END) AS pass_rate,' +
    ' avg((data->\'score\'->>\'total\')::numeric) AS avg_total' +
    ' FROM bob_events WHERE event = \'stack_scored\' AND created_at > ' + since +
    ' GROUP BY 1 ORDER BY 3', P);

  var layers = q1(nk,
    'SELECT jsonb_array_length(data->\'stack\') AS size, count(*) AS n,' +
    ' avg((data->\'score\'->>\'total\')::numeric) AS avg_total' +
    ' FROM bob_events WHERE event = \'stack_scored\' AND created_at > ' + since +
    ' GROUP BY 1 ORDER BY 1', P);

  var recent = q1(nk,
    'SELECT id, to_char(created_at, \'YYYY-MM-DD HH24:MI:SS\') AS at, user_id::text AS user_id, match_id, event, data::text AS data' +
    ' FROM bob_events ORDER BY id DESC LIMIT 40', []);

  var pw = {};
  partWins.forEach(function (r) { pw[r.part] = r; });
  return JSON.stringify({
    days: days,
    generated_at: new Date().toISOString(),
    totals: {
      players: num(totals.players), matches_started: num(totals.matches_started), matches_finished: num(totals.matches_finished),
      stacks_scored: num(totals.stacks_scored), rounds_played: num(totals.rounds_played), events: num(totals.events)
    },
    daily: daily.map(function (r) { return { day: r.day, event: r.event, n: num(r.n) }; }),
    characters: chars.map(function (r) {
      return { char: r.char, name: CHARS[r.char] ? CHARS[r.char].n : r.char, matches: num(r.matches), win_rate: num(r.win_rate), abilities_used: num(r.abilities_used) };
    }),
    parts: parts.map(function (r) {
      var w = pw[r.part] || {};
      return { part: r.part, name: PARTS[r.part] ? PARTS[r.part].n : r.part, family: PARTS[r.part] ? PARTS[r.part].f : null,
        cost: PARTS[r.part] ? PARTS[r.part].c : null, uses: num(r.uses), stacks: num(r.stacks), avg_total: num(r.avg_total),
        pass_rate: num(r.pass_rate), win_rate: num(w.win_rate), win_rounds: num(w.rounds) };
    }),
    rounds: rounds.map(function (r) {
      return { round: num(r.round), n: num(r.n), avg_total: num(r.avg_total), avg_rival: num(r.avg_rival), win_rate: num(r.win_rate),
        pass_rate: num(r.pass_rate), avg_time_left: num(r.avg_time_left) };
    }),
    enemies: enemies.map(function (r) {
      var names = (r.enemies || '').split(' + ').map(function (k) { return ENEMIES[k] ? ENEMIES[k].n : k; }).join(' + ');
      return { enemies: r.enemies, name: names, stacks: num(r.stacks), pass_rate: num(r.pass_rate), avg_total: num(r.avg_total) };
    }),
    stack_sizes: layers.map(function (r) { return { size: num(r.size), n: num(r.n), avg_total: num(r.avg_total) }; }),
    recent: recent.map(function (r) {
      return { id: num(r.id), at: r.at, user_id: r.user_id, match_id: r.match_id, event: r.event, data: JSON.parse(r.data) };
    })
  });
}

function createAnalyticsTables(nk) {
  EVENT_SCHEMA.forEach(function (sql) { nk.sqlExec(sql, []); });
}

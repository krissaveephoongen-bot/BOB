function rpcPing(ctx, logger, nk, payload) {
  return JSON.stringify({ ok: true, game: 'Battle of the Brooms', rules: 'v0.1', parts: Object.keys(PARTS).length });
}
// payload: {"seed":123,"round":1} -> the room both teams receive (same seed = same room)
function rpcMakeRound(ctx, logger, nk, payload) {
  var q = JSON.parse(payload || '{}');
  return JSON.stringify(makeRound(q.round || 1, q.seed || 1));
}
// payload: {"seed":123,"round":1,"stack":["hot","soap"],"budget":100,"timeFrac":0.4,"shield":false,"match":"m123","char":"sweep"}
// match and char are optional; they only label the analytics row this call writes.
function rpcScoreStack(ctx, logger, nk, payload) {
  var q = JSON.parse(payload || '{}');
  var R = makeRound(q.round || 1, q.seed || 1);
  var stack = (q.stack || []).filter(function (id) { return !!PARTS[id]; });
  var allowed = R.offer.concat(METHODS);
  var illegal = stack.filter(function (id) { return allowed.indexOf(id) < 0; });
  var budget = q.budget || ROUND_BUDGET;
  if (illegal.length) throw new Error('parts not offered this round: ' + illegal.join(','));
  if (countLayers(stack) > MAX_LAYERS || countMethods(stack) > MAX_METHODS) throw new Error('stack exceeds limits');
  if (stackCost(stack) > budget) throw new Error('stack exceeds budget');
  var ev = evalStack(stack, R.enemies, !!q.shield);
  var sc = totalScore(ev.stats, R.spec, budget - ev.cost, budget, q.timeFrac || 0);
  logEvent(nk, logger, ctx.userId, cleanMatchId(q.match), 'stack_scored', {
    seed: q.seed || 1, round: q.round || 1, char: CHARS[q.char] ? q.char : null, stack: stack, cost: ev.cost, budget: budget,
    time_frac: q.timeFrac || 0, shield: !!q.shield, stats: ev.stats, score: sc, enemies: R.enemies, need: R.spec.need
  });
  return JSON.stringify({ stats: ev.stats, effects: ev.fx, cost: ev.cost, score: sc, spec: R.spec, enemies: R.enemies });
}
function InitModule(ctx, logger, nk, initializer) {
  initializer.registerRpc('bob_ping', rpcPing);
  initializer.registerRpc('bob_make_round', rpcMakeRound);
  initializer.registerRpc('bob_score_stack', rpcScoreStack);
  // Nakama finds RPCs by reading this function, so every registerRpc must sit here, not in a helper.
  initializer.registerRpc('bob_log_event', rpcLogEvent);
  initializer.registerRpc('bob_analytics', rpcAnalytics);
  initializer.registerRpc('bob_create_match', rpcCreateMatch);
  initializer.registerMatch('bob_coop', { matchInit: matchInit, matchJoinAttempt: matchJoinAttempt, matchJoin: matchJoin,
    matchLeave: matchLeave, matchLoop: matchLoop, matchTerminate: matchTerminate, matchSignal: matchSignal });
  initializer.registerMatchmakerMatched(mpMatchmakerMatched);
  createAnalyticsTables(nk);
  logger.info('Battle of the Brooms module loaded');
}

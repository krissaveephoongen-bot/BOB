// LINE Login (docs 28): the phone opens LINE's authorize page, LINE hands the app back a one-time `code`, and this RPC
// swaps that code for a LINE profile. The channel secret never leaves the server: it lives in the server environment
// (server/.env locally, deploy/.env when public) and is not in the APK, the repo or the game's PlayerPrefs.
//
// Env: LINE_CHANNEL_ID, LINE_CHANNEL_SECRET, LINE_REDIRECT_URI (default bob://line-login).
// The redirect_uri must be byte-identical in three places: the LINE console, LINE_REDIRECT_URI here, and the authorize
// URL the client builds from line_status.

var LINE_TOKEN_URL = 'https://api.line.me/oauth2/v2.1/token';
var LINE_PROFILE_URL = 'https://api.line.me/v2/profile';

function lineConfig(ctx) {
  var env = (ctx && ctx.env) || {};
  return {
    id: env.LINE_CHANNEL_ID || '',
    secret: env.LINE_CHANNEL_SECRET || '',
    redirect: env.LINE_REDIRECT_URI || 'bob://line-login'
  };
}

function lineEnc(s) { return encodeURIComponent(s === null || s === undefined ? '' : String(s)); }

// Stable Nakama custom id for a LINE user. Hashing with the channel secret makes the id unguessable, so knowing one
// player's LINE userId does not let anyone sign in as them through the shared server key.
function lineCustomId(nk, lineUserId, secret) {
  var hex = '';
  try { hex = nk.hmacSha256Hash(lineUserId, secret); } catch (e) { hex = ''; }
  if (!hex) { try { hex = nk.sha256Hash(lineUserId + ':' + secret); } catch (e2) { hex = lineUserId; } }
  return 'line_' + hex;
}

// One HTTP call with a JSON body, keeping LINE's own error text when the call fails.
function lineJson(nk, url, method, headers, body) {
  var res = nk.httpRequest(url, method, headers, body || '', 15000);
  var json = {};
  try { json = JSON.parse((res && res.body) || '{}'); } catch (e) { json = {}; }
  return { code: (res && res.code) || 0, json: json };
}

// GET /v2/rpc/line_status -> {ok, channelId, redirect}. The client asks before opening the browser, so a player never
// walks through LINE only to be told the server has no secret. Never returns the secret.
function rpcLineStatus(ctx, logger, nk, payload) {
  var c = lineConfig(ctx);
  var ready = !!c.id && !!c.secret;
  return JSON.stringify({
    ok: ready, channelId: c.id, redirect: c.redirect,
    note: ready ? 'ready' : 'server is missing LINE_CHANNEL_ID / LINE_CHANNEL_SECRET'
  });
}

// POST body: {"code":"...","verifier":"..."} -> {customId, userId, lineUserId, displayName, picture, chatStatus}
// The client then calls AuthenticateCustomAsync(customId) to get its own Nakama session; this RPC also creates the
// Nakama account so the id exists before the client asks for it.
function rpcLineLogin(ctx, logger, nk, payload) {
  var c = lineConfig(ctx);
  if (!c.id || !c.secret) throw new Error('LINE login is not configured on this server (LINE_CHANNEL_ID / LINE_CHANNEL_SECRET)');
  var q = {};
  try { q = JSON.parse(payload || '{}'); } catch (e) { q = {}; }
  if (!q.code) throw new Error('missing code');

  // PKCE: LINE still wants client_secret even with S256, so both go in the same POST.
  var body = 'grant_type=authorization_code' +
    '&code=' + lineEnc(q.code) +
    '&redirect_uri=' + lineEnc(c.redirect) +
    '&client_id=' + lineEnc(c.id) +
    '&client_secret=' + lineEnc(c.secret);
  if (q.verifier) body += '&code_verifier=' + lineEnc(q.verifier);

  var tok = lineJson(nk, LINE_TOKEN_URL, 'POST',
    { 'Content-Type': 'application/x-www-form-urlencoded', Accept: 'application/json' }, body);
  if (!tok.json.access_token) {
    var why = tok.json.error_description || tok.json.error || 'no access_token';
    throw new Error('LINE token exchange failed (HTTP ' + tok.code + '): ' + why);
  }

  var prof = lineJson(nk, LINE_PROFILE_URL, 'GET',
    { Authorization: 'Bearer ' + tok.json.access_token, Accept: 'application/json' }, '');
  if (!prof.json.userId) throw new Error('LINE profile failed (HTTP ' + prof.code + '): no userId');

  var customId = lineCustomId(nk, prof.json.userId, c.secret);
  var acc = null;
  try { acc = nk.authenticateCustom(customId, null, true); } // creates the Nakama account if this is the first login
  catch (e) { logger.warn('line_login: authenticateCustom failed for ' + customId + ': ' + e.message); }

  logger.info('line_login: ' + (prof.json.displayName || '(no name)') + ' -> nakama ' + (acc ? acc.userId : '(client will create)'));
  return JSON.stringify({
    customId: customId,
    userId: acc ? acc.userId : null,
    created: acc ? !!acc.create : null,
    lineUserId: prof.json.userId,
    displayName: prof.json.displayName || '',
    picture: prof.json.pictureUrl || '',
    chatStatus: tok.json.status_message || ''
  });
}

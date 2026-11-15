// Offline test for the LINE Login RPCs (docs 28). No Nakama needed: the built bundle is loaded into a VM with a fake
// nk, so it checks the exchange, the hashed custom id and the error paths that a real phone would hit.
//   node line-auth-test.js        (after node build-module.js)
const fs = require('fs'), path = require('path'), vm = require('vm');

const src = fs.readFileSync(path.join(__dirname, 'modules/index.js'), 'utf8');
const sandbox = {};
vm.createContext(sandbox);
vm.runInContext(src + '\n;this.api = { status: rpcLineStatus, login: rpcLineLogin };', sandbox);
const api = sandbox.api;

const logger = { info: () => {}, warn: (m) => console.log('  warn:', m) };
let fails = 0;
function check(name, ok, extra) {
  console.log((ok ? 'ok   ' : 'FAIL ') + name + (extra !== undefined ? '  ' + JSON.stringify(extra) : ''));
  if (!ok) fails++;
}

// 1. No secret configured: the client must be told before it opens a browser.
const st0 = JSON.parse(api.status({ env: {} }, logger, {}, '{}'));
check('status without env says not ready', st0.ok === false && st0.note.indexOf('LINE_CHANNEL_ID') >= 0, st0);
check('status never leaks the secret', JSON.stringify(st0).indexOf('secret') < 0, st0);

// 2. Configured: public fields only.
const st1 = JSON.parse(api.status({ env: { LINE_CHANNEL_ID: '2012623256', LINE_CHANNEL_SECRET: 'shh' } }, logger, {}, '{}'));
check('status ready + redirect default', st1.ok === true && st1.redirect === 'bob://line-login', st1);

// 3. Login without config throws a readable error.
let msg = '';
try { api.login({ env: {} }, logger, {}, '{"code":"x"}'); } catch (e) { msg = e.message; }
check('login without config throws', msg.indexOf('not configured') >= 0, msg);

// 4. Happy path: token -> profile -> hashed custom id.
const calls = [];
const nk = {
  httpRequest: (url, method, headers, body) => {
    calls.push({ url: url, method: method, headers: headers, body: body });
    if (url.indexOf('/token') >= 0) return { code: 200, body: JSON.stringify({ access_token: 'tok', status_message: 'ok' }) };
    return { code: 200, body: JSON.stringify({ userId: 'U1234567890', displayName: 'Jak', pictureUrl: 'https://x/y.png' }) };
  },
  hmacSha256Hash: (input, key) => 'h-' + input + '-' + key,
  authenticateCustom: () => ({ userId: 'nakama-1', create: true })
};
const out = JSON.parse(api.login({ env: { LINE_CHANNEL_ID: '2012623256', LINE_CHANNEL_SECRET: 'sec', LINE_REDIRECT_URI: 'bob://line-login' } },
  logger, nk, '{"code":"abc","verifier":"v1"}'));
check('token POST carries secret + verifier', calls[0].body.indexOf('client_secret=sec') >= 0 && calls[0].body.indexOf('code_verifier=v1') >= 0, calls[0].body);
check('profile GET uses bearer', calls[1].headers.Authorization === 'Bearer tok', calls[1].headers);
check('custom id is hashed, not the raw LINE id', out.customId === 'line_h-U1234567890-sec', out.customId);
check('profile fields returned', out.displayName === 'Jak' && out.userId === 'nakama-1', out);

// 5. Bad code: LINE's own error text reaches the caller.
const bad = { httpRequest: () => ({ code: 400, body: JSON.stringify({ error: 'invalid_grant', error_description: 'code expired' }) }) };
msg = '';
try { api.login({ env: { LINE_CHANNEL_ID: 'a', LINE_CHANNEL_SECRET: 'b' } }, logger, bad, '{"code":"old"}'); } catch (e) { msg = e.message; }
check('expired code throws with LINE text', msg.indexOf('code expired') >= 0, msg);

// 6. hmac missing: falls back to sha256, still not the raw id.
const noHmac = { httpRequest: nk.httpRequest, sha256Hash: (s) => 's256-' + s, authenticateCustom: nk.authenticateCustom };
const out2 = JSON.parse(api.login({ env: { LINE_CHANNEL_ID: 'a', LINE_CHANNEL_SECRET: 'b' } }, logger, noHmac, '{"code":"abc"}'));
check('sha256 fallback when hmac is missing', out2.customId.indexOf('line_s256-') === 0, out2.customId);

console.log(fails === 0 ? '\nall LINE auth checks passed' : '\n' + fails + ' check(s) failed');
process.exit(fails === 0 ? 0 : 1);

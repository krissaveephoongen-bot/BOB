// The smallest server that can answer the phone's daily question (docs 31 section 2.2): one route, no database.
//   node server/daily-server.js            (PORT from the environment, 8099 by default)
//   GET /health        -> {ok:true}
//   GET /daily/seed    -> {ok, day, dayNumber, seed, rounds, nextDayInSec}
//
// It is the same code a Nakama deployment answers with (bob_daily), because both call server/daily.js. The endpoint has
// no idea who the player is: the seed is the same for everyone, so there is nothing to authenticate and nothing to keep.
const http = require('http');
const { dailyHandle } = require('./daily.js');

const port = parseInt(process.env.PORT || '8099', 10);

const server = http.createServer((req, res) => {
  const out = dailyHandle(req.method, req.url);
  const body = JSON.stringify(out.body);
  res.writeHead(out.status, {
    'content-type': 'application/json; charset=utf-8',
    'cache-control': 'no-store',
    'content-length': Buffer.byteLength(body)
  });
  res.end(req.method === 'HEAD' ? '' : body);
});

server.listen(port, () => {
  console.log('[bob-daily] listening on ' + port + ' — GET /daily/seed');
});

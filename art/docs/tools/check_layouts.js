#!/usr/bin/env node
// Checks the room layouts in art/docs/ux/BOB_Level_Layouts.html (doc 19, section 6) and runs the walk simulation.
// Usage: node art/docs/tools/check_layouts.js [runs]   -> exits 1 if any room breaks a rule.
// The layout data and the rules live in the page's <script id="bob-layout-logic"> block, so the page and this
// check always read the same numbers. Edit the layouts there, then run this before committing.
const fs = require('fs'), path = require('path'), vm = require('vm');
const html = fs.readFileSync(path.join(__dirname, '..', 'ux', 'BOB_Level_Layouts.html'), 'utf8');
const m = html.match(/<script id="bob-layout-logic">([\s\S]*?)<\/script>/);
if (!m) { console.error('logic block not found'); process.exit(2); }
const ctx = {}; vm.createContext(ctx); vm.runInContext(m[1] + '\nthis.BOBL=BOBL;', ctx);
const B = ctx.BOBL, runs = +process.argv[2] || 200;
let bad = 0;
for (const room of B.ROOMS) {
  const A = B.analyse(room), f = A.fairRes;
  console.log(`\n${room.code} ${room.name}: ${A.issues.length ? 'FAIL' : 'ok'}`);
  A.issues.forEach(i => console.log('  - ' + i)); bad += A.issues.length;
  console.log(`  min spacing ${A.minSp.toFixed(2)} (${A.minPair}) · T04 path ${A.longRes[0].d.toFixed(1)} · spawn spread cart/shelf/mean ${f.cart.toFixed(2)}/${f.shelf.toFixed(2)}/${f.mean.toFixed(2)} · mean leg ${A.meanPair.toFixed(1)}`);
  for (const n of [1, 2, 3, 4]) {
    const s = B.simStats(A, n, runs);
    console.log(`  ${n}p: steps ${s.steps.toFixed(1)} · finish ${s.finish.toFixed(1)}s (p90 ${s.p90.toFixed(1)}) · walk ${(s.walkShare * 100).toFixed(0)}% · leg ${s.meanLeg.toFixed(1)} (max ${s.maxLeg.toFixed(1)})`);
  }
}
process.exit(bad ? 1 : 0);

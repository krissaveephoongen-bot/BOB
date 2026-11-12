// Renders docs/sheets/*.svg to PNG (same folder). Needs Node + Playwright with Chromium.
const fs = require('fs'), path = require('path');
const { chromium } = require('playwright');
const DIR = path.join(path.dirname(__dirname), 'docs', 'sheets');
(async () => {
  const b = await chromium.launch(); const p = await b.newPage({ deviceScaleFactor: 1 });
  for (const f of fs.readdirSync(DIR).filter(f => f.endsWith('.svg'))) {
    await p.setViewportSize({ width: 1600, height: 1000 });
    await p.goto('file://' + path.join(DIR, f));
    await p.screenshot({ path: path.join(DIR, f.replace('.svg', '.png')) });
  }
  await b.close(); console.log('rendered sheets');
})();

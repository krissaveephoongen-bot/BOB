// Rasterises every SVG listed in the manifest to a PNG next to the Unity Sprites folder, with transparent background.
// Needs Node + Playwright with Chromium:  npm i -D playwright && npx playwright install chromium
//   node tools/render_png.js
const fs = require('fs'), path = require('path');
const { chromium } = require('playwright');
const ROOT = path.dirname(__dirname);
const ART = path.join(ROOT, 'unity', 'Assets', 'Art');
const manifest = JSON.parse(fs.readFileSync(path.join(ART, 'bob-art-manifest.json'), 'utf8'));

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage({ deviceScaleFactor: 1 });
  for (const s of manifest.sprites) {
    const svg = fs.readFileSync(path.join(ROOT, 'source', 'svg', s.path + '.svg'), 'utf8');
    const W = s.w * s.frames, H = s.h;
    await page.setViewportSize({ width: W, height: H });
    await page.setContent(`<html><body style="margin:0;background:transparent">${svg}</body></html>`);
    const out = path.join(ART, s.path + '.png');
    fs.mkdirSync(path.dirname(out), { recursive: true });
    await page.locator('body > svg').screenshot({ path: out, omitBackground: true });
  }
  await browser.close();
  console.log('rendered', manifest.sprites.length, 'PNGs');
})();

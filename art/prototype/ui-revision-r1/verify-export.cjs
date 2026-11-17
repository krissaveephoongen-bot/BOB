'use strict';
// Local-only browser verification/export. Does not modify design source files.
// Run: node C:/Users/Jakgrits/BOB-repo/art/prototype/ui-revision-r1/verify-export.cjs
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const { spawnSync } = require('node:child_process');
const { chromium } = require('C:/Users/Jakgrits/AppData/Local/npm-cache/_npx/9833c18b2d85bc59/node_modules/playwright');
const root = __dirname;
const sourceNames = ['index.html', 'fixtures.js', 'prepare.cjs'];
const hash = value => crypto.createHash('sha256').update(value).digest('hex');
const hashes = () => Object.fromEntries(sourceNames.map(n => [n, hash(fs.readFileSync(path.join(root, n)))]));
const run = new Date().toISOString().replace(/[:.]/g, '-');
const out = path.join(root, 'exports', run);
const ev = path.join(root, 'evidence', run);
const runtime = path.join(ev, 'browser-runtime');
for (const d of [out, ev, runtime]) fs.mkdirSync(d, { recursive: true });
process.env.TEMP = runtime;
process.env.TMP = runtime;
const report = { run, scope: 'Local browser design only; no Unity, actual phone or Figma import validation', node: process.version, sourceHashesBefore: hashes(), tests: [], exports: [], console: [], requests: [], failures: [], findings: [] };
const saveReport = () => fs.writeFileSync(path.join(ev, 'verification.json'), JSON.stringify(report, null, 2));
let browser;
const delay = ms => new Promise(resolve => setTimeout(resolve, ms));
function check(name, pass, observed) {
  report.tests.push({ name, status: pass ? 'pass' : 'fail', observed });
  if (!pass) console.log('FAIL ' + name + ': ' + JSON.stringify(observed));
}
(async () => {
  browser = await chromium.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: true, downloadsPath: runtime, env: { ...process.env, TEMP: runtime, TMP: runtime } });
  report.browser = browser.version();
  const context = await browser.newContext({ viewport: { width: 1920, height: 1300 }, deviceScaleFactor: 1, reducedMotion: 'reduce' });
  const page = await context.newPage();
  page.setDefaultTimeout(8000);
  page.on('console', m => report.console.push({ type: m.type(), text: m.text(), location: m.location() }));
  page.on('pageerror', e => report.failures.push({ type: 'pageerror', message: e.message }));
  page.on('requestfailed', r => report.failures.push({ type: 'requestfailed', url: r.url().slice(0, 180), reason: r.failure() }));
  page.on('response', r => { if (r.url().startsWith('http')) report.requests.push({ url: r.url(), status: r.status() }); });
  await page.addInitScript(() => {
    window.__verificationInputs = [];
    for (const type of ['pointerdown', 'pointerup', 'keydown', 'keyup', 'click']) {
      document.addEventListener(type, e => window.__verificationInputs.push({ type, trusted: e.isTrusted, target: e.target.closest?.('[id]')?.id, key: e.key, at: performance.now() }), true);
    }
  });
  const cdp = await context.newCDPSession(page);
  await cdp.send('Network.enable');
  await cdp.send('Network.setCacheDisabled', { cacheDisabled: true });
  await page.goto('http://127.0.0.1:8768/?verify=' + run, { waitUntil: 'networkidle' });
  await page.reload({ waitUntil: 'networkidle' });
  await page.evaluate(() => document.fonts.ready);
  report.loadedIndexHash = hash(await page.evaluate(async () => (await fetch('index.html', { cache: 'no-store' })).text()));
  report.loadedFixturesHash = hash(await page.evaluate(async () => (await fetch('fixtures.js', { cache: 'no-store' })).text()));
  report.fonts = await page.evaluate(() => ({ status: document.fonts.status, regular: document.fonts.check('400 30px "IBM Plex Sans Thai"', 'เรียก'), bold: document.fonts.check('700 30px "IBM Plex Sans Thai"', 'เรียก'), faces: [...document.fonts].map(f => ({ family: f.family, weight: f.weight, status: f.status })) }));
  report.assetDecodes = await page.evaluate(async () => Promise.all(Object.entries(window.BOB_DESIGN_ASSETS).map(async ([name, src]) => {
    const im = new Image(); im.src = src;
    try { await im.decode(); return { name, embedded: src.startsWith('data:image/'), width: im.naturalWidth, height: im.naturalHeight, ok: true }; }
    catch (e) { return { name, ok: false, error: String(e) }; }
  })));
  report.fixture = await page.evaluate(() => ({ seed: BOB_DESIGN_DATA.seed, empty: BOB_DESIGN_DATA.scores[''], good: BOB_DESIGN_DATA.scores['sponge|brush|steam|cloth'], brush: BOB_DESIGN_DATA.scores.brush }));
  check('Thai regular and bold fonts loaded', report.fonts.regular && report.fonts.bold && report.fonts.faces.every(f => f.status === 'loaded'), report.fonts);
  check('All embedded raster assets decode', report.assetDecodes.every(a => a.ok), report.assetDecodes);
  const state = () => page.evaluate(() => ({ mode, W, picked: [...picked], stack: [...stack], locked, hint, score: BOB_DESIGN_DATA.scores[stack.join('|')], cost: stack.reduce((n, id) => n + BOB_DESIGN_DATA.parts[id].c, 0), label: document.querySelector('#Cart-lock')?.textContent, toast: !!document.querySelector('#Overlay-toast'), arrow: !!document.querySelector('#Overlay-edge-arrow') }));
  async function selectNative(selector, value) {
    const options = await page.locator(selector + ' option').evaluateAll(options => options.map(o => o.value));
    const i = options.indexOf(String(value));
    if (i < 0) throw new Error('Missing option ' + value);
    await page.locator(selector).click();
    await page.keyboard.press('Home');
    for (let n = 0; n < i; n++) await page.keyboard.press('ArrowDown');
    await page.keyboard.press('Enter');
    await page.waitForFunction(({selector, value}) => document.querySelector(selector).value === String(value), {selector, value});
  }
  const choose = value => selectNative('#screen', value);
  const click = selector => page.locator(selector).click();
  async function drag(id) {
    await page.locator('#Tray-' + id).scrollIntoViewIfNeeded();
    const a = await page.locator('#Tray-' + id).boundingBox();
    const b = await page.locator('#Cart-drop-zone').boundingBox();
    if (!a || !b) throw new Error('Missing drag source/target ' + id);
    await page.mouse.move(a.x + a.width / 2, a.y + a.height / 2);
    await page.mouse.down();
    await page.mouse.move(b.x + b.width / 2, b.y + b.height / 2, { steps: 18 });
    await page.mouse.up();
  }
  async function hold(ms) {
    await page.locator('#Cart-lock').scrollIntoViewIfNeeded();
    const b = await page.locator('#Cart-lock').boundingBox();
    await page.mouse.move(b.x + b.width / 2, b.y + b.height / 2);
    await page.mouse.down();
    // Deliberate interaction duration, not process polling.
    await delay(ms);
    await page.mouse.up();
  }
  async function pick(ids) {
    await choose('cart-pick');
    for (const id of ids) await click('#Shelf-' + id);
    const transition = await state();
    await page.waitForSelector('#Cart-drop-zone');
    return transition;
  }
  async function shot(name, designSize = false) {
    await page.locator('#stage').scrollIntoViewIfNeeded();
    await page.mouse.move(0, 0);
    const b = await page.locator('#stage > svg').boundingBox();
    const pos = await page.evaluate(() => ({ x: scrollX, y: scrollY, w: W }));
    const { data } = await cdp.send('Page.captureScreenshot', { format: 'png', captureBeyondViewport: true, fromSurface: true, clip: { x: b.x + pos.x, y: b.y + pos.y, width: b.width, height: b.height, scale: designSize ? pos.w / b.width : 1 } });
    const file = path.join(ev, name + '.png'); fs.writeFileSync(file, Buffer.from(data, 'base64')); return file;
  }
  async function scenario(name, fn) {
    try { await fn(); } catch (e) { report.tests.push({ name, status: 'error', error: e.stack }); report.failures.push({ type: 'scenario', name, error: e.message }); console.log('ERROR ' + name + ': ' + e.message); }
    saveReport();
  }
  for (const width of [1920, 2400]) {
    await selectNative('#ratio', width);
    await scenario('good cart ' + width, async () => {
      const transition = await pick(['sponge', 'brush', 'steam', 'cloth']);
      check('Fourth pick auto-advances ' + width, transition.picked.length === 4 && ['cart-picked', 'cart-build'].includes(transition.mode) && (await state()).mode === 'cart-build', transition);
      for (const id of ['sponge', 'brush', 'steam', 'cloth']) await drag(id);
      let s = await state();
      check('Dragged good stack has 10/7/3/6 cost75 pass4 ' + width, s.stack.join('|') === 'sponge|brush|steam|cloth' && s.score.stats.join('/') === '10/7/3/6' && s.cost === 75 && s.score.pass === 4, s);
      await hold(250); await delay(420); s = await state();
      check('Short 250ms hold stays unlocked ' + width, !s.locked, s);
      await shot('interaction-cart-ready-' + width, true);
      await hold(650); s = await state();
      check('600ms threshold hold locks with buff ' + width, s.locked && s.mode === 'cart-locked' && s.score.passed && s.score.pass === 4, s);
      await shot('interaction-cart-locked-' + width, true);
    });
    await scenario('empty and incomplete ' + width, async () => {
      await pick(['sponge', 'brush', 'steam', 'cloth']);
      await hold(650); let s = await state();
      check('Empty cart locks without buff ' + width, s.locked && !s.score.passed && s.score.pass === 0 && s.mode === 'cart-no-buff', s);
      await shot('interaction-empty-locked-' + width, true);
      await pick(['sponge', 'brush', 'steam', 'cloth']);
      await drag('brush'); await hold(650); s = await state();
      check('Incomplete brush locks without buff ' + width, s.locked && !s.score.passed && s.score.stats.join('/') === '4/0/1/0' && s.mode === 'cart-no-buff', s);
    });
    await scenario('invalid soak ' + width, async () => {
      await pick(['soak', 'sponge', 'brush', 'cloth']);
      await drag('soak'); const s = await state();
      check('Soak rejected on empty cart, no cost ' + width, !s.stack.length && s.cost === 0 && s.hint.includes('วิธีใช้') && await page.locator('#Tray-soak').count() === 1, s);
      await shot('interaction-invalid-soak-' + width, true);
    });
    await scenario('top layer removal ' + width, async () => {
      await pick(['sponge', 'brush', 'steam', 'cloth']);
      for (const id of ['sponge', 'brush', 'steam', 'cloth']) await drag(id);
      const height = await page.locator('#Stack-layer-3 > rect').getAttribute('height');
      check('Top layer touch surface is 144 design units ' + width, Number(height) >= 144, height);
      await click('#Stack-layer-0');
      check('Lower layer cannot be removed ' + width, (await state()).stack.length === 4, await state());
      for (let i = 3; i >= 0; i--) await click('#Stack-layer-' + i);
      const s = await state();
      check('Top layers remove in order without returning to tray ' + width, !s.stack.length && !s.picked.length && s.cost === 0, s);
      await shot('interaction-cart-removed-' + width, true);
    });
    await scenario('call toast ' + width, async () => {
      await choose('arrow');
      const start = Date.now(); await click('#HUD-ping');
      let s = await state();
      check('Call immediately shows toast without arrow ' + width, s.toast && !s.arrow && (await page.locator('#Overlay-toast').textContent()).includes('เรียก'), s);
      await shot('interaction-call-toast-' + width, true);
      const left = 1850 - (Date.now() - start); if (left > 0) await delay(left);
      s = await state(); check('Toast still visible before 2.2s ' + width, s.toast && !s.arrow, { elapsed: Date.now() - start, ...s });
      await page.waitForSelector('#Overlay-edge-arrow', { timeout: 1000 }); s = await state();
      check('Arrow returns after toast ' + width, !s.toast && s.arrow, { elapsed: Date.now() - start, ...s });
    });
    await scenario('character and pause routes ' + width, async () => {
      await choose('character'); await click('#Character-done'); check('Character done closes to Home ' + width, (await state()).mode === 'home', await state());
      await click('#Home-play'); await click('#HUD-pause'); check('HUD pause opens pause ' + width, (await state()).mode === 'pause', await state());
      await click('#Pause-resume'); check('Pause resume returns to HUD ' + width, (await state()).mode === 'hud', await state());
      await click('#HUD-pause'); await click('#Pause-home'); check('Pause home opens reference ' + width, (await state()).mode === 'home', await state());
      await click('#Home-character'); await click('#Character-close'); check('Character close also returns Home ' + width, (await state()).mode === 'home', await state());
    });
  }
  await scenario('keyboard lock', async () => {
    await choose('cart-build');
    // Native Tab navigation, never DOM-dispatched events or element.focus().
    await page.keyboard.press('Tab');
    for (let n = 0; n < 20 && !(await page.locator('#Cart-lock').evaluate(el => el === document.activeElement)); n++) await page.keyboard.press('Tab');
    const focused = await page.locator('#Cart-lock').evaluate(el => el === document.activeElement);
    check('Keyboard Tab reaches lock', focused, await page.evaluate(() => document.activeElement.id));
    if (focused) { await page.keyboard.down('Space'); await delay(650); await page.keyboard.up('Space'); const s = await state(); check('Keyboard hold locks empty cart without buff', s.locked && !s.score.passed, s); }
  });
  // Small landscape review shell: real interactions, not a device/phone validation.
  await page.setViewportSize({ width: 667, height: 375 });
  for (const width of [1920, 2400]) {
    await selectNative('#ratio', width);
    await scenario('small landscape ' + width, async () => {
      await pick(['sponge', 'brush', 'steam', 'cloth']);
      for (const id of ['sponge', 'brush', 'steam', 'cloth']) await drag(id);
      await hold(650); const s = await state();
      check('Small landscape cart flow ' + width, s.locked && s.score.pass === 4 && s.cost === 75, s);
    });
    for (const screen of ['hud', 'toast', 'cart-ready', 'character', 'components']) {
      await choose(screen);
      await shot('small-667x375-' + width + '-' + screen);
      report.tests.push({ name: 'Small layout measurements ' + width + ' ' + screen, status: 'observation', observed: await page.evaluate(() => {
        const box = document.querySelector('#stage svg').getBoundingClientRect();
        return { viewport: [innerWidth, innerHeight], pageWidth: document.documentElement.scrollWidth, stage: { width: box.width, height: box.height }, below44px: [...document.querySelectorAll('#stage [role="button"]')].map(el => { const b = el.getBoundingClientRect(); return { id: el.id, width: b.width, height: b.height }; }).filter(b => b.width < 44 || b.height < 44) };
      }) });
    }
    await page.evaluate(() => scrollTo(0, 0));
    await page.screenshot({ path: path.join(ev, 'small-review-shell-' + width + '.png'), fullPage: true });
  }
  report.inputs = await page.evaluate(() => window.__verificationInputs);
  check('Behavior inputs are native trusted browser events', report.inputs.length > 0 && report.inputs.every(e => e.trusted), { count: report.inputs.length, untrusted: report.inputs.filter(e => !e.trusted) });
  // Fixture APIs are used only below, to select review states for export/screenshots.
  await page.setViewportSize({ width: 1920, height: 1300 });
  const screens = await page.locator('#screen option').evaluateAll(options => options.map(o => o.value).filter(v => v !== 'home'));
  const audit = () => {
    const svg = document.querySelector('#stage svg'); const W = svg.viewBox.baseVal.width;
    const nodes = [...svg.querySelectorAll('text')].map(el => { const b = el.getBBox(), m = el.getCTM(); const pts = [[b.x,b.y],[b.x+b.width,b.y+b.height]].map(([x,y]) => new DOMPoint(x,y).matrixTransform(m)); const r = el.getBoundingClientRect(); return { text: el.textContent, group: el.closest('g[id]')?.id || '', x: pts[0].x, y: pts[0].y, right: pts[1].x, bottom: pts[1].y, client: { x: r.x, y: r.y, right: r.right, bottom: r.bottom } }; });
    // BBox is in SVG CSS viewport scale via getCTM; root scaling is normalized here.
    const scale = svg.getBoundingClientRect().width / W;
    for (const n of nodes) for (const key of ['x','y','right','bottom']) n[key] /= scale;
    const outside = nodes.filter(n => n.x < -0.5 || n.y < -0.5 || n.right > W + 0.5 || n.bottom > 1080.5);
    const overlaps = [];
    for (let i=0;i<nodes.length;i++) for(let j=i+1;j<nodes.length;j++) { const a=nodes[i],b=nodes[j]; const x=Math.min(a.right,b.right)-Math.max(a.x,b.x), y=Math.min(a.bottom,b.bottom)-Math.max(a.y,b.y); if(x>1 && y>1) overlaps.push({ a:a.text,b:b.text,groups:[a.group,b.group],intersection:{width:x,height:y} }); }
    const ctx = document.createElement('canvas').getContext('2d');
    const inkBounds = [...svg.querySelectorAll('text')].map(el => {
      const style = getComputedStyle(el); ctx.font = `${style.fontWeight} ${style.fontSize} ${style.fontFamily}`;
      ctx.textAlign = ({start:'left',middle:'center',end:'right'})[el.getAttribute('text-anchor')] || 'left';
      const metrics = ctx.measureText(el.textContent), x = Number(el.getAttribute('x')), y = Number(el.getAttribute('y')), m = el.getCTM();
      const a = new DOMPoint(x-metrics.actualBoundingBoxLeft,y-metrics.actualBoundingBoxAscent).matrixTransform(m), b = new DOMPoint(x+metrics.actualBoundingBoxRight,y+metrics.actualBoundingBoxDescent).matrixTransform(m);
      return {text:el.textContent,group:el.closest('g[id]')?.id||'',x:a.x/scale,y:a.y/scale,right:b.x/scale,bottom:b.y/scale};
    });
    const inkOverlaps=[];
    for(let i=0;i<inkBounds.length;i++) for(let j=i+1;j<inkBounds.length;j++){const a=inkBounds[i],b=inkBounds[j],x=Math.min(a.right,b.right)-Math.max(a.x,b.x),y=Math.min(a.bottom,b.bottom)-Math.max(a.y,b.y);if(x>1&&y>1) inkOverlaps.push({a:a.text,b:b.text,groups:[a.group,b.group],intersection:{width:x,height:y}});}
    const xml = BOB_REVIEW.exportSvg(); const parsed = new DOMParser().parseFromString(xml, 'image/svg+xml');
    const imgs = [...parsed.querySelectorAll('image')].map(im => ({ embedded: im.getAttribute('href')?.startsWith('data:image/'), prefix: im.getAttribute('href')?.slice(0,40) }));
    return { textCount:nodes.length, vectorCount:parsed.querySelectorAll('rect,path,circle').length, outside, overlaps, inkBounds, inkOverlaps, textBounds:nodes.map(({client,...n})=>n), xmlValid:!parsed.querySelector('parsererror'), xmlError:parsed.querySelector('parsererror')?.textContent, images:imgs, interactionAttributes:parsed.querySelectorAll('[data-action],[data-drag],[tabindex],[role]').length, containsOldPing: nodes.some(n=>n.text.includes('ปิง')) };
  };
  for (const width of [1920, 2400]) {
    for (const screen of screens) {
      await page.evaluate(({width,screen}) => { BOB_REVIEW.setWidth(width); BOB_REVIEW.setScreen(screen); }, {width,screen});
      await page.evaluate(() => document.fonts.ready);
      const name = 'bob-r1-' + screen + '-' + width + 'x1080';
      const xml = await page.evaluate(() => BOB_REVIEW.exportSvg());
      const svgFile = path.join(out, name + '.svg'); fs.writeFileSync(svgFile, xml);
      const pngTemp = await shot(name, true); const pngFile = path.join(out, name + '.png'); fs.renameSync(pngTemp, pngFile);
      const bytes = fs.readFileSync(pngFile); const dimensions = [bytes.readUInt32BE(16),bytes.readUInt32BE(20)];
      const validation = await page.evaluate(audit);
      report.exports.push({ screen, width, svgFile, pngFile, dimensions, sha256:hash(xml), ...validation });
      check('Export validates ' + name, validation.xmlValid && validation.outside.length === 0 && validation.images.every(i=>i.embedded) && validation.textCount > 0 && !validation.containsOldPing && dimensions[0] === width && dimensions[1] === 1080, { dimensions, xmlValid:validation.xmlValid, outside:validation.outside, oldWording:validation.containsOldPing });
      console.log('Exported ' + name);
    }
  }
  // Standalone exports have no @font-face, intentionally tested without inherited page CSS.
  const standalone = await context.newPage();
  await standalone.goto('http://127.0.0.1:8768/exports/' + run + '/bob-r1-cart-ready-1920x1080.svg', {waitUntil:'networkidle'});
  report.standaloneFont = await standalone.evaluate(() => ({ faces:[...document.fonts].map(f=>({family:f.family,status:f.status})), fontFamily:getComputedStyle(document.querySelector('text')).fontFamily, title:document.title, width:document.querySelector('svg').getBoundingClientRect().width }));
  const fontSession = await context.newCDPSession(standalone);
  await fontSession.send('DOM.enable'); await fontSession.send('CSS.enable');
  const doc = await fontSession.send('DOM.getDocument');
  const textNode = await fontSession.send('DOM.querySelector', {nodeId:doc.root.nodeId,selector:'text'});
  report.standaloneFont.platformFonts = (await fontSession.send('CSS.getPlatformFontsForNode', {nodeId:textNode.nodeId})).fonts;
  report.findings.push({ severity:'minor', category:'export-portability', location:'index.html:exportSvg', description:'Standalone SVG has no embedded @font-face. Install the two supplied IBM Plex Sans Thai font files before import to preserve typography; no Figma import validation performed.', observed:report.standaloneFont });
  await standalone.close();
  // Contact sheets are drawn from the already captured PNG evidence, not rerendered SVG.
  const grid = await context.newPage();
  for (const width of [1920,2400]) {
    const entries = report.exports.filter(e=>e.width===width).map(e=>({name:e.screen,src:'data:image/png;base64,'+fs.readFileSync(e.pngFile).toString('base64')}));
    await grid.setContent('<html><body style="margin:0"><canvas id="grid"></canvas></body></html>');
    const png = await grid.evaluate(async ({entries,width}) => { const c=document.querySelector('canvas'), ctx=c.getContext('2d'), cols=4, tileW=480, tileH=tileW*1080/width, rowH=tileH+40; c.width=cols*tileW; c.height=Math.ceil(entries.length/cols)*rowH; ctx.fillStyle='#e9eee9';ctx.fillRect(0,0,c.width,c.height); for(let i=0;i<entries.length;i++){const im=new Image();im.src=entries[i].src;await im.decode();const x=(i%cols)*tileW,y=Math.floor(i/cols)*rowH;ctx.fillStyle='#24364b';ctx.font='18px sans-serif';ctx.fillText(entries[i].name+' / '+width+'x1080',x+10,y+27);ctx.drawImage(im,x,y+40,tileW,tileH);}return c.toDataURL('image/png').split(',')[1]; },{entries,width});
    fs.writeFileSync(path.join(ev,'overview-'+width+'.png'),Buffer.from(png,'base64'));
  }
  await grid.close();
  report.smallTargets = report.tests.filter(t=>t.name.startsWith('Small layout measurements') && t.observed.below44px.length).map(t=>({screen:t.name,targets:t.observed.below44px}));
  if(report.smallTargets.length) report.findings.push({severity:'minor',category:'small-landscape',description:'At the 667x375 review viewport some rendered controls are smaller than 44 CSS pixels. Native mouse flow still tested; this is not actual phone usability validation.',observed:report.smallTargets});
  report.inkOverlapCandidates = report.exports.filter(e=>e.inkOverlaps.length).map(e=>({screen:e.screen,width:e.width,overlaps:e.inkOverlaps}));
  report.sourceHashesAfter = hashes();
  check('Design sources stable during run', JSON.stringify(report.sourceHashesBefore) === JSON.stringify(report.sourceHashesAfter) && report.loadedIndexHash === report.sourceHashesAfter['index.html'] && report.loadedFixturesHash === report.sourceHashesAfter['fixtures.js'], { before:report.sourceHashesBefore, after:report.sourceHashesAfter, loadedIndexHash:report.loadedIndexHash, loadedFixturesHash:report.loadedFixturesHash });
  report.summary = { passed:report.tests.filter(t=>t.status==='pass').length, failed:report.tests.filter(t=>t.status==='fail').length, errors:report.tests.filter(t=>t.status==='error').length, exports:report.exports.length, textOutside:report.exports.reduce((n,e)=>n+e.outside.length,0), overlapCandidates:report.exports.reduce((n,e)=>n+e.overlaps.length,0) };
})().catch(e => { report.fatal = e.stack; console.error(e.stack); process.exitCode = 1; }).finally(async () => {
  if (browser) await browser.close();
  saveReport();
  const python = spawnSync('python', ['-c', 'import sys,json,pathlib,xml.etree.ElementTree as E,zipfile\nroot,out,ev=map(pathlib.Path,sys.argv[1:])\nr=[]\nfor f in out.glob("*.svg"):\n e=E.parse(f).getroot();r.append({"file":str(f),"root":e.tag,"width":e.get("width"),"height":e.get("height"),"valid":True})\n(ev/"svg-xml-validation.json").write_text(json.dumps(r,indent=2),encoding="utf-8")\nz=out.parent/("bob-r1-"+out.name+".zip")\nwith zipfile.ZipFile(z,"w",zipfile.ZIP_DEFLATED) as a:\n for base in [out,ev]:\n  for f in base.rglob("*"):\n   if f.is_file() and "browser-runtime" not in f.parts: a.write(f,f.relative_to(root))\n for name in ["verify-export.cjs","IBMPlexSansThai-Regular.ttf","IBMPlexSansThai-Bold.ttf"]: a.write(root/name,name)\nprint(json.dumps({"zip":str(z),"xmlValidated":len(r)}))', root, out, ev], {encoding:'utf8'});
  report.packaging = { status:python.status, stdout:python.stdout, stderr:python.stderr };
  saveReport();
  console.log(JSON.stringify({ evidence:ev, exports:out, summary:report.summary, packaging:report.packaging, fatal:report.fatal },null,2));
});

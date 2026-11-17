const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const root = path.resolve(__dirname, '../../..');
const ctx = vm.createContext({});
vm.runInContext(fs.readFileSync(path.join(root, 'server/modules/index.js'), 'utf8') + ';this.api={PARTS,ROUND_BUDGET,taskCartShelf,taskCartNeed,taskCartStar,taskCartScore};', ctx);
const A = ctx.api;
const shelf = ['sponge', 'brush', 'steam', 'cloth', 'polish', 'soak'];
let seed;
for (let n = 0; n < 100000; n++) {
  const need = A.taskCartNeed(n, 1);
  if (A.taskCartShelf(n, 1).join() === shelf.join() && [need.clean, need.gentle, need.speed, need.shine].join() === '7,3,3,3') { seed = n; break; }
}
if (seed === undefined) throw new Error('Reference shelf fixture not found');
const scores = {};
function walk(stack) {
  scores[stack.join('|')] = A.taskCartScore(seed, 1, stack);
  if (stack.length < 4) for (const id of shelf) if (!stack.includes(id)) walk([...stack, id]);
}
walk([]);
const data = { seed, difficulty: 1, budget: A.ROUND_BUDGET, shelf, parts: A.PARTS, scores, star: A.taskCartStar(seed), ceilings: [17, 9, 11, 16] };
const dataUri = (name, type) => `data:${type};base64,${fs.readFileSync(path.join(__dirname, name)).toString('base64')}`;
const assets = { world: dataUri('world.jpg', 'image/jpeg'), bubble: dataUri('bubble.png', 'image/png'), home: dataUri('home-reference.jpg', 'image/jpeg') };
fs.writeFileSync(path.join(__dirname, 'fixtures.js'), 'window.BOB_DESIGN_DATA=' + JSON.stringify(data) + ';\nwindow.BOB_DESIGN_ASSETS=' + JSON.stringify(assets) + ';\n');
for (const weight of ['Regular', 'Bold']) fs.copyFileSync(path.join(root, `unity/Assets/Resources/Fonts/IBMPlexSansThai-${weight}.ttf`), path.join(__dirname, `IBMPlexSansThai-${weight}.ttf`));
console.log(JSON.stringify({ seed, shelf, need: A.taskCartNeed(seed, 1), success: scores['sponge|brush|steam|cloth'], fixtures: Object.keys(scores).length }, null, 2));

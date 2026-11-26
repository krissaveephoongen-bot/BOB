export const PAL = {
  ink: '#041E4F',
  inkMuted: '#5A5F7A',
  frame: '#2E4484',
  surface: '#FFFDF5',
  cream: '#FEEECF',
  gold: '#F6C431',
  gold2: '#FFD166',
  accent: '#FD7713',
  secondary: '#1A6FC4',
  sky: '#3695F2',
  red: '#FB2F44',
  success: '#178A4F',
  danger: '#D81E36',
  spark: '#3DDC84',
  tool: '#F4A340',
  chem: '#4EA8DE',
  energy: '#EF476F',
  method: '#9B7EDE',
  outline: '#4A2E22',
  skin: '#FADDC1',
  skinShadow: '#E9B896',
  stain: '#8A5226',
  grease: '#8B4FC8',
  trash: '#7D8796',
  germ: '#7BC144'
};

export const CAT = {
  Sweep: { th: 'กวาด', color: PAL.tool },
  Wash: { th: 'ล้าง', color: PAL.chem },
  Scrub: { th: 'ถู', color: PAL.success },
  Spray: { th: 'สเปรย์', color: PAL.method },
  Everyone: { th: 'ทุกมือ', color: PAL.energy }
};

export const HEROES = [
  { id: 'sweep', th: 'กวาด', en: 'Sweep', color: '#F79A2E', cat: 'Sweep', ability: 'จัดทัพ', abilityNote: '+25% ความเร็วทีม 8 วิ' },
  { id: 'bubble', th: 'ฟอง', en: 'Bubble', color: '#3D8FE0', cat: 'Wash', ability: 'ฟองช่วย', abilityNote: '+25% งานล้าง' },
  { id: 'press', th: 'ถู', en: 'Scrub', color: '#2FA97A', cat: 'Scrub', ability: 'ถังกันคราบ', abilityNote: '+25% งานถู' },
  { id: 'spray', th: 'สเปรย์', en: 'Spray', color: '#8E6BD8', cat: 'Spray', ability: 'เล็งเป้า', abilityNote: '+25% งานสเปรย์' }
];

export const CHAPTERS = [
  { n: 1, th: 'ห้องเช่า', lv: 1, band: 1, note: '' },
  { n: 2, th: 'รับงานเพื่อนบ้าน', lv: 2, band: 1, note: '' },
  { n: 3, th: 'ร้านค้าในซอย', lv: 3, band: 2, note: '' },
  { n: 4, th: 'บ้านเก่ามีเรื่องเล่า', lv: 4, band: 2, note: 'คราบสองชั้น' },
  { n: 5, th: 'งานใหญ่ของย่าน', lv: 5, band: 3, note: '90 วิ · สะพาน 45 วิ · จุดสุ่ม' },
  { n: 6, th: 'ฟื้นตลาดเก่า', lv: 6, band: 3, note: '+200 โบนัส' }
];

export const DISTRICT = 'ตลาดเก่าท่าศาลา';
export const RIVAL_TEAM = 'ทีมสะอาดเอี่ยม';

// Spike plays the three gesture archetypes that carry the game: drag-path, circular scrub, sort.
export const TASKS = {
  T01: { id: 'T01', th: 'กวาดตามทาง', cat: 'Sweep', base: 20, goal: [1, 2, 3], goalUnit: 'เส้นทาง' },
  T02: { id: 'T02', th: 'ถูคราบวน', cat: 'Scrub', base: 24, goal: [2400, 3000, 3600], goalUnit: 'u' },
  T05: { id: 'T05', th: 'แยกขยะ', cat: 'Everyone', base: 26, goal: [6, 8, 10], goalUnit: 'ชิ้น' }
};

export const TASK_POOL = ['T01', 'T02', 'T05'];

export const BINS = [
  { key: 'wet', th: 'ขยะเปียก', color: PAL.germ, items: ['เปลือกกล้วย', 'เศษข้าว', 'ใบชา', 'ก้างปลา', 'เปลือกไข่'] },
  { key: 'recycle', th: 'รีไซเคิล', color: PAL.chem, items: ['ขวดพลาสติก', 'กระป๋อง', 'กระดาษ', 'กล่องนม', 'แก้ว'] },
  { key: 'general', th: 'ขยะทั่วไป', color: PAL.trash, items: ['ถุงพลาสติก', 'โฟม', 'ซองขนม', 'ฟองน้ำ', 'ผ้าขี้ริ้ว'] },
  { key: 'danger', th: 'ของอันตราย', color: PAL.energy, items: ['ถ่านไฟฉาย', 'กระป๋องสเปรย์', 'หลอดไฟ', 'ขวดน้ำยา'] }
];

export const STR = {
  title: 'สมรภูมิปัดกวาด',
  subtitle: 'Battle of the Brooms — เว็บสไปค์ M1–M3',
  play: 'เล่นเลย',
  guest: 'เล่นแบบ แขก',
  back: 'กลับเมนู',
  next: 'บทต่อไป',
  retry: 'ลองอีกครั้ง',
  work: 'ทำงาน',
  sabotage: 'ปราบสะพาน · ส่งคราบ',
  round: 'รอบ',
  time: 'เวลา',
  winChapter: 'ผ่านบท!',
  notYet: 'ยังไม่ผ่าน',
  tasksLeft: 'งานที่เหลือ',
  stainSpread: 'คราบลาม',
  bridgeDown: 'สะพานลงแล้ว · ปราบกันได้',
  bridgeShake: 'สะพานสั่น…',
  bestOf3: 'ชนะ 2 ใน 3 รอบ',
  stars: 'ดาว'
};

export const REF = { w: 1920, h: 1080 };
export const PX_PER_UNIT = 60; // island is authored in world units, 12 u across
export const WALK_SPEED_U = 3.2;
export const MIN_TAP_U = 48; // >=144 design units at 3u per css px

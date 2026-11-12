#!/usr/bin/env python3
"""Character design sheets (placeholder) for the 4 heroes, laid out like the concept "กวาด (Sweeper)" sheet.
Writes docs/sheets/sheet_<id>.svg; tools/render_sheets.js turns them into PNG.
    python3 tools/make_sheets.py && node tools/render_sheets.js
"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import make_art as A

OUT = os.path.join(A.ROOT, "docs", "sheets")
INFO = {
    "sweep":  ("กวาด", "Sweeper", "หัวหน้าทีม · จัดทัพ: สลับ 2 ชั้นที่ติดกันบนรถเข็น", "Clean the world, one swipe at a time!", "swipe"),
    "bubble": ("ฟอง", "Foam", "ซัพพอร์ต · โล่ฟอง: กันปฏิกิริยาเสียที่แรงที่สุด 1 คู่", "Bubbles make everything better!", "bubble_dome"),
    "press":  ("ถู", "Scrub", "ผู้คุมเกม · รีดเวลา: หยุดนาฬิกา 8 วินาที", "Slow and steady scrubs the floor!", "time_freeze"),
    "spray":  ("สเปรย์", "Spray", "สายวิเคราะห์ · เล็งเป้า: ดูคะแนนกองนี้ล่วงหน้า", "One spray, right on target.", "spray_mist"),
}
EXPR = ["happy", "wink", "angry", "surprised", "cry", "effort", "dizzy", "joy"]
FONT = 'font-family="Mitr, Kanit, Tahoma, sans-serif"'

def panel(x, y, w, h, title):
    return (f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="18" fill="#F4F8FE" stroke="#C9D8EE" stroke-width="2"/>'
            f'<rect x="{x+14}" y="{y-14}" width="{len(title)*11+40}" height="30" rx="15" fill="{A.NAVY}"/>'
            f'<text x="{x+34}" y="{y+7}" {FONT} font-weight="700" font-size="17" fill="#fff">{title}</text>')

def put(body, x, y, s=1.0):
    return f'<g transform="translate({x},{y}) scale({s})">{body}</g>'

def label(x, y, t, size=15):
    return f'<text x="{x}" y="{y}" text-anchor="middle" {FONT} font-size="{size}" fill="{A.NAVY}">{t}</text>'


def sheet_svg(cid):
    th, en, role, tag, fx = INFO[cid]
    c = A.CHARS[cid]
    W, H = 1600, 1000
    o = f'<rect width="{W}" height="{H}" fill="#DDEBFA"/>'
    # hero block
    o += f'<rect x="20" y="20" width="430" height="440" rx="24" fill="#fff" stroke="#C9D8EE" stroke-width="2"/>'
    o += f'<circle cx="235" cy="240" r="180" fill="{c["col"]}" fill-opacity=".18"/>' + put(A.character(cid, expr="joy", arms_up=True), 55, 80, 2.8)
    # title
    o += f'<rect x="470" y="20" width="560" height="74" rx="37" fill="{A.NAVY}"/><text x="500" y="70" {FONT} font-weight="800" font-size="40" fill="#fff">{th}  ({en})</text>'
    o += f'<text x="480" y="122" {FONT} font-style="italic" font-weight="700" font-size="22" fill="{A.NAVY}">{tag}</text>'
    o += f'<text x="480" y="152" {FONT} font-size="18" fill="#4A5A80">{role}</text>'
    # turnaround
    o += panel(470, 190, 560, 270, "Turnaround (หน้า / หลัง)")
    o += put(A.character(cid), 500, 210, 1.8) + label(615, 448, "Front")
    o += put(A.character(cid, back=True), 760, 210, 1.8) + label(875, 448, "Back")
    o += label(990, 330, "Side: ให้ศิลปินวาด", 13)
    # expressions
    o += panel(1050, 40, 530, 420, "Expressions")
    for i, e in enumerate(EXPR):
        x, y = 1066 + (i % 4) * 128, 64 + (i // 4) * 196
        o += put(f'<svg width="120" height="150" viewBox="4 0 120 150" overflow="hidden"><rect x="4" width="120" height="150" rx="14" fill="#fff"/><g transform="translate(-77,-26) scale(2.2)">{A.character(cid, expr=e)}</g></svg>', x, y + 6, 1.0) + label(x + 60, y + 176, e)
    # poses
    o += panel(20, 500, 1010, 300, "Key Poses (ใช้ในเกมแบบรถเข็น)")
    poses = [("Idle", dict()), ("Walk", dict(legL=7, armL=4, armR=-4, prop_ang=-25, bob=-2)), ("Carry", dict(carry=True, expr="effort")),
             ("Work", dict(armR=4, prop_ang=15, expr="effort")), ("Skill", dict(arms_up=True, expr="angry")), ("Hit", dict(expr="dizzy", squash=0.92)), ("Victory", dict(arms_up=True, expr="joy"))]
    for i, (n, kw) in enumerate(poses):
        x = 34 + i * 142
        o += put(A.character(cid, **kw), x, 530, 1.1) + label(x + 70, 690, n)
    o += label(525, 780, "แบบ 2D ชั่วคราวจากโค้ด: ใช้กำหนดท่าและอ่านง่ายบนจอ ไม่ใช่ภาพ final", 14)
    # equipment + skill
    o += panel(1050, 500, 530, 300, "Equipment &amp; Skill")
    o += put(A.held_prop(c["prop"], 0, 0, 0), 1150, 680, 2.6) + label(1150, 770, "อุปกรณ์")
    o += put(A.round_button(c["col"], A.SKILL_ICON[cid](), 40), 1260, 560, 1.4) + label(1327, 770, "ไอคอนสกิล")
    import json
    man = json.load(open(os.path.join(A.ART, "bob-art-manifest.json")))
    n = next(m["frames"] for m in man["sprites"] if m["path"] == f"Sprites/FX/fx_{fx}")
    k = min(n - 1, 3)
    o += put(f'<svg width="150" height="150" viewBox="{k*128} 0 128 128" overflow="hidden"><image href="../../source/svg/Sprites/FX/fx_{fx}.svg" x="0" y="0" width="{n*128}" height="128"/></svg>', 1420, 580, 1.0) + label(1494, 770, "FX")
    # palette
    o += panel(20, 840, 1560, 140, "Color Palette")
    sw = [(c["col"], "เสื้อ"), (c["hair"], "ผม"), (A.SKIN, "ผิว"), (A.APRON, "ผ้ากันเปื้อน"), (A.PANTS, "กางเกง"), (A.BOOT, "รองเท้า"), (A.INK, "เส้นขอบ")]
    extra = {"sweep": [(A.BAND, "ผ้าคาดหัว")], "bubble": [("#FFD84D", "ฟองน้ำ")], "press": [("#C9CED6", "ถังโลหะ")], "spray": [("#9ED8F5", "เลนส์แว่น"), ("#F79BB0", "ขวดสเปรย์")]}[cid]
    for i, (col, n) in enumerate(sw + extra):
        x = 50 + i * 165
        o += f'<rect x="{x}" y="872" width="56" height="56" rx="12" fill="{col}" stroke="{A.INK}" stroke-width="3"/><text x="{x+68}" y="896" {FONT} font-size="16" fill="{A.NAVY}">{n}</text><text x="{x+68}" y="920" font-family="monospace" font-size="15" fill="#4A5A80">{col}</text>'
    return W, H, o

if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    for cid in A.CHARS:
        W, H, body = sheet_svg(cid)
        with open(os.path.join(OUT, f"sheet_{cid}.svg"), "w") as fh:
            fh.write(f'<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="{W}" height="{H}" viewBox="0 0 {W} {H}">{body}</svg>')
    print("sheets written to", OUT)

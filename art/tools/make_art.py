#!/usr/bin/env python3
"""Battle of the Brooms: placeholder art generator.

Writes SVG sources (one SVG per sprite or per horizontal sprite sheet) into
source/svg and a manifest that render_png.js and the Unity importer read.
Everything is drawn from code so it can be regenerated after a colour or size change.

    python3 tools/make_art.py && node tools/render_png.js

Frame grid: every sheet is one row of equal frames, drawn on a 128-unit grid and
rendered at SCALE (2x -> 256 px frames).
"""
import json, math, os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART = os.path.join(ROOT, "unity", "Assets", "Art")
SRC = os.path.join(ROOT, "source", "svg")  # kept outside Assets so Unity does not import the SVGs
SCALE = 2

INK = "#2B2D42"; SURFACE = "#FFFDF5"; SUN = "#FFD166"; TEAL = "#1FA6A0"; ROSE = "#EF476F"
SKIN = "#F9D5B4"; MUTED = "#6B6E86"; FLOOR = "#F3E3B5"; FLOOR2 = "#EAD7A0"
FAM = {"tool": "#F4A340", "chem": "#4EA8DE", "energy": "#EF476F", "method": "#9B7EDE"}

# Looks follow the official concept (docs/concept/BOB_Asset_Sheet.png):
# sweep = กวาด (red headband, orange, broom), bubble = ฟอง (space buns, blue, sponge),
# spray = สเปรย์ (black bob + goggles, purple, pink spray bottle), press = ถู (bucket hat, green, mop).
CHARS = {
    "sweep":  {"col": "#F79A2E", "hair": "#4B2E1F", "prop": "broom"},
    "bubble": {"col": "#3D8FE0", "hair": "#8A5530", "prop": "sponge"},
    "spray":  {"col": "#8E6BD8", "hair": "#2A2228", "prop": "bottle"},
    "press":  {"col": "#2FA97A", "hair": "#4B2E1F", "prop": "mop"},
}
PANTS = "#2E4484"; BOOT = "#5A3825"; APRON = "#FEEECF"; BAND = "#FB2F44"
ENEMIES = {
    "stain":  {"col": "#7A5C3E", "hi": "#9C7B58"},
    "dust":   {"col": "#A9A39A", "hi": "#CFCAC2"},
    "grease": {"col": "#C9A227", "hi": "#F2D46B"},
    "rust":   {"col": "#B5532A", "hi": "#D9774A"},
    "scale":  {"col": "#8FB8CC", "hi": "#D3EEF7"},
}
PARTS = {  # id -> family (matches prototype PARTS)
    "cloth": "tool", "sponge": "tool", "brush": "tool", "broom": "tool",
    "soap": "chem", "vinegar": "chem", "soda": "chem", "polish": "chem",
    "hot": "energy", "steam": "energy", "scrub": "energy", "air": "energy",
    "soak": "method", "spray": "method", "circle": "method",
}

manifest = {"scale": SCALE, "sprites": []}

def f(v): return f"{v:.2f}".rstrip("0").rstrip(".")
S = f'stroke="{INK}" stroke-width="3.5" stroke-linejoin="round" stroke-linecap="round"'

def write_svg(rel, w, h, body):
    path = os.path.join(SRC, rel + ".svg")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w") as fh:
        fh.write(f'<svg xmlns="http://www.w3.org/2000/svg" width="{w*SCALE}" height="{h*SCALE}" viewBox="0 0 {w} {h}">{body}</svg>')

def sprite(rel, w, h, body, **meta):
    """One image (no animation)."""
    write_svg(rel, w, h, body)
    manifest["sprites"].append({"path": rel, "w": w * SCALE, "h": h * SCALE, "frames": 1, **meta})

def sheet(rel, fw, fh, frames, fps=10, loop=True, pivot=(0.5, 0.5), **meta):
    """frames: list of SVG bodies, each drawn in a fw x fh box. Laid out left to right."""
    body = "".join(f'<g transform="translate({i*fw},0)">{fr}</g>' for i, fr in enumerate(frames))
    write_svg(rel, fw * len(frames), fh, body)
    manifest["sprites"].append({"path": rel, "w": fw * SCALE, "h": fh * SCALE, "frames": len(frames),
                                "fps": fps, "loop": loop, "pivot": list(pivot), **meta})

def blob(cx, cy, rfun, n=48):
    pts = []
    for i in range(n):
        a = i / n * 2 * math.pi
        r = rfun(a)
        pts.append((cx + math.cos(a) * r[0], cy + math.sin(a) * r[1]))
    # smooth closed path through points (Catmull-Rom -> cubic Bezier)
    d = f"M{f(pts[0][0])},{f(pts[0][1])}"
    for i in range(n):
        p0, p1, p2, p3 = pts[i - 1], pts[i], pts[(i + 1) % n], pts[(i + 2) % n]
        c1 = (p1[0] + (p2[0] - p0[0]) / 6, p1[1] + (p2[1] - p0[1]) / 6)
        c2 = (p2[0] - (p3[0] - p1[0]) / 6, p2[1] - (p3[1] - p1[1]) / 6)
        d += f"C{f(c1[0])},{f(c1[1])} {f(c2[0])},{f(c2[1])} {f(p2[0])},{f(p2[1])}"
    return d + "Z"

def star(cx, cy, r1, r2, n=5, rot=-90):
    pts = []
    for i in range(n * 2):
        r = r1 if i % 2 == 0 else r2
        a = math.radians(rot + i * 180 / n)
        pts.append(f"{f(cx + math.cos(a) * r)},{f(cy + math.sin(a) * r)}")
    return "M" + " L".join(pts) + "Z"

def sparkle(cx, cy, r, col="#fff", sw=2.5):
    return f'<path d="{star(cx, cy, r, r*0.28, 4, -90)}" fill="{col}" stroke="{INK}" stroke-width="{sw}" stroke-linejoin="round"/>'

# ---------------------------------------------------------------- characters
def held_prop(kind, x, y, ang):
    g = f'<g transform="translate({f(x)},{f(y)}) rotate({f(ang)})">'
    if kind == "broom":
        g += f'<rect x="-2.5" y="-46" width="5" height="44" rx="2" fill="#B07A45" {S}/>'
        g += f'<path d="M-9,-6 L9,-6 L14,12 L-14,12 Z" fill="{SUN}" {S}/>'
        g += f'<path d="M-7,0 L-9,10 M0,0 L0,11 M7,0 L9,10" stroke="{INK}" stroke-width="2" fill="none"/>'
    elif kind == "sponge":
        g += f'<rect x="-13" y="-16" width="26" height="18" rx="6" fill="#FFD84D" {S}/>'
        g += f'<rect x="-13" y="-4" width="26" height="6" rx="3" fill="#7BC96F" stroke="{INK}" stroke-width="2.5"/>'
        g += f'<circle cx="-6" cy="-10" r="1.8" fill="#E0B52E"/><circle cx="3" cy="-12" r="1.5" fill="#E0B52E"/><circle cx="7" cy="-7" r="1.8" fill="#E0B52E"/>'
        g += f'<circle cx="14" cy="-24" r="4" fill="#DFF4FF" fill-opacity=".7" stroke="#4EA8DE" stroke-width="1.6"/><circle cx="-10" cy="-26" r="2.6" fill="#DFF4FF" fill-opacity=".7" stroke="#4EA8DE" stroke-width="1.4"/>'
    elif kind == "bottle":
        g += f'<rect x="-8" y="-20" width="16" height="22" rx="4" fill="#F79BB0" {S}/>'
        g += f'<path d="M-5,-20 L-5,-28 L8,-28 L12,-24 L8,-24 L5,-20 Z" fill="#fff" {S}/>'
        g += f'<path d="M-5,-20 L-9,-14" stroke="{INK}" stroke-width="2.5" stroke-linecap="round"/>'
        g += f'<rect x="-5" y="-12" width="10" height="7" rx="2" fill="#fff" fill-opacity=".8" stroke="{INK}" stroke-width="2"/>'
    elif kind == "mop":
        g += f'<rect x="-2.5" y="-46" width="5" height="40" rx="2" fill="#B07A45" {S}/>'
        g += f'<rect x="-8" y="-8" width="16" height="6" rx="2" fill="#9AA3AE" {S}/>'
        g += f'<path d="M-12,14 Q-10,2 -6,-2 L6,-2 Q10,2 12,14 Q6,10 4,15 Q0,9 -4,15 Q-6,10 -12,14 Z" fill="#fff" {S}/>'
    return g + "</g>"

def hair_back(kind, c, hy):
    """Hair drawn behind the head circle."""
    if kind == "bubble":  # two space buns
        return (f'<circle cx="45" cy="{f(hy-20)}" r="9" fill="{c["hair"]}" {S}/>'
                f'<circle cx="83" cy="{f(hy-20)}" r="9" fill="{c["hair"]}" {S}/>')
    if kind == "spray":   # bob haircut reaching the jaw
        return f'<path d="M38,{f(hy+18)} Q34,{f(hy-28)} 64,{f(hy-27)} Q94,{f(hy-28)} 90,{f(hy+18)} Z" fill="{c["hair"]}" {S}/>'
    return ""

def hair(kind, c, hy):
    cap = f'<path d="M41,{f(hy)} Q42,{f(hy-24)} 64,{f(hy-24)} Q86,{f(hy-24)} 87,{f(hy)} Z" fill="{c["hair"]}" {S}/>'
    if kind == "sweep":   # messy short hair + red headband knotted on the right with two tails
        o = f'<path d="M41,{f(hy-1)} L42,{f(hy-14)} L48,{f(hy-20)} L52,{f(hy-30)} L60,{f(hy-24)} L66,{f(hy-32)} L72,{f(hy-24)} L80,{f(hy-28)} L82,{f(hy-18)} L87,{f(hy-12)} L87,{f(hy-1)} Z" fill="{c["hair"]}" {S}/>'
        o += f'<path d="M84,{f(hy-6)} L100,{f(hy-14)} L98,{f(hy-4)} Z M84,{f(hy-2)} L99,{f(hy+6)} L92,{f(hy+9)} Z" fill="{BAND}" stroke="{INK}" stroke-width="2.5" stroke-linejoin="round"/>'
        o += f'<rect x="40" y="{f(hy-7)}" width="48" height="8" rx="3.5" fill="{BAND}" stroke="{INK}" stroke-width="2.5"/>'
        o += f'<circle cx="87" cy="{f(hy-3)}" r="4" fill="{BAND}" stroke="{INK}" stroke-width="2.5"/>'
        return o
    if kind == "bubble":  # cap with side-swept bangs
        return cap + f'<path d="M44,{f(hy-6)} Q56,{f(hy-20)} 80,{f(hy-14)} Q66,{f(hy-12)} 58,{f(hy-4)} Z" fill="{c["hair"]}" stroke="{INK}" stroke-width="2" stroke-linejoin="round"/>'
    if kind == "spray":   # straight bangs + goggles (blue lenses, purple strap) on top of the head
        o = f'<path d="M41,{f(hy+1)} Q42,{f(hy-25)} 64,{f(hy-25)} Q86,{f(hy-25)} 87,{f(hy+1)} Z" fill="{c["hair"]}" {S}/>'
        o += f'<rect x="38" y="{f(hy-17)}" width="52" height="6" rx="3" fill="{c["col"]}" stroke="{INK}" stroke-width="2.5"/>'
        o += f'<circle cx="54" cy="{f(hy-16)}" r="8" fill="#9ED8F5" stroke="{INK}" stroke-width="3"/><circle cx="74" cy="{f(hy-16)}" r="8" fill="#9ED8F5" stroke="{INK}" stroke-width="3"/>'
        o += f'<path d="M50,{f(hy-19)} L53,{f(hy-22)} M70,{f(hy-19)} L73,{f(hy-22)}" stroke="#fff" stroke-width="2.2" stroke-linecap="round"/>'
        return o
    if kind == "press":   # silver bucket worn as a helmet, hair peeking out, handle at the side
        o = f'<path d="M42,{f(hy-1)} Q42,{f(hy-10)} 50,{f(hy-10)} L78,{f(hy-10)} Q86,{f(hy-10)} 86,{f(hy-1)} Z" fill="{c["hair"]}" {S}/>'
        o += f'<path d="M87,{f(hy-6)} Q101,{f(hy+6)} 90,{f(hy+16)}" fill="none" stroke="{INK}" stroke-width="5" stroke-linecap="round"/><path d="M87,{f(hy-6)} Q101,{f(hy+6)} 90,{f(hy+16)}" fill="none" stroke="#C9CED6" stroke-width="2"/>'
        o += f'<path d="M45,{f(hy-6)} L49,{f(hy-36)} L79,{f(hy-36)} L83,{f(hy-6)} Z" fill="#C9CED6" {S}/>'
        o += f'<path d="M53,{f(hy-32)} L51,{f(hy-10)}" stroke="#fff" stroke-width="3" stroke-linecap="round" opacity=".8"/>'
        o += f'<rect x="41" y="{f(hy-10)}" width="46" height="7" rx="3" fill="#AEB5BF" stroke="{INK}" stroke-width="2.5"/>'
        return o
    return cap

def face(hy, blink=False, mouth="smile", expr=None):
    o = ""
    L, R, ey = 55, 73, hy + 9
    eye = lambda x: (f'<ellipse cx="{x}" cy="{f(ey)}" rx="3.6" ry="4.4" fill="{INK}"/><circle cx="{x+1.2}" cy="{f(ey-1.6)}" r="1.3" fill="#fff"/>')
    arc_up = lambda x: f'<path d="M{x-4},{f(ey+1)} Q{x},{f(ey-5)} {x+4},{f(ey+1)}" fill="none" stroke="{INK}" stroke-width="3" stroke-linecap="round"/>'
    cheeks = f'<ellipse cx="48" cy="{f(hy+16)}" rx="4" ry="2.5" fill="{ROSE}" fill-opacity=".35"/><ellipse cx="80" cy="{f(hy+16)}" rx="4" ry="2.5" fill="{ROSE}" fill-opacity=".35"/>'
    m_open = f'<path d="M58,{f(hy+15)} Q64,{f(hy+26)} 70,{f(hy+15)} Z" fill="{ROSE}" stroke="{INK}" stroke-width="2.4" stroke-linejoin="round"/>'
    if expr is None:
        if blink:
            o += f'<path d="M51,{f(ey)} L59,{f(ey)} M69,{f(ey)} L77,{f(ey)}" stroke="{INK}" stroke-width="3" stroke-linecap="round"/>'
        else:
            o += eye(L) + eye(R)
        o += cheeks
        if mouth == "smile":
            o += f'<path d="M59,{f(hy+16)} Q64,{f(hy+21)} 69,{f(hy+16)}" fill="none" stroke="{INK}" stroke-width="2.6" stroke-linecap="round"/>'
        elif mouth == "open":
            o += m_open
        elif mouth == "o":
            o += f'<ellipse cx="64" cy="{f(hy+18)}" rx="3.5" ry="4" fill="{INK}"/>'
        return o
    o += cheeks
    if expr == "happy":
        o += eye(L) + eye(R) + m_open
    elif expr == "wink":
        o += eye(L) + arc_up(R) + m_open
    elif expr == "angry":
        o += eye(L) + eye(R)
        o += f'<path d="M49,{f(ey-9)} L59,{f(ey-5)} M79,{f(ey-9)} L69,{f(ey-5)}" stroke="{INK}" stroke-width="3.5" stroke-linecap="round"/>'
        o += f'<path d="M56,{f(hy+15)} L72,{f(hy+15)} Q70,{f(hy+26)} 64,{f(hy+26)} Q58,{f(hy+26)} 56,{f(hy+15)} Z" fill="{ROSE}" stroke="{INK}" stroke-width="2.4" stroke-linejoin="round"/><path d="M57,{f(hy+16.5)} L71,{f(hy+16.5)}" stroke="#fff" stroke-width="2.5"/>'
    elif expr == "surprised":
        o += f'<ellipse cx="{L}" cy="{f(ey)}" rx="4.6" ry="5.6" fill="{INK}"/><ellipse cx="{R}" cy="{f(ey)}" rx="4.6" ry="5.6" fill="{INK}"/><circle cx="{L+1.5}" cy="{f(ey-2)}" r="1.7" fill="#fff"/><circle cx="{R+1.5}" cy="{f(ey-2)}" r="1.7" fill="#fff"/>'
        o += f'<ellipse cx="64" cy="{f(hy+18)}" rx="3.2" ry="4" fill="{INK}"/>'
        o += f'<path d="M98,{f(hy-22)} L96,{f(hy-8)}" stroke="{ROSE}" stroke-width="5" stroke-linecap="round"/><circle cx="95.5" cy="{f(hy-2)}" r="2.6" fill="{ROSE}"/>'
    elif expr == "cry":
        o += f'<path d="M51,{f(ey-1)} Q55,{f(ey+3)} 59,{f(ey-1)} M69,{f(ey-1)} Q73,{f(ey+3)} 77,{f(ey-1)}" fill="none" stroke="{INK}" stroke-width="3" stroke-linecap="round"/>'
        o += f'<path d="M50,{f(ey+3)} q-3,6 0,8 q3,-2 0,-8 Z M78,{f(ey+3)} q-3,6 0,8 q3,-2 0,-8 Z" fill="#7CC8F5" stroke="{INK}" stroke-width="1.5"/>'
        o += f'<path d="M59,{f(hy+20)} Q64,{f(hy+15)} 69,{f(hy+20)}" fill="none" stroke="{INK}" stroke-width="2.6" stroke-linecap="round"/>'
    elif expr == "effort":
        o += f'<path d="M51,{f(ey-3)} L58,{f(ey)} L51,{f(ey+3)} M77,{f(ey-3)} L70,{f(ey)} L77,{f(ey+3)}" fill="none" stroke="{INK}" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/>'
        o += f'<rect x="57" y="{f(hy+14)}" width="14" height="7" rx="2" fill="#fff" stroke="{INK}" stroke-width="2.2"/><path d="M57,{f(hy+17.5)} L71,{f(hy+17.5)} M61.5,{f(hy+14)} L61.5,{f(hy+21)} M66.5,{f(hy+14)} L66.5,{f(hy+21)}" stroke="{INK}" stroke-width="1.4"/>'
        o += f'<path d="M86,{f(hy-6)} q3,5 0,7 q-3,-2 0,-7 Z" fill="#7CC8F5" stroke="{INK}" stroke-width="1.5"/>'
    elif expr == "dizzy":
        for x in (L, R):
            o += f'<path d="M{x},{f(ey)} m-4.5,0 a4.5,4.5 0 1,1 4.5,4.5 a2.5,2.5 0 1,1 -2.5,-2.5" fill="none" stroke="{INK}" stroke-width="2.2"/>'
        o += f'<path d="M57,{f(hy+18)} q3.5,-3 7,0 q3.5,3 7,0" fill="none" stroke="{INK}" stroke-width="2.6" stroke-linecap="round"/>'
    elif expr == "joy":
        o += arc_up(L) + arc_up(R) + m_open
        o += sparkle(96, hy - 16, 6, SUN, 2) + sparkle(34, hy - 20, 4, SUN, 1.5)
    return o

def back_hair(kind, c, hy):
    """Extra hair/accessory detail for the rear view (head already filled with hair colour)."""
    if kind == "sweep":
        return (f'<rect x="40" y="{f(hy-7)}" width="48" height="8" rx="3.5" fill="{BAND}" stroke="{INK}" stroke-width="2.5"/>'
                f'<path d="M58,{f(hy-3)} L50,{f(hy+18)} L58,{f(hy+16)} Z M66,{f(hy-3)} L74,{f(hy+18)} L66,{f(hy+16)} Z" fill="{BAND}" stroke="{INK}" stroke-width="2.5" stroke-linejoin="round"/>'
                f'<circle cx="64" cy="{f(hy-3)}" r="4.5" fill="{BAND}" stroke="{INK}" stroke-width="2.5"/>')
    if kind == "bubble":
        return (f'<circle cx="45" cy="{f(hy-20)}" r="9" fill="{c["hair"]}" {S}/><circle cx="83" cy="{f(hy-20)}" r="9" fill="{c["hair"]}" {S}/>')
    if kind == "spray":
        return (f'<path d="M38,{f(hy+18)} Q34,{f(hy-28)} 64,{f(hy-27)} Q94,{f(hy-28)} 90,{f(hy+18)} Z" fill="{c["hair"]}" {S}/>'
                f'<rect x="38" y="{f(hy-17)}" width="52" height="6" rx="3" fill="{c["col"]}" stroke="{INK}" stroke-width="2.5"/>')
    if kind == "press":
        return (f'<path d="M45,{f(hy-6)} L49,{f(hy-36)} L79,{f(hy-36)} L83,{f(hy-6)} Z" fill="#C9CED6" {S}/>'
                f'<rect x="41" y="{f(hy-10)}" width="46" height="7" rx="3" fill="#AEB5BF" stroke="{INK}" stroke-width="2.5"/>')
    return ""

def character(cid, bob=0.0, legL=0.0, legR=0.0, armL=0.0, armR=0.0, blink=False, mouth="smile",
              arms_up=False, squash=1.0, prop_ang=-15, expr=None, back=False, carry=False):
    """expr: None|happy|wink|angry|surprised|cry|effort|dizzy|joy. back: rear view. carry: hands up over the head
    (the carried part is drawn in-game at Socket_Carry), tool slung on the back so it never covers the face."""
    c = CHARS[cid]
    by = bob
    o = f'<ellipse cx="64" cy="116" rx="{f(24/squash)}" ry="6" fill="#000" fill-opacity=".18"/>'
    o += f'<g transform="translate(64,116) scale({f(1/squash)},{f(squash)}) translate(-64,-116)">'
    # legs
    for lx, lift in ((49, legL), (67, legR)):
        o += f'<rect x="{lx}" y="{f(94-lift)}" width="12" height="{f(17)}" rx="4" fill="{PANTS}" stroke="{INK}" stroke-width="2.5"/>'
        o += f'<path d="M{lx-1},{f(115-lift)} L{lx-1},{f(108-lift)} Q{lx+6},{f(105-lift)} {lx+13},{f(108-lift)} L{lx+14},{f(115-lift)} Z" fill="{BOOT}" stroke="{INK}" stroke-width="2.5" stroke-linejoin="round"/>'
    # back arm (left), body, apron
    ly = 76 + by + armL
    if carry:
        o += held_prop(c["prop"], 40, 92 + by, 35)
    if carry:
        o += f'<path d="M44,{f(72+by)} L46,{f(40+by)}" stroke="{INK}" stroke-width="11" stroke-linecap="round"/><path d="M44,{f(72+by)} L46,{f(40+by)}" stroke="{c["col"]}" stroke-width="6" stroke-linecap="round"/>'
    elif arms_up:
        o += f'<path d="M44,{f(74+by)} L30,{f(52+by)}" stroke="{INK}" stroke-width="11" stroke-linecap="round"/><path d="M44,{f(74+by)} L30,{f(52+by)}" stroke="{c["col"]}" stroke-width="6" stroke-linecap="round"/>'
        o += f'<circle cx="29" cy="{f(49+by)}" r="6" fill="{SKIN}" {S}/>'
    else:
        o += f'<circle cx="38" cy="{f(ly+12)}" r="6" fill="{SKIN}" {S}/>'
        o += f'<path d="M44,{f(70+by)} L39,{f(ly+8)}" stroke="{INK}" stroke-width="11" stroke-linecap="round"/><path d="M44,{f(70+by)} L39,{f(ly+8)}" stroke="{c["col"]}" stroke-width="6" stroke-linecap="round"/>'
    o += f'<rect x="40" y="{f(64+by)}" width="48" height="36" rx="13" fill="{c["col"]}" {S}/>'
    if back:
        o += f'<path d="M50,{f(70+by)} L78,{f(92+by)} M78,{f(70+by)} L50,{f(92+by)}" stroke="{APRON}" stroke-width="3"/><path d="M58,{f(92+by)} q6,6 12,0" fill="none" stroke="{APRON}" stroke-width="3"/>'
    else:
        o += f'<path d="M52,{f(70+by)} L76,{f(70+by)} L78,{f(98+by)} L50,{f(98+by)} Z" fill="{APRON}" stroke="{INK}" stroke-width="2.5" stroke-linejoin="round"/>'
        o += f'<rect x="58" y="{f(80+by)}" width="12" height="8" rx="2" fill="none" stroke="{INK}" stroke-width="2"/>'
    # head
    hy = 46 + by
    o += hair_back(cid, c, hy)
    o += f'<circle cx="64" cy="{f(hy)}" r="24" fill="{SKIN}" {S}/>'
    o += hair(cid, c, hy)
    if back:
        o += f'<circle cx="64" cy="{f(hy)}" r="24" fill="{c["hair"]}" {S}/>'
        o += back_hair(cid, c, hy)
    else:
        o += face(hy, blink, mouth, expr)
    # front arm (right) holding the prop
    ry_ = 76 + by + armR
    if carry:
        o += f'<path d="M84,{f(72+by)} L82,{f(40+by)}" stroke="{INK}" stroke-width="11" stroke-linecap="round"/><path d="M84,{f(72+by)} L82,{f(40+by)}" stroke="{c["col"]}" stroke-width="6" stroke-linecap="round"/>'
        o += f'<circle cx="46" cy="{f(36+by)}" r="6" fill="{SKIN}" {S}/><circle cx="82" cy="{f(36+by)}" r="6" fill="{SKIN}" {S}/>'
    elif arms_up:
        o += held_prop(c["prop"], 99, 50 + by, 20)
        o += f'<path d="M84,{f(74+by)} L98,{f(54+by)}" stroke="{INK}" stroke-width="11" stroke-linecap="round"/><path d="M84,{f(74+by)} L98,{f(54+by)}" stroke="{c["col"]}" stroke-width="6" stroke-linecap="round"/>'
        o += f'<circle cx="99" cy="{f(50+by)}" r="6" fill="{SKIN}" {S}/>'
    else:
        o += held_prop(c["prop"], 92, ry_ + 10, prop_ang)
        o += f'<path d="M84,{f(70+by)} L90,{f(ry_+8)}" stroke="{INK}" stroke-width="11" stroke-linecap="round"/><path d="M84,{f(70+by)} L90,{f(ry_+8)}" stroke="{c["col"]}" stroke-width="6" stroke-linecap="round"/>'
        o += f'<circle cx="91" cy="{f(ry_+12)}" r="6" fill="{SKIN}" {S}/>'
    return o + "</g>"

def make_characters():
    for cid in CHARS:
        base = f"Sprites/Characters/{cid}/{cid}"
        idle = []
        for i in range(6):
            t = i / 6 * 2 * math.pi
            idle.append(character(cid, bob=math.sin(t) * 1.6, blink=(i == 4), squash=1 + math.sin(t) * 0.02))
        sheet(base + "_idle", 128, 128, idle, fps=8, pivot=(0.5, 0.06), group="characters", owner=cid, anim="idle")
        walk = []
        for i in range(8):
            t = i / 8 * 2 * math.pi
            walk.append(character(cid, bob=-abs(math.sin(t)) * 3, legL=max(0, math.sin(t)) * 7, legR=max(0, -math.sin(t)) * 7,
                                  armL=math.sin(t) * 4, armR=-math.sin(t) * 4, prop_ang=-15 - math.sin(t) * 10))
        sheet(base + "_walk", 128, 128, walk, fps=12, pivot=(0.5, 0.06), group="characters", owner=cid, anim="walk")
        # carry: hands busy (prop raised), slower bob
        carry = []
        for i in range(6):
            t = i / 6 * 2 * math.pi
            carry.append(character(cid, bob=-abs(math.sin(t)) * 2, legL=max(0, math.sin(t)) * 5, legR=max(0, -math.sin(t)) * 5,
                                   carry=True, expr="effort" if i % 3 == 1 else None, mouth="o"))
        sheet(base + "_carry", 128, 128, carry, fps=10, pivot=(0.5, 0.06), group="characters", owner=cid, anim="carry")
        # work: scrubbing / using the tool (prop swings)
        work = [character(cid, bob=[0, 1, 2, 1][i], armR=[-6, 0, 4, 0][i], prop_ang=[-45, -15, 15, -15][i], mouth="open" if i % 2 else "smile") for i in range(4)]
        sheet(base + "_work", 128, 128, work, fps=10, pivot=(0.5, 0.06), group="characters", owner=cid, anim="work")
        cheer = [character(cid, bob=[0, -6, -10, -6, 0, 0][i], arms_up=i in (1, 2, 3), mouth="open", squash=[0.92, 1.05, 1.06, 1.03, 0.95, 1][i]) for i in range(6)]
        sheet(base + "_cheer", 128, 128, cheer, fps=10, loop=False, pivot=(0.5, 0.06), group="characters", owner=cid, anim="cheer")
        EXPRS = ["happy", "wink", "angry", "surprised", "cry", "effort", "dizzy", "joy"]
        sheet(base + "_faces", 128, 128, [f'<svg width="128" height="128" viewBox="0 0 128 128" overflow="hidden"><g transform="translate(-77,-30) scale(2.2)">{character(cid, expr=e)}</g></svg>' for e in EXPRS],
              fps=1, loop=False, pivot=(0.5, 0.5), group="characters", hero=cid, labels=EXPRS, still=True)
        sheet(base + "_back", 128, 128, [character(cid, back=True)], fps=1, loop=False, pivot=(0.5, 0.06), group="characters", hero=cid)
        sprite(f"Sprites/Characters/{cid}/{cid}_portrait", 128, 128,
               f'<circle cx="64" cy="64" r="60" fill="{CHARS[cid]["col"]}" fill-opacity=".25"/><g transform="translate(-12,4) scale(1.2)">{character(cid, mouth="open")}</g>',
               group="characters", owner=cid)

# ---------------------------------------------------------------- enemies
def enemy_shape(eid, cx, cy, ph, k=1.0):
    """Body path for each enemy type. ph = animation phase (radians), k = scale."""
    if eid == "dust":     # fluffy cloud
        rf = lambda a: (k * (34 + 5 * abs(math.sin(a * 4.5 + ph * 0.5)) + 2 * math.sin(a * 9 + ph)),) * 2
        return blob(cx, cy, lambda a: (rf(a)[0], rf(a)[0] * 0.82), 72)
    if eid == "grease":   # glossy drop, flat bottom
        return blob(cx, cy, lambda a: (k * (35 + 3 * math.sin(a * 2 + ph)), k * (31 + (6 if math.sin(a) > 0.5 else 0) * math.sin(a * 5 + ph) + 3 * math.sin(a * 3 + ph))))
    if eid == "rust":     # jagged chunk
        n = 14
        pts = []
        for i in range(n):
            a = i / n * 2 * math.pi
            r = k * (34 + (7 if i % 2 else -2) + 2 * math.sin(ph + i))
            pts.append(f"{f(cx + math.cos(a) * r)},{f(cy + math.sin(a) * r * 0.85)}")
        return "M" + " L".join(pts) + "Z"
    if eid == "scale":    # crystal (hexagon-ish)
        pts = []
        for i in range(6):
            a = math.radians(i * 60 - 90)
            r = k * (36 + 2 * math.sin(ph + i * 2))
            pts.append(f"{f(cx + math.cos(a) * r * 0.95)},{f(cy + math.sin(a) * r * 0.85)}")
        return "M" + " L".join(pts) + "Z"
    # stain: splat
    return blob(cx, cy, lambda a: (k * (33 + 6 * math.sin(a * 5 + ph * 0.6) + 3 * math.sin(a * 7 - ph)),) * 2, 64)

def enemy_extras(eid, cx, cy, ph, k=1.0):
    o = ""
    if eid == "grease":
        o += f'<path d="M{f(cx-14*k)},{f(cy-20*k)} Q{f(cx-4*k)},{f(cy-28*k)} {f(cx+8*k)},{f(cy-22*k)}" fill="none" stroke="#fff" stroke-width="4" stroke-linecap="round" stroke-opacity=".8"/>'
        dy = 4 + 3 * math.sin(ph)
        o += f'<ellipse cx="{f(cx+18*k)}" cy="{f(cy+26*k+dy)}" rx="{f(6*k)}" ry="{f((8+dy/2)*k)}" fill="{ENEMIES[eid]["col"]}" {S}/>'
    elif eid == "rust":
        for dx, dy in ((-16, -14), (14, -18), (20, 12)):
            o += f'<circle cx="{f(cx+dx*k)}" cy="{f(cy+dy*k)}" r="{f(3*k)}" fill="#7A2E12"/>'
        o += f'<path d="M{f(cx-26*k)},{f(cy+6*k)} l6,-4 l4,6 l6,-5" fill="none" stroke="#7A2E12" stroke-width="2.5"/>'
    elif eid == "scale":
        o += f'<path d="M{f(cx)},{f(cy-30*k)} L{f(cx-10*k)},{f(cy-4*k)} L{f(cx)},{f(cy+28*k)} M{f(cx)},{f(cy-30*k)} L{f(cx+12*k)},{f(cy-2*k)} L{f(cx)},{f(cy+28*k)}" fill="none" stroke="#fff" stroke-width="2.5" stroke-opacity=".7"/>'
    elif eid == "dust":
        for i, (dx, dy) in enumerate(((-38, -24), (36, -28), (-30, 26), (40, 18))):
            r = 3 + 1.5 * math.sin(ph + i)
            o += f'<circle cx="{f(cx+dx*k)}" cy="{f(cy+dy*k + math.sin(ph+i)*3)}" r="{f(r)}" fill="#CFCAC2" stroke="{INK}" stroke-width="2"/>'
    elif eid == "stain":
        for dx, dy, r in ((-40, 20, 5), (38, -22, 4), (34, 28, 3)):
            o += f'<circle cx="{f(cx+dx*k)}" cy="{f(cy+dy*k)}" r="{r}" fill="{ENEMIES[eid]["col"]}" {S}/>'
    return o

def enemy(eid, ph=0.0, squash=1.0, face="angry", flash=0.0, k=1.0, alpha=1.0, dy=0.0):
    e = ENEMIES[eid]
    cx, cy = 64, 64 + dy
    o = f'<ellipse cx="64" cy="108" rx="{f(36*k/squash)}" ry="{f(7*k)}" fill="#000" fill-opacity="{f(.15*alpha)}"/>'
    o += f'<g opacity="{f(alpha)}" transform="translate(64,104) scale({f(1/squash)},{f(squash)}) translate(-64,-104)">'
    o += enemy_extras(eid, cx, cy, ph, k) if eid in ("grease",) else ""
    o += f'<path d="{enemy_shape(eid, cx, cy, ph, k)}" fill="{e["col"]}" {S}/>'
    # inner highlight
    o += f'<path d="{enemy_shape(eid, cx-6*k, cy-8*k, ph, k*0.45)}" fill="{e["hi"]}" fill-opacity=".55"/>'
    if eid != "grease":
        o += enemy_extras(eid, cx, cy, ph, k)
    ex = 11 * k
    ey = cy - 4 * k
    if face == "angry":
        for s in (-1, 1):
            o += f'<circle cx="{f(cx+s*ex)}" cy="{f(ey)}" r="{f(8*k)}" fill="#fff" stroke="{INK}" stroke-width="3"/>'
            o += f'<circle cx="{f(cx+s*ex+2*k)}" cy="{f(ey+2*k)}" r="{f(3.6*k)}" fill="{INK}"/>'
        o += f'<path d="M{f(cx-22*k)},{f(ey-12*k)} L{f(cx-4*k)},{f(ey-6*k)} M{f(cx+22*k)},{f(ey-12*k)} L{f(cx+4*k)},{f(ey-6*k)}" stroke="{INK}" stroke-width="3.5" stroke-linecap="round"/>'
        o += f'<path d="M{f(cx-8*k)},{f(cy+14*k)} Q{f(cx)},{f(cy+8*k)} {f(cx+8*k)},{f(cy+14*k)}" fill="none" stroke="{INK}" stroke-width="3" stroke-linecap="round"/>'
        o += f'<path d="M{f(cx-4*k)},{f(cy+11.5*k)} l2,4 l2,-4" fill="#fff" stroke="{INK}" stroke-width="1.5"/>'
    elif face == "hurt":
        for s in (-1, 1):
            o += f'<path d="M{f(cx+s*ex-5*k)},{f(ey-5*k)} l{f(10*k)},{f(10*k)} M{f(cx+s*ex+5*k)},{f(ey-5*k)} l{f(-10*k)},{f(10*k)}" stroke="{INK}" stroke-width="3.5" stroke-linecap="round"/>'
        o += f'<ellipse cx="{f(cx)}" cy="{f(cy+14*k)}" rx="{f(6*k)}" ry="{f(5*k)}" fill="{INK}"/>'
    elif face == "dizzy":
        for s in (-1, 1):
            o += f'<path d="M{f(cx+s*ex)},{f(ey)} m-5,0 a5,5 0 1,1 5,5 a3,3 0 1,1 -3,-3" fill="none" stroke="{INK}" stroke-width="2.5"/>'
        o += f'<path d="M{f(cx-8*k)},{f(cy+14*k)} q4,-4 8,0 q4,4 8,0" fill="none" stroke="{INK}" stroke-width="3" stroke-linecap="round"/>'
    if flash > 0:
        o += f'<path d="{enemy_shape(eid, cx, cy, ph, k)}" fill="#fff" fill-opacity="{f(flash)}"/>'
    return o + "</g>"

def make_enemies():
    for eid in ENEMIES:
        base = f"Sprites/Enemies/{eid}/{eid}"
        idle = []
        for i in range(8):
            t = i / 8 * 2 * math.pi
            idle.append(enemy(eid, ph=t, squash=1 + 0.05 * math.sin(t), dy=-2 * math.sin(t)))
        sheet(base + "_idle", 128, 128, idle, fps=10, pivot=(0.5, 0.15), group="enemies", owner=eid, anim="idle")
        hit = [enemy(eid, ph=0, squash=s, face="hurt", flash=fl) for s, fl in ((0.8, 0.8), (1.15, 0.4), (0.95, 0.1), (1.0, 0))]
        sheet(base + "_hit", 128, 128, hit, fps=14, loop=False, pivot=(0.5, 0.15), group="enemies", owner=eid, anim="hit")
        defeat = []
        for i in range(8):
            p = i / 7
            fr = enemy(eid, ph=p * 6, squash=1 + 0.3 * math.sin(p * math.pi), face="dizzy", k=max(0.05, 1 - p ** 1.6), alpha=max(0, 1 - p ** 2), flash=0.3 * p)
            for j in range(5):
                a = j / 5 * 2 * math.pi + 0.4
                r = 20 + 40 * p
                if 0.15 < p < 1.0:
                    fr += sparkle(64 + math.cos(a) * r, 64 + math.sin(a) * r * 0.8, 8 * (1 - p) + 3, SUN)
            defeat.append(fr)
        sheet(base + "_defeat", 128, 128, defeat, fps=12, loop=False, pivot=(0.5, 0.15), group="enemies", owner=eid, anim="defeat")

# ---------------------------------------------------------------- parts (item icons)
def part_icon(pid):
    fam = PARTS[pid]
    o = f'<rect x="8" y="8" width="112" height="112" rx="26" fill="{FAM[fam]}" {S}/>'
    o += f'<rect x="16" y="16" width="96" height="96" rx="20" fill="{SURFACE}" fill-opacity=".9"/>'
    g = ""
    if pid == "cloth":
        g = f'<path d="M30,44 Q48,34 64,44 T98,44 L98,86 Q82,96 64,86 T30,86 Z" fill="#FFB4C6" {S}/><path d="M30,60 Q48,50 64,60 T98,60" fill="none" stroke="{INK}" stroke-width="2.5" stroke-dasharray="4 5"/>'
    elif pid == "sponge":
        g = f'<rect x="28" y="40" width="72" height="50" rx="10" fill="#FFE066" {S}/><rect x="28" y="40" width="72" height="16" rx="8" fill="#57CC99" {S}/>'
        g += "".join(f'<circle cx="{x}" cy="{y}" r="{r}" fill="#E6B800"/>' for x, y, r in ((42, 70, 4), (58, 78, 3), (74, 68, 5), (88, 80, 3), (50, 64, 2.5)))
    elif pid == "brush":
        g = f'<rect x="58" y="22" width="12" height="40" rx="5" fill="#B07A45" {S}/><rect x="34" y="58" width="60" height="18" rx="6" fill="{TEAL}" {S}/>'
        g += "".join(f'<path d="M{x},76 L{x},100" stroke="{INK}" stroke-width="3" stroke-linecap="round"/>' for x in range(40, 92, 8))
    elif pid == "broom":
        g = f'<rect x="60" y="16" width="8" height="54" rx="3" fill="#B07A45" {S}/><path d="M48,66 L80,66 L96,106 L32,106 Z" fill="{SUN}" {S}/>'
        g += f'<path d="M50,78 L44,104 M64,78 L64,104 M78,78 L84,104" stroke="{INK}" stroke-width="2.5"/><rect x="46" y="62" width="36" height="10" rx="3" fill="{ROSE}" {S}/>'
    elif pid == "soap":
        g = f'<rect x="28" y="54" width="64" height="38" rx="12" fill="#C8F0FF" {S}/><path d="M38,64 L80,64" stroke="#fff" stroke-width="5" stroke-linecap="round"/>'
        g += "".join(f'<circle cx="{x}" cy="{y}" r="{r}" fill="#fff" fill-opacity=".7" stroke="{INK}" stroke-width="2.5"/>' for x, y, r in ((86, 42, 9), (68, 34, 6), (100, 28, 5)))
    elif pid == "vinegar":
        g = f'<path d="M54,22 L74,22 L74,38 Q90,46 90,62 L90,100 Q90,106 84,106 L44,106 Q38,106 38,100 L38,62 Q38,46 54,38 Z" fill="#FFF3C4" {S}/>'
        g += f'<rect x="52" y="16" width="24" height="10" rx="3" fill="{ROSE}" {S}/><rect x="42" y="66" width="44" height="22" rx="4" fill="#fff" {S}/><path d="M54,72 L64,84 L74,72" fill="none" stroke="{INK}" stroke-width="3"/>'
    elif pid == "soda":
        g = f'<path d="M34,44 L94,44 L90,104 L38,104 Z" fill="#fff" {S}/><path d="M34,44 L44,30 L84,30 L94,44" fill="#DDE3EA" {S}/><rect x="44" y="60" width="40" height="22" rx="4" fill="{FAM["chem"]}" stroke="{INK}" stroke-width="2.5"/>'
        g += "".join(f'<circle cx="{x}" cy="{y}" r="2" fill="{INK}"/>' for x, y in ((52, 92), (62, 96), (74, 92), (80, 98), (46, 98)))
    elif pid == "polish":
        g = f'<rect x="34" y="50" width="60" height="48" rx="8" fill="#F2C14E" {S}/><rect x="30" y="42" width="68" height="14" rx="5" fill="{MUTED}" {S}/><path d="M44,70 L84,70 M44,80 L74,80" stroke="#fff" stroke-width="4" stroke-linecap="round"/>'
        g += sparkle(92, 32, 12, "#fff") + sparkle(28, 38, 7, "#fff")
    elif pid == "hot":
        g = f'<path d="M64,26 Q90,60 90,78 A26,26 0 0 1 38,78 Q38,60 64,26 Z" fill="#FF8FA3" {S}/><path d="M54,74 Q54,64 62,58" fill="none" stroke="#fff" stroke-width="4" stroke-linecap="round"/>'
        g += f'<path d="M30,40 q-6,-8 0,-16 M98,40 q6,-8 0,-16" fill="none" stroke="{ROSE}" stroke-width="4" stroke-linecap="round"/>'
    elif pid == "steam":
        for x, y, r in ((44, 80, 16), (64, 70, 20), (86, 80, 15), (64, 90, 16)):
            g += f'<circle cx="{x}" cy="{y}" r="{r}" fill="#fff" {S}/>'
        g += f'<rect x="30" y="80" width="68" height="18" fill="#fff"/><path d="M30,98 L98,98" {S}/>'
        g += f'<path d="M50,44 q-6,-8 0,-16 q6,-8 0,-16 M70,44 q-6,-8 0,-16 q6,-8 0,-16" fill="none" stroke="{MUTED}" stroke-width="4" stroke-linecap="round"/>'
    elif pid == "scrub":
        g = f'<path d="M30,92 L44,48 L58,92 L72,48 L86,92 L98,56" fill="none" stroke="{ROSE}" stroke-width="9" stroke-linecap="round" stroke-linejoin="round"/>'
        g += f'<path d="M30,92 L44,48 L58,92 L72,48 L86,92 L98,56" fill="none" stroke="{INK}" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/>'
        g += f'<path d="M92,30 l8,-8 M100,40 l10,-4 M82,26 l2,-10" stroke="{INK}" stroke-width="3.5" stroke-linecap="round"/>'
    elif pid == "air":
        g = f'<path d="M24,52 L78,52 A12,12 0 1 0 66,40" fill="none" stroke="{INK}" stroke-width="7" stroke-linecap="round"/>'
        g += f'<path d="M24,70 L94,70 A12,12 0 1 1 82,82" fill="none" stroke="{INK}" stroke-width="7" stroke-linecap="round"/>'
        g += f'<path d="M34,88 L66,88" stroke="{INK}" stroke-width="7" stroke-linecap="round"/>'
        g += f'<path d="M24,52 L78,52 A12,12 0 1 0 66,40 M24,70 L94,70 A12,12 0 1 1 82,82 M34,88 L66,88" fill="none" stroke="#C8F0FF" stroke-width="3" stroke-linecap="round"/>'
    elif pid == "soak":
        g = f'<path d="M30,48 L98,48 L90,104 L38,104 Z" fill="#C3CBD6" {S}/><ellipse cx="64" cy="50" rx="34" ry="9" fill="{FAM["chem"]}" {S}/>'
        g += f'<path d="M36,48 Q64,20 92,48" fill="none" {S}/><path d="M50,72 q6,-6 12,0 t12,0" fill="none" stroke="#fff" stroke-width="3"/>'
    elif pid == "spray":
        g = f'<rect x="56" y="48" width="30" height="54" rx="8" fill="#DCD0FF" {S}/><path d="M60,48 L60,36 L82,36 L94,30 L94,40 L84,40 L82,48" fill="{ROSE}" {S}/>'
        g += "".join(f'<circle cx="{x}" cy="{y}" r="{r}" fill="{FAM["chem"]}"/>' for x, y, r in ((44, 32, 3), (36, 40, 3.5), (30, 28, 2.5), (40, 22, 2.5), (26, 46, 2)))
    elif pid == "circle":
        g = f'<path d="M94,64 A30,30 0 1 1 82,40" fill="none" stroke="{INK}" stroke-width="10" stroke-linecap="round"/>'
        g += f'<path d="M94,64 A30,30 0 1 1 82,40" fill="none" stroke="{FAM["method"]}" stroke-width="5" stroke-linecap="round"/>'
        g += f'<path d="M72,30 L90,34 L84,50 Z" fill="{FAM["method"]}" {S}/>' + sparkle(64, 64, 9, SUN)
    return o + g

def make_parts():
    for pid in PARTS:
        sprite(f"Sprites/Parts/part_{pid}", 128, 128, part_icon(pid), group="parts", owner=pid, family=PARTS[pid])

# ---------------------------------------------------------------- props
def make_props():
    # cart: 3/4 view trolley, 3 shelves, body w/ layers drawn in-game on top
    o = f'<ellipse cx="96" cy="150" rx="78" ry="10" fill="#000" fill-opacity=".15"/>'
    o += f'<path d="M150,30 L172,14" stroke="{INK}" stroke-width="7" stroke-linecap="round"/><path d="M150,30 L172,14" stroke="{MUTED}" stroke-width="3" stroke-linecap="round"/>'
    o += f'<rect x="24" y="30" width="134" height="108" rx="10" fill="#DDE3EA" {S}/>'
    for y in (60, 92):
        o += f'<rect x="24" y="{y}" width="134" height="8" fill="{MUTED}" stroke="{INK}" stroke-width="2.5"/>'
    o += f'<rect x="24" y="30" width="134" height="12" rx="6" fill="{TEAL}" {S}/>'
    for x in (42, 140):
        o += f'<circle cx="{x}" cy="146" r="10" fill="{INK}"/><circle cx="{x}" cy="146" r="4" fill="#C3CBD6"/>'
    sprite("Sprites/Props/cart", 192, 160, o, group="props", pivot=[0.5, 0.06])
    # cart layer slab (tinted per family in-game; plain white so SpriteRenderer.color works)
    sprite("Sprites/Props/cart_layer", 128, 24, f'<rect x="4" y="3" width="120" height="18" rx="6" fill="#fff" {S}/><path d="M14,9 L60,9" stroke="#fff" stroke-width="3" stroke-opacity=".8"/>', group="props")
    # sofa
    o = f'<ellipse cx="128" cy="148" rx="118" ry="10" fill="#000" fill-opacity=".15"/>'
    o += f'<rect x="20" y="30" width="216" height="70" rx="22" fill="#E07A5F" {S}/>'
    o += f'<rect x="34" y="70" width="92" height="56" rx="14" fill="#F2A07B" {S}/><rect x="130" y="70" width="92" height="56" rx="14" fill="#F2A07B" {S}/>'
    o += f'<rect x="6" y="56" width="34" height="84" rx="14" fill="#E07A5F" {S}/><rect x="216" y="56" width="34" height="84" rx="14" fill="#E07A5F" {S}/>'
    o += f'<rect x="30" y="136" width="10" height="12" fill="{INK}"/><rect x="216" y="136" width="10" height="12" fill="{INK}"/>'
    sprite("Sprites/Props/sofa", 256, 160, o, group="props", pivot=[0.5, 0.06])
    # stations (shelf per family, with that family's 2 example icons)
    sample = {"tool": ("broom", "sponge"), "chem": ("soap", "vinegar"), "energy": ("hot", "steam"), "method": ("soak", "circle")}
    for fam, col in FAM.items():
        o = f'<ellipse cx="96" cy="122" rx="86" ry="7" fill="#000" fill-opacity=".15"/>'
        o += f'<rect x="8" y="20" width="176" height="100" rx="14" fill="{col}" {S}/>'
        o += f'<rect x="18" y="30" width="156" height="80" rx="8" fill="{SURFACE}" fill-opacity=".85" stroke="{INK}" stroke-width="2.5"/>'
        o += f'<rect x="18" y="68" width="156" height="6" fill="{col}"/>'
        for i, pid in enumerate(sample[fam]):
            o += f'<g transform="translate({30 + i*78},26) scale(0.42)">{part_icon(pid)}</g>'
        sprite(f"Sprites/Props/station_{fam}", 192, 128, o, group="props", pivot=[0.5, 0.06], family=fam)
    # floor tiles
    sprite("Sprites/Props/floor_tile", 64, 64, f'<rect width="64" height="64" fill="{FLOOR}"/><rect width="32" height="32" fill="{FLOOR2}"/><rect x="32" y="32" width="32" height="32" fill="{FLOOR2}"/>', group="props", tile=True)
    sprite("Sprites/Props/floor_tile_dirty", 64, 64, f'<rect width="64" height="64" fill="{FLOOR}"/><rect width="32" height="32" fill="{FLOOR2}"/><rect x="32" y="32" width="32" height="32" fill="{FLOOR2}"/>'
           f'<path d="{blob(30, 36, lambda a: (14 + 4*math.sin(a*5), 10 + 3*math.sin(a*3)))}" fill="#7A5C3E" fill-opacity=".45"/><circle cx="50" cy="16" r="3" fill="#7A5C3E" fill-opacity=".45"/>', group="props", tile=True)

# ---------------------------------------------------------------- UI
def make_ui():
    # 9-slice: borders recorded in manifest (in px at render scale)
    sprite("Sprites/UI/ui_button", 96, 48, f'<rect x="3" y="7" width="90" height="38" rx="14" fill="{INK}"/><rect x="3" y="3" width="90" height="38" rx="14" fill="{SUN}" {S}/><path d="M16,12 L60,12" stroke="#fff" stroke-width="4" stroke-linecap="round" stroke-opacity=".7"/>',
           group="ui", border=[18 * SCALE] * 4)
    sprite("Sprites/UI/ui_button_pressed", 96, 48, f'<rect x="3" y="7" width="90" height="38" rx="14" fill="#E9B949" {S}/>', group="ui", border=[18 * SCALE] * 4)
    sprite("Sprites/UI/ui_button_teal", 96, 48, f'<rect x="3" y="7" width="90" height="38" rx="14" fill="{INK}"/><rect x="3" y="3" width="90" height="38" rx="14" fill="{TEAL}" {S}/><path d="M16,12 L60,12" stroke="#fff" stroke-width="4" stroke-linecap="round" stroke-opacity=".5"/>',
           group="ui", border=[18 * SCALE] * 4)
    sprite("Sprites/UI/ui_panel", 96, 96, f'<rect x="3" y="3" width="90" height="90" rx="20" fill="{SURFACE}" {S}/>', group="ui", border=[24 * SCALE] * 4)
    sprite("Sprites/UI/ui_bar_bg", 64, 20, f'<rect x="2" y="2" width="60" height="16" rx="8" fill="#E8DFC4" {S}/>', group="ui", border=[10 * SCALE] * 4)
    sprite("Sprites/UI/ui_bar_fill", 64, 20, f'<rect x="2" y="2" width="60" height="16" rx="8" fill="#fff"/>', group="ui", border=[10 * SCALE] * 4)
    sprite("Sprites/UI/ui_card", 96, 128, f'<rect x="3" y="3" width="90" height="122" rx="14" fill="{SURFACE}" {S}/><rect x="10" y="10" width="76" height="76" rx="10" fill="#fff" stroke="{INK}" stroke-width="2"/>', group="ui", border=[16 * SCALE] * 4)
    icons = {
        "stat_clean": f'<path d="M48,14 Q74,44 74,60 A26,26 0 0 1 22,60 Q22,44 48,14 Z" fill="{FAM["chem"]}" {S}/>' + sparkle(60, 52, 9, "#fff"),
        "stat_gentle": f'<path d="M48,80 C10,54 14,22 34,22 C42,22 48,30 48,34 C48,30 54,22 62,22 C82,22 86,54 48,80 Z" fill="#FF8FA3" {S}/>',
        "stat_speed": f'<path d="M54,8 L22,54 L44,54 L38,88 L74,38 L52,38 Z" fill="{SUN}" {S}/>',
        "stat_shine": f'<path d="{star(48, 50, 36, 15)}" fill="{SUN}" {S}/>' + sparkle(78, 18, 8, "#fff"),
        "icon_timer": f'<circle cx="48" cy="52" r="34" fill="{SURFACE}" {S}/><rect x="40" y="6" width="16" height="10" rx="3" fill="{ROSE}" {S}/><path d="M48,52 L48,30 M48,52 L64,60" stroke="{INK}" stroke-width="5" stroke-linecap="round"/>',
        "icon_budget": f'<circle cx="48" cy="48" r="36" fill="{SUN}" {S}/><circle cx="48" cy="48" r="26" fill="none" stroke="#E9A93A" stroke-width="4"/><path d="M48,30 L48,66 M58,38 Q48,30 40,36 Q36,46 48,48 Q60,50 56,60 Q48,66 38,58" fill="none" stroke="{INK}" stroke-width="4" stroke-linecap="round"/>',
        "icon_star": f'<path d="{star(48, 50, 40, 17)}" fill="{SUN}" {S}/>',
        "icon_star_empty": f'<path d="{star(48, 50, 40, 17)}" fill="#E8DFC4" {S}/>',
        "icon_pause": f'<circle cx="48" cy="48" r="38" fill="{SURFACE}" {S}/><rect x="32" y="30" width="11" height="36" rx="3" fill="{INK}"/><rect x="53" y="30" width="11" height="36" rx="3" fill="{INK}"/>',
        "icon_check": f'<circle cx="48" cy="48" r="38" fill="#57CC99" {S}/><path d="M28,50 L42,64 L68,34" fill="none" stroke="#fff" stroke-width="9" stroke-linecap="round" stroke-linejoin="round"/>',
        "icon_cross": f'<circle cx="48" cy="48" r="38" fill="{ROSE}" {S}/><path d="M32,32 L64,64 M64,32 L32,64" stroke="#fff" stroke-width="9" stroke-linecap="round"/>',
        "icon_ability": f'<circle cx="48" cy="48" r="38" fill="{TEAL}" {S}/><path d="{star(48, 48, 24, 10, 4)}" fill="#fff" stroke="{INK}" stroke-width="3"/>',
        "icon_lift": f'<circle cx="48" cy="48" r="38" fill="{SURFACE}" {S}/><path d="M48,68 L48,30 M32,44 L48,28 L64,44" fill="none" stroke="{INK}" stroke-width="7" stroke-linecap="round" stroke-linejoin="round"/>',
    }
    for name, body in icons.items():
        sprite(f"Sprites/UI/{name}", 96, 96, body, group="ui")
    # placeholder logo
    o = f'<rect width="512" height="512" rx="110" fill="#FFF6DC"/>'
    o += f'<circle cx="256" cy="236" r="190" fill="{SUN}" stroke="{INK}" stroke-width="10"/>'
    o += f'<g transform="translate(96,60) scale(2.5)">{character("sweep", mouth="open", arms_up=True)}</g>'
    o += f'<g transform="translate(300,250) scale(1.4)">{enemy("dust", ph=1)}</g>'
    o += f'<rect x="56" y="392" width="400" height="80" rx="24" fill="{ROSE}" stroke="{INK}" stroke-width="8"/>'
    o += f'<text x="256" y="446" text-anchor="middle" font-family="Mitr, Kanit, Arial Rounded MT Bold, sans-serif" font-weight="700" font-size="40" textLength="360" lengthAdjust="spacingAndGlyphs" fill="#fff">BATTLE OF THE BROOMS</text>'
    sprite("Sprites/UI/logo_placeholder", 512, 512, o, group="ui")

# ---------------------------------------------------------------- FX
def make_fx():
    fr = []
    for i in range(6):  # bubble pop
        p = i / 5
        if i < 2:
            r = 26 + i * 6
            fr.append(f'<circle cx="64" cy="64" r="{r}" fill="#DFF4FF" fill-opacity=".7" stroke="{FAM["chem"]}" stroke-width="4"/><path d="M48,50 Q54,42 62,42" stroke="#fff" stroke-width="5" fill="none" stroke-linecap="round"/>')
        else:
            q = (i - 2) / 3
            g = ""
            for j in range(8):
                a = j / 8 * 2 * math.pi
                r1, r2 = 30 + 26 * q, 38 + 30 * q
                g += f'<path d="M{f(64+math.cos(a)*r1)},{f(64+math.sin(a)*r1)} L{f(64+math.cos(a)*r2)},{f(64+math.sin(a)*r2)}" stroke="{FAM["chem"]}" stroke-width="{f(5*(1-q)+1)}" stroke-linecap="round" opacity="{f(1-q*0.8)}"/>'
            fr.append(g)
    sheet("Sprites/FX/fx_bubble_pop", 128, 128, fr, fps=16, loop=False, group="fx")
    fr = []
    for i in range(6):  # sparkle (good reaction)
        p = i / 5
        s = math.sin(p * math.pi)
        fr.append(sparkle(64, 64, 10 + 34 * s, SUN, 3) + sparkle(34, 36, 12 * s + 0.1, "#fff") + sparkle(96, 92, 10 * s + 0.1, "#fff"))
    sheet("Sprites/FX/fx_sparkle", 128, 128, fr, fps=14, loop=False, group="fx")
    fr = []
    for i in range(6):  # steam puff
        p = i / 5
        g = ""
        for j, (dx, r0) in enumerate(((-16, 14), (10, 18), (0, 12))):
            y = 96 - p * 60 - j * 8
            g += f'<circle cx="{f(64+dx+math.sin(p*4+j)*6)}" cy="{f(y)}" r="{f(r0*(0.6+p*0.8))}" fill="#fff" fill-opacity="{f(0.9*(1-p**1.5))}" stroke="{MUTED}" stroke-width="3" stroke-opacity="{f(1-p)}"/>'
        fr.append(g)
    sheet("Sprites/FX/fx_steam", 128, 128, fr, fps=12, loop=False, group="fx")
    fr = []
    for i in range(6):  # dust poof / bad reaction smoke
        p = i / 5
        g = ""
        for j in range(6):
            a = j / 6 * 2 * math.pi
            r = 10 + 36 * p
            g += f'<circle cx="{f(64+math.cos(a)*r)}" cy="{f(70+math.sin(a)*r*0.6)}" r="{f(14*(1-p*0.6))}" fill="#A9A39A" fill-opacity="{f(1-p)}" stroke="{INK}" stroke-width="2.5" stroke-opacity="{f(1-p)}"/>'
        fr.append(g)
    sheet("Sprites/FX/fx_poof", 128, 128, fr, fps=14, loop=False, group="fx")
    fr = []
    for i in range(6):  # fizz (vinegar + soda)
        g = ""
        for j in range(10):
            x = 24 + (j * 37) % 80
            y = 110 - ((i * 18 + j * 23) % 96)
            r = 3 + (j % 3) * 2
            g += f'<circle cx="{x}" cy="{y}" r="{r}" fill="#fff" stroke="{FAM["chem"]}" stroke-width="2.5"/>'
        fr.append(g)
    sheet("Sprites/FX/fx_fizz", 128, 128, fr, fps=12, loop=True, group="fx")
    fr = []
    for i in range(4):  # target ring (move marker / drop zone)
        r = 30 + i * 6
        fr.append(f'<ellipse cx="64" cy="64" rx="{r}" ry="{f(r*0.45)}" fill="none" stroke="{ROSE}" stroke-width="{6-i}" opacity="{f(1-i*0.22)}"/>')
    sheet("Sprites/FX/fx_target", 128, 128, fr, fps=8, loop=True, group="fx")

# ================================================================ concept pass (docs/concept/*.png)
# Enemies restyled to the concept: big white eyes, thick angry brows, toothy open mouth.
# ids stain..scale stay (BobRules.cs uses them); stain = เจ้าคราบ Dirt Monster, grease = สไลม์ Slime.
# trash (ขยะปีศาจ), germ (เชื้อโรค) and boss (บอสคราบยักษ์) are new art, not yet in the rules.
ENEMIES = {
    "stain":  {"col": "#8A5226", "hi": "#B98250", "th": "เจ้าคราบ"},
    "dust":   {"col": "#A9A39A", "hi": "#D8D3CB", "th": "ฝุ่น"},
    "grease": {"col": "#8B4FC8", "hi": "#C9A2F2", "th": "สไลม์"},
    "rust":   {"col": "#B5532A", "hi": "#DD8256", "th": "สนิม"},
    "scale":  {"col": "#8FB8CC", "hi": "#DDF2FA", "th": "หินปูน"},
    "trash":  {"col": "#8C96A6", "hi": "#C9D0DA", "th": "ขยะปีศาจ"},
    "germ":   {"col": "#7BC144", "hi": "#BDE695", "th": "เชื้อโรค"},
    "boss":   {"col": "#7A3FB8", "hi": "#B98BEA", "th": "บอสคราบยักษ์"},
}
MOUTH = "#3A1F2B"

def enemy_shape(eid, cx, cy, ph, k=1.0):
    if eid == "stain":    # lumpy mud pile, wide flat base
        def rf(a):
            top = max(0.0, -math.sin(a))
            rx = 37 + 3 * math.sin(a * 6 + ph * 0.6)
            ry = 28 + 9 * top ** 2 + 3 * math.sin(a * 5 - ph)
            if math.sin(a) > 0.55: ry = 26
            return (k * rx, k * ry)
        return blob(cx, cy + 4 * k, rf, 64)
    if eid in ("grease", "boss"):  # slime: tall drip peak on top, flat puddle base
        def rf(a):
            top = max(0.0, -math.sin(a))
            rx = 38 + 2.5 * math.sin(a * 3 + ph)
            ry = 26 + 14 * top ** 4 + 2 * math.sin(a * 4 + ph)
            if math.sin(a) > 0.6: ry = 25
            return (k * rx, k * ry)
        return blob(cx, cy + 6 * k, rf, 64)
    if eid == "germ":     # round with soft bumps
        return blob(cx, cy, lambda a: (k * (30 + 3 * math.sin(a * 8 + ph)), k * (28 + 3 * math.sin(a * 8 + ph))), 64)
    if eid == "trash":    # trash can body (slightly wider at the top)
        w1, w2, t, b = 30 * k, 25 * k, cy - 26 * k, cy + 36 * k
        return f"M{f(cx-w1)},{f(t)} L{f(cx+w1)},{f(t)} L{f(cx+w2)},{f(b-6*k)} Q{f(cx+w2)},{f(b)} {f(cx+w2-6*k)},{f(b)} L{f(cx-w2+6*k)},{f(b)} Q{f(cx-w2)},{f(b)} {f(cx-w2)},{f(b-6*k)} Z"
    if eid == "dust":
        rf = lambda a: (k * (34 + 5 * abs(math.sin(a * 4.5 + ph * 0.5)) + 2 * math.sin(a * 9 + ph)),) * 2
        return blob(cx, cy, lambda a: (rf(a)[0], rf(a)[0] * 0.82), 72)
    if eid == "rust":
        n = 14; pts = []
        for i in range(n):
            a = i / n * 2 * math.pi
            r = k * (34 + (7 if i % 2 else -2) + 2 * math.sin(ph + i))
            pts.append(f"{f(cx + math.cos(a) * r)},{f(cy + math.sin(a) * r * 0.85)}")
        return "M" + " L".join(pts) + "Z"
    pts = []  # scale: crystal
    for i in range(6):
        a = math.radians(i * 60 - 90)
        r = k * (36 + 2 * math.sin(ph + i * 2))
        pts.append(f"{f(cx + math.cos(a) * r * 0.95)},{f(cy + math.sin(a) * r * 0.85)}")
    return "M" + " L".join(pts) + "Z"

def enemy_back(eid, cx, cy, ph, k=1.0):
    """Drawn behind the body."""
    o = ""
    if eid in ("grease", "boss"):  # orbiting droplets
        for i in range(5):
            a = i / 5 * 2 * math.pi + ph * 0.5
            x, y = cx + math.cos(a) * 46 * k, cy + 6 * k + math.sin(a) * 26 * k
            o += f'<ellipse cx="{f(x)}" cy="{f(y)}" rx="{f(4.5*k)}" ry="{f(5.5*k)}" fill="{ENEMIES[eid]["col"]}" {S}/><circle cx="{f(x-1.5*k)}" cy="{f(y-2*k)}" r="{f(1.4*k)}" fill="#fff"/>'
    if eid == "germ":     # antennae
        for s_ in (-1, 1):
            tx, ty = cx + s_ * 14 * k + math.sin(ph) * 3 * s_, cy - 42 * k
            o += f'<path d="M{f(cx+s_*8*k)},{f(cy-22*k)} Q{f(cx+s_*16*k)},{f(cy-34*k)} {f(tx)},{f(ty)}" fill="none" stroke="{INK}" stroke-width="3.5" stroke-linecap="round"/><circle cx="{f(tx)}" cy="{f(ty)}" r="{f(5*k)}" fill="{ENEMIES[eid]["col"]}" {S}/>'
    return o

def enemy_extras(eid, cx, cy, ph, k=1.0):
    o = ""
    e = ENEMIES[eid]
    if eid == "stain":
        for dx, dy, r in ((-18, 16, 3), (16, 22, 2.5), (22, -6, 2), (-24, -2, 2.2)):
            o += f'<circle cx="{f(cx+dx*k)}" cy="{f(cy+dy*k)}" r="{f(r*k)}" fill="#5E3516"/>'
        for dx, dy, r in ((-44, 30, 5), (42, 28, 4), (36, -20, 3)):
            o += f'<circle cx="{f(cx+dx*k)}" cy="{f(cy+dy*k)}" r="{f(r*k)}" fill="{e["col"]}" {S}/>'
    elif eid in ("grease", "boss"):
        o += f'<path d="M{f(cx-22*k)},{f(cy-6*k)} Q{f(cx-20*k)},{f(cy-24*k)} {f(cx-6*k)},{f(cy-30*k)}" fill="none" stroke="#fff" stroke-width="{f(4*k)}" stroke-linecap="round" stroke-opacity=".8"/>'
        o += f'<circle cx="{f(cx+24*k)}" cy="{f(cy+18*k)}" r="{f(3*k)}" fill="#F7B6E8" fill-opacity=".8"/>'
    elif eid == "trash":
        lift = max(0.0, math.sin(ph)) * 8 * k
        o += f'<path d="M{f(cx-14*k)},{f(cy-26*k)} L{f(cx-14*k)},{f(cy+30*k)} M{f(cx+14*k)},{f(cy-26*k)} L{f(cx+14*k)},{f(cy+30*k)}" stroke="{INK}" stroke-width="2" stroke-opacity=".35"/>'
        o += f'<g transform="rotate({f(-12*lift/(8*k) if k else 0)},{f(cx-30*k)},{f(cy-28*k)})"><rect x="{f(cx-34*k)}" y="{f(cy-36*k-lift)}" width="{f(68*k)}" height="{f(10*k)}" rx="{f(4*k)}" fill="{e["hi"]}" {S}/><rect x="{f(cx-7*k)}" y="{f(cy-42*k-lift)}" width="{f(14*k)}" height="{f(7*k)}" rx="{f(3*k)}" fill="{e["col"]}" {S}/></g>'
        o += f'<rect x="{f(cx-22*k)}" y="{f(cy-20*k)}" width="{f(44*k)}" height="{f(34*k)}" rx="{f(10*k)}" fill="#22202A"/>'
    elif eid == "germ":
        for s_ in (-1, 1):
            o += f'<ellipse cx="{f(cx+s_*14*k)}" cy="{f(cy+30*k)}" rx="{f(7*k)}" ry="{f(4*k)}" fill="{e["col"]}" {S}/>'
        for dx, dy in ((-18, 12), (20, 4), (6, 20)):
            o += f'<circle cx="{f(cx+dx*k)}" cy="{f(cy+dy*k)}" r="{f(2.6*k)}" fill="#4E8F2A"/>'
    elif eid == "rust":
        for dx, dy in ((-16, -14), (14, -18), (20, 12)):
            o += f'<circle cx="{f(cx+dx*k)}" cy="{f(cy+dy*k)}" r="{f(3*k)}" fill="#7A2E12"/>'
    elif eid == "scale":
        o += f'<path d="M{f(cx)},{f(cy-30*k)} L{f(cx-10*k)},{f(cy-4*k)} L{f(cx)},{f(cy+28*k)} M{f(cx)},{f(cy-30*k)} L{f(cx+12*k)},{f(cy-2*k)} L{f(cx)},{f(cy+28*k)}" fill="none" stroke="#fff" stroke-width="2.5" stroke-opacity=".7"/>'
    elif eid == "dust":
        for i, (dx, dy) in enumerate(((-38, -24), (36, -28), (-30, 26), (40, 18))):
            o += f'<circle cx="{f(cx+dx*k)}" cy="{f(cy+dy*k + math.sin(ph+i)*3)}" r="{f(3 + 1.5 * math.sin(ph + i))}" fill="#CFCAC2" stroke="{INK}" stroke-width="2"/>'
    if eid == "boss":     # golden crown
        cy0 = cy - 38 * k
        pts = [(-16, 0), (-18, -16), (-8, -7), (0, -20), (8, -7), (18, -16), (16, 0)]
        d = "M" + " L".join(f"{f(cx+x*k)},{f(cy0+y*k)}" for x, y in pts) + "Z"
        o += f'<path d="{d}" fill="#F6C431" {S}/><circle cx="{f(cx)}" cy="{f(cy0-6*k)}" r="{f(2.6*k)}" fill="{ROSE}" stroke="{INK}" stroke-width="1.5"/>'
    return o

def enemy_face(eid, cx, cy, face, k=1.0):
    o = ""
    ex, ey = 12 * k, cy - 4 * k
    if eid == "trash": ey = cy - 6 * k
    if face == "angry":
        for s_ in (-1, 1):
            o += f'<ellipse cx="{f(cx+s_*ex)}" cy="{f(ey)}" rx="{f(8.5*k)}" ry="{f(9.5*k)}" fill="#fff" stroke="{INK}" stroke-width="3"/>'
            o += f'<circle cx="{f(cx+s_*ex-s_*2*k)}" cy="{f(ey+2*k)}" r="{f(4*k)}" fill="{INK}"/><circle cx="{f(cx+s_*ex-s_*2*k+1.2*k)}" cy="{f(ey+0.6*k)}" r="{f(1.3*k)}" fill="#fff"/>'
        o += f'<path d="M{f(cx-23*k)},{f(ey-14*k)} L{f(cx-5*k)},{f(ey-7*k)} M{f(cx+23*k)},{f(ey-14*k)} L{f(cx+5*k)},{f(ey-7*k)}" stroke="{INK}" stroke-width="{f(5*k)}" stroke-linecap="round"/>'
        if eid != "trash":
            my = cy + 12 * k
            o += f'<path d="M{f(cx-11*k)},{f(my)} Q{f(cx)},{f(my-4*k)} {f(cx+11*k)},{f(my)} Q{f(cx)},{f(my+11*k)} {f(cx-11*k)},{f(my)} Z" fill="{MOUTH}" stroke="{INK}" stroke-width="2.5" stroke-linejoin="round"/>'
            o += f'<path d="M{f(cx-7*k)},{f(my-1.5*k)} l{f(2.5*k)},{f(4*k)} l{f(2.5*k)},{f(-4.4*k)} M{f(cx+2*k)},{f(my-2.4*k)} l{f(2.5*k)},{f(4*k)} l{f(2.5*k)},{f(-3.6*k)}" fill="#fff" stroke="{INK}" stroke-width="1.2"/>'
        else:
            o += f'<path d="M{f(cx-8*k)},{f(cy+8*k)} Q{f(cx)},{f(cy+3*k)} {f(cx+8*k)},{f(cy+8*k)}" fill="none" stroke="#fff" stroke-width="2.5" stroke-linecap="round"/>'
    elif face == "hurt":
        col = "#fff" if eid == "trash" else INK
        for s_ in (-1, 1):
            o += f'<path d="M{f(cx+s_*ex-5*k)},{f(ey-5*k)} l{f(10*k)},{f(10*k)} M{f(cx+s_*ex+5*k)},{f(ey-5*k)} l{f(-10*k)},{f(10*k)}" stroke="{col}" stroke-width="{f(3.5*k)}" stroke-linecap="round"/>'
        o += f'<ellipse cx="{f(cx)}" cy="{f(cy+13*k)}" rx="{f(6*k)}" ry="{f(5*k)}" fill="{MOUTH if eid != "trash" else "#fff"}"/>'
    elif face == "dizzy":
        col = "#fff" if eid == "trash" else INK
        for s_ in (-1, 1):
            o += f'<path d="M{f(cx+s_*ex)},{f(ey)} m-5,0 a5,5 0 1,1 5,5 a3,3 0 1,1 -3,-3" fill="none" stroke="{col}" stroke-width="2.5"/>'
        o += f'<path d="M{f(cx-8*k)},{f(cy+13*k)} q4,-4 8,0 q4,4 8,0" fill="none" stroke="{col}" stroke-width="3" stroke-linecap="round"/>'
    return o

def enemy(eid, ph=0.0, squash=1.0, face="angry", flash=0.0, k=1.0, alpha=1.0, dy=0.0):
    e = ENEMIES[eid]
    cx, cy = 64, 64 + dy
    o = f'<ellipse cx="64" cy="108" rx="{f(36*k/squash)}" ry="{f(7*k)}" fill="#000" fill-opacity="{f(.15*alpha)}"/>'
    o += f'<g opacity="{f(alpha)}" transform="translate(64,104) scale({f(1/squash)},{f(squash)}) translate(-64,-104)">'
    o += enemy_back(eid, cx, cy, ph, k)
    o += f'<path d="{enemy_shape(eid, cx, cy, ph, k)}" fill="{e["col"]}" {S}/>'
    if eid != "trash":
        o += f'<path d="{enemy_shape(eid, cx-7*k, cy-9*k, ph, k*0.42)}" fill="{e["hi"]}" fill-opacity=".5"/>'
    o += enemy_extras(eid, cx, cy, ph, k)
    o += enemy_face(eid, cx, cy, face, k)
    if flash > 0:
        o += f'<path d="{enemy_shape(eid, cx, cy, ph, k)}" fill="#fff" fill-opacity="{f(flash)}"/>'
    return o + "</g>"

def make_enemies():
    for eid in ENEMIES:
        big = eid == "boss"
        W = 256 if big else 128
        wrap = (lambda b: f'<g transform="translate(13,30) scale(1.8)">{b}</g>') if big else (lambda b: b)
        base = f"Sprites/Enemies/{eid}/{eid}"
        meta = dict(group="enemies", owner=eid)
        idle = [wrap(enemy(eid, ph=i / 8 * 2 * math.pi, squash=1 + 0.05 * math.sin(i / 8 * 2 * math.pi), dy=-2 * math.sin(i / 8 * 2 * math.pi))) for i in range(8)]
        sheet(base + "_idle", W, W, idle, fps=10, pivot=(0.5, 0.15), anim="idle", **meta)
        attack = [wrap(enemy(eid, ph=i, squash=s, dy=d)) for i, (s, d) in enumerate(((1.12, 4), (0.85, -6), (0.8, -10), (1.18, 4), (1.0, 0)))]
        sheet(base + "_attack", W, W, attack, fps=12, loop=False, pivot=(0.5, 0.15), anim="attack", **meta)
        hit = [wrap(enemy(eid, ph=0, squash=s, face="hurt", flash=fl)) for s, fl in ((0.8, 0.8), (1.15, 0.4), (0.95, 0.1), (1.0, 0))]
        sheet(base + "_hit", W, W, hit, fps=14, loop=False, pivot=(0.5, 0.15), anim="hit", **meta)
        defeat = []
        for i in range(8):
            p = i / 7
            fr = enemy(eid, ph=p * 6, squash=1 + 0.3 * math.sin(p * math.pi), face="dizzy", k=max(0.05, 1 - p ** 1.6), alpha=max(0, 1 - p ** 2), flash=0.3 * p)
            for j in range(5):
                a = j / 5 * 2 * math.pi + 0.4
                r = 20 + 40 * p
                if 0.15 < p < 1.0:
                    fr += sparkle(64 + math.cos(a) * r, 64 + math.sin(a) * r * 0.8, 8 * (1 - p) + 3, SUN)
            defeat.append(wrap(fr))
        sheet(base + "_defeat", W, W, defeat, fps=12, loop=False, pivot=(0.5, 0.15), anim="defeat", **meta)
        spawn = [wrap(enemy(eid, k=max(0.05, s), squash=q, alpha=min(1, s * 1.5))) for s, q in ((0.1, 1), (0.4, 1.2), (0.8, 1.15), (1.1, 0.9), (1.0, 1.0))]
        sheet(base + "_spawn", W, W, spawn, fps=12, loop=False, pivot=(0.5, 0.15), anim="spawn", **meta)
        sprite(f"Sprites/Enemies/{eid}/{eid}_portrait", 128, 128,
               f'<circle cx="64" cy="64" r="60" fill="{ENEMIES[eid]["col"]}" fill-opacity=".22"/>' + enemy(eid, ph=0.6, k=0.95 if not big else 0.85), group="enemies", owner=eid)

# ---------------------------------------------------------------- concept: items, school props, floors, decals
GOLD = "#F6C431"; GOLD_D = "#D99A1E"; WATER = "#3695F2"; NAVY = "#2E4484"; RED = "#FB2F44"; ORANGE = "#FD7713"

def item_icon(name):
    if name == "coin":
        return (f'<circle cx="48" cy="50" r="34" fill="{GOLD_D}" {S}/><circle cx="48" cy="46" r="34" fill="{GOLD}" {S}/>'
                f'<circle cx="48" cy="46" r="24" fill="none" stroke="{GOLD_D}" stroke-width="4"/><path d="{star(48, 47, 13, 6)}" fill="#FFE27A" stroke="{GOLD_D}" stroke-width="2.5"/>'
                f'<path d="M28,30 Q34,22 44,20" stroke="#fff" stroke-width="4" fill="none" stroke-linecap="round" opacity=".8"/>')
    if name == "star":
        return f'<path d="{star(48, 52, 40, 18)}" fill="{GOLD}" {S}/><path d="M36,40 L44,30" stroke="#fff" stroke-width="5" stroke-linecap="round" opacity=".8"/>' + sparkle(80, 18, 7, "#fff", 2)
    if name == "gem":
        return (f'<path d="M24,36 L36,18 L60,18 L72,36 L48,82 Z" fill="{WATER}" {S}/><path d="M24,36 L72,36 M36,18 L42,36 L48,82 L54,36 L60,18" fill="none" stroke="{INK}" stroke-width="2.5" stroke-linejoin="round"/>'
                f'<path d="M30,34 L38,22" stroke="#fff" stroke-width="4" stroke-linecap="round" opacity=".85"/>')
    if name == "heart":
        return f'<path d="M48,82 C8,56 12,20 32,20 C40,20 48,28 48,34 C48,28 56,20 64,20 C84,20 88,56 48,82 Z" fill="{RED}" {S}/><path d="M26,34 Q28,26 36,26" stroke="#fff" stroke-width="5" fill="none" stroke-linecap="round" opacity=".8"/>'
    if name == "stopwatch":
        return (f'<rect x="40" y="6" width="16" height="10" rx="3" fill="{RED}" {S}/><path d="M70,22 l8,-8" {S}/>'
                f'<circle cx="48" cy="54" r="34" fill="{ORANGE}" {S}/><circle cx="48" cy="54" r="25" fill="#FFF7E6" stroke="{INK}" stroke-width="3"/>'
                f'<path d="M48,54 L48,36 M48,54 L60,62" stroke="{INK}" stroke-width="4.5" stroke-linecap="round"/>')
    if name == "magnet":
        return (f'<path d="M22,30 L22,52 A26,26 0 0 0 74,52 L74,30 L58,30 L58,52 A10,10 0 0 1 38,52 L38,30 Z" fill="{RED}" {S}/>'
                f'<rect x="22" y="18" width="16" height="14" fill="#DDE3EA" {S}/><rect x="58" y="18" width="16" height="14" fill="#DDE3EA" {S}/>'
                f'<path d="M14,12 l-6,-6 M30,6 l0,-4 M66,6 l0,-4 M82,12 l6,-6" stroke="{INK}" stroke-width="3" stroke-linecap="round"/>')
    if name == "bubble":
        return f'<circle cx="48" cy="48" r="34" fill="#DFF4FF" fill-opacity=".65" stroke="{WATER}" stroke-width="4"/><path d="M28,38 Q32,26 44,22" stroke="#fff" stroke-width="6" fill="none" stroke-linecap="round"/><circle cx="66" cy="66" r="4" fill="#fff"/>'
    if name == "chest_closed" or name == "chest_open":
        o = f'<rect x="14" y="44" width="68" height="40" rx="6" fill="#C97B3A" {S}/><rect x="14" y="56" width="68" height="6" fill="{GOLD}" stroke="{INK}" stroke-width="2"/>'
        if name == "chest_closed":
            o += f'<path d="M14,46 Q14,22 48,22 Q82,22 82,46 Z" fill="#E08E45" {S}/><rect x="42" y="40" width="12" height="16" rx="3" fill="{GOLD}" {S}/>'
        else:
            o += f'<path d="M14,40 Q18,8 52,10 Q80,14 82,30 Z" fill="#E08E45" {S}/><path d="M22,46 L74,46" stroke="{GOLD}" stroke-width="10"/>' + sparkle(48, 30, 12, GOLD, 2.5) + sparkle(24, 24, 7, "#fff", 2) + sparkle(74, 20, 6, "#fff", 2)
        return o
    if name == "trophy":
        return (f'<path d="M30,14 L66,14 L64,44 A16,16 0 0 1 32,44 Z" fill="{GOLD}" {S}/><path d="M30,20 Q14,20 18,34 Q22,44 32,44 M66,20 Q82,20 78,34 Q74,44 64,44" fill="none" {S}/>'
                f'<rect x="42" y="58" width="12" height="12" fill="{GOLD_D}" {S}/><rect x="28" y="70" width="40" height="12" rx="3" fill="{NAVY}" {S}/><path d="{star(48, 32, 9, 4)}" fill="#FFE27A"/>')
    return ""

ITEMS = ["coin", "star", "gem", "heart", "stopwatch", "magnet", "bubble", "chest_closed", "chest_open", "trophy"]

def shadow(cx, cy, rx, ry=None):
    return f'<ellipse cx="{cx}" cy="{cy}" rx="{rx}" ry="{ry or max(4, rx*0.18)}" fill="#000" fill-opacity=".15"/>'

def school_prop(name):
    """(w, h, svg body). Drawn front-on with a slight top-down tilt, pivot at the bottom centre."""
    if name == "wet_sign":
        return 64, 96, shadow(32, 90, 24) + f'<path d="M12,88 L26,10 L38,10 L52,88" fill="#FFD23F" {S}/><path d="M20,52 L44,52" stroke="{INK}" stroke-width="2"/><path d="M32,26 L42,44 L22,44 Z" fill="none" stroke="{INK}" stroke-width="3" stroke-linejoin="round"/><path d="M32,32 L32,38" stroke="{INK}" stroke-width="3"/><path d="M26,66 q6,-8 12,0" fill="none" stroke="{INK}" stroke-width="3"/>'
    if name == "cone":
        return 64, 80, shadow(32, 74, 24) + f'<rect x="8" y="64" width="48" height="10" rx="3" fill="{ORANGE}" {S}/><path d="M18,66 L28,8 L36,8 L46,66 Z" fill="{ORANGE}" {S}/><path d="M23,40 L41,40 L43,52 L21,52 Z" fill="#fff"/>'
    if name == "bucket":
        return 80, 80, shadow(40, 74, 28) + f'<path d="M14,22 Q40,-6 66,22" fill="none" stroke="{INK}" stroke-width="4"/><path d="M12,24 L68,24 L62,72 L18,72 Z" fill="{WATER}" {S}/><ellipse cx="40" cy="24" rx="28" ry="7" fill="#9ED8F5" {S}/><path d="M22,36 L24,62" stroke="#fff" stroke-width="4" stroke-linecap="round" opacity=".7"/>'
    if name == "trash_bag":
        return 80, 80, shadow(40, 74, 30) + f'<path d="M14,70 Q6,40 30,30 L34,22 L46,22 L50,30 Q74,40 66,70 Z" fill="#2E2C36" {S}/><path d="M34,22 L28,10 M46,22 L52,10" stroke="{INK}" stroke-width="4" stroke-linecap="round"/><path d="M24,46 Q28,38 36,36" stroke="#6A6878" stroke-width="4" fill="none" stroke-linecap="round"/>'
    if name in ("trash_bin", "recycle_bin"):
        col = "#8C96A6" if name == "trash_bin" else WATER
        o = shadow(40, 92, 28) + f'<path d="M14,22 L66,22 L60,88 L20,88 Z" fill="{col}" {S}/><rect x="10" y="12" width="60" height="12" rx="4" fill="#DDE3EA" {S}/>'
        if name == "recycle_bin":
            o += f'<path d="M40,40 L48,54 L32,54 Z M40,44 l0,0" fill="none" stroke="#fff" stroke-width="4" stroke-linejoin="round"/><path d="M30,64 Q40,72 50,64" fill="none" stroke="#fff" stroke-width="4" stroke-linecap="round"/>'
        else:
            o += f'<path d="M30,30 L32,80 M50,30 L48,80" stroke="{INK}" stroke-width="2" opacity=".35"/>'
        return 80, 96, o
    if name == "toilet_paper":
        return 64, 64, shadow(32, 58, 22) + f'<rect x="10" y="14" width="44" height="40" rx="8" fill="#fff" {S}/><ellipse cx="32" cy="16" rx="22" ry="8" fill="#F2F2F2" {S}/><ellipse cx="32" cy="16" rx="7" ry="3" fill="#C9B79C" stroke="{INK}" stroke-width="2"/><path d="M54,40 L58,58 L44,56" fill="#fff" {S}/>'
    if name == "spray_blue":
        return 48, 80, shadow(24, 74, 16) + f'<rect x="10" y="30" width="28" height="44" rx="8" fill="{WATER}" {S}/><path d="M14,30 L14,18 L34,18 L44,12 L44,22 L36,22 L34,30" fill="#fff" {S}/><rect x="14" y="44" width="20" height="14" rx="3" fill="#fff" stroke="{INK}" stroke-width="2"/>'
    if name == "desk":
        return 128, 112, shadow(64, 104, 54) + f'<rect x="20" y="52" width="8" height="50" fill="#8A6A4A" {S}/><rect x="100" y="52" width="8" height="50" fill="#8A6A4A" {S}/><path d="M8,40 L120,40 L112,58 L16,58 Z" fill="#E4A764" {S}/><rect x="16" y="58" width="96" height="10" fill="#C98B4E" {S}/><rect x="26" y="68" width="76" height="18" fill="#B07A45" stroke="{INK}" stroke-width="2.5"/>'
    if name == "chair":
        return 64, 112, shadow(32, 104, 24) + f'<rect x="12" y="62" width="6" height="40" fill="#8A6A4A" {S}/><rect x="46" y="62" width="6" height="40" fill="#8A6A4A" {S}/><rect x="10" y="14" width="44" height="36" rx="6" fill="#E4A764" {S}/><path d="M6,58 L58,58 L54,70 L10,70 Z" fill="#E4A764" {S}/>'
    if name == "blackboard":
        return 192, 128, f'<rect x="8" y="8" width="176" height="104" rx="6" fill="#B07A45" {S}/><rect x="18" y="18" width="156" height="84" rx="3" fill="#2F5D50" stroke="{INK}" stroke-width="2.5"/><text x="34" y="52" font-family="sans-serif" font-size="18" fill="#fff" opacity=".85">ABC  1+2=3</text><path d="M34,72 q20,-12 40,0 t40,0" stroke="#fff" stroke-width="3" fill="none" opacity=".6"/><rect x="20" y="112" width="152" height="8" fill="#B07A45" {S}/>'
    if name == "locker":
        o = shadow(64, 154, 56)
        for i, col in enumerate((RED, ORANGE, WATER)):
            x = 10 + i * 36
            o += f'<rect x="{x}" y="12" width="36" height="140" rx="4" fill="{col}" {S}/><path d="M{x+8},26 L{x+28},26 M{x+8},32 L{x+28},32" stroke="{INK}" stroke-width="2.5"/><rect x="{x+26}" y="76" width="4" height="14" rx="2" fill="#DDE3EA" stroke="{INK}" stroke-width="2"/>'
        return 128, 160, o
    if name == "cafe_table":
        return 192, 112, shadow(96, 104, 84) + f'<rect x="30" y="56" width="10" height="46" fill="#8A6A4A" {S}/><rect x="152" y="56" width="10" height="46" fill="#8A6A4A" {S}/><path d="M8,36 L184,36 L176,58 L16,58 Z" fill="#F2D2A9" {S}/><rect x="16" y="58" width="160" height="8" fill="#D9B07E" {S}/><rect x="40" y="26" width="32" height="12" rx="3" fill="#9AA3AE" {S}/><circle cx="56" cy="28" r="6" fill="#F28B82" stroke="{INK}" stroke-width="2"/>'
    if name == "cafe_chair":
        return 64, 96, shadow(32, 88, 24) + f'<rect x="14" y="54" width="6" height="32" fill="#8A6A4A" {S}/><rect x="44" y="54" width="6" height="32" fill="#8A6A4A" {S}/><path d="M8,44 L56,44 L52,58 L12,58 Z" fill="{RED}" {S}/><rect x="12" y="12" width="40" height="30" rx="8" fill="{RED}" {S}/>'
    if name == "food_stall":
        o = f'<rect x="12" y="60" width="232" height="90" rx="6" fill="#F2D2A9" {S}/><rect x="12" y="98" width="232" height="12" fill="#D9B07E" {S}/>'
        for i in range(8):
            o += f'<path d="M{8+i*30},56 L{38+i*30},56 L{38+i*30},62 Q{23+i*30},80 {8+i*30},62 Z" fill="{RED if i % 2 == 0 else "#fff"}" {S}/>'
        o += f'<rect x="4" y="30" width="248" height="28" rx="6" fill="{GREEN if False else "#57B87A"}" {S}/><text x="128" y="51" text-anchor="middle" font-family="sans-serif" font-weight="700" font-size="18" fill="#fff">โรงอาหาร</text>'
        return 256, 160, o
    if name == "slide":
        return 192, 160, shadow(96, 152, 84) + f'<rect x="20" y="20" width="10" height="130" fill="{WATER}" {S}/><rect x="60" y="20" width="10" height="130" fill="{WATER}" {S}/><path d="M24,40 L66,40 M24,70 L66,70 M24,100 L66,100 M24,130 L66,130" stroke="{INK}" stroke-width="5"/><path d="M14,20 L76,20 L76,30 L14,30 Z" fill="{RED}" {S}/><path d="M70,26 Q110,40 140,110 Q150,138 182,140 L182,150 Q140,150 128,118 Q104,50 70,40 Z" fill="{ORANGE}" {S}/>'
    if name == "bench":
        return 160, 96, shadow(80, 88, 70) + f'<rect x="20" y="60" width="8" height="28" fill="{NAVY}" {S}/><rect x="132" y="60" width="8" height="28" fill="{NAVY}" {S}/><rect x="10" y="20" width="140" height="14" rx="4" fill="#C98B4E" {S}/><rect x="10" y="38" width="140" height="10" rx="4" fill="#C98B4E" {S}/><rect x="6" y="52" width="148" height="12" rx="4" fill="#E4A764" {S}/>'
    if name == "tree":
        return 160, 192, shadow(80, 184, 50) + f'<path d="M70,184 L74,110 L86,110 L90,184 Z" fill="#9A6A42" {S}/><circle cx="80" cy="76" r="54" fill="#5DBB63" {S}/><circle cx="46" cy="100" r="30" fill="#5DBB63" {S}/><circle cx="116" cy="100" r="30" fill="#5DBB63" {S}/><circle cx="62" cy="56" r="14" fill="#8FD98F"/><circle cx="104" cy="70" r="9" fill="#8FD98F"/>'
    if name == "bush":
        return 128, 80, shadow(64, 74, 54) + f'<path d="M10,72 Q6,40 34,40 Q40,14 66,22 Q92,10 100,40 Q124,40 118,72 Z" fill="#5DBB63" {S}/><circle cx="44" cy="44" r="6" fill="#F28B82" stroke="{INK}" stroke-width="2"/><circle cx="86" cy="36" r="6" fill="#FFE27A" stroke="{INK}" stroke-width="2"/>'
    if name == "window":
        return 128, 128, f'<rect x="8" y="8" width="112" height="112" rx="6" fill="#FFFFFF" {S}/><rect x="18" y="18" width="42" height="92" fill="#BFE6FA" stroke="{INK}" stroke-width="2.5"/><rect x="68" y="18" width="42" height="92" fill="#BFE6FA" stroke="{INK}" stroke-width="2.5"/><path d="M24,60 L48,30 M74,70 L100,36" stroke="#fff" stroke-width="5" stroke-linecap="round"/>'
    if name == "boss_door":
        return 192, 192, f'<path d="M20,186 L20,80 Q20,14 96,14 Q172,14 172,80 L172,186 Z" fill="#6E6A7A" {S}/><path d="M40,186 L40,84 Q40,36 96,36 Q152,36 152,84 L152,186 Z" fill="#5B2E9A" {S}/><path d="M40,186 L40,84 Q40,36 96,36 Q152,36 152,84 L152,186 Z" fill="#B98BEA" fill-opacity=".35"/>' + enemy_face("boss", 96, 110, "angry", 1.2)
    if name == "pipe":
        return 192, 96, f'<rect x="0" y="30" width="192" height="30" fill="#8C96A6" {S}/><rect x="40" y="22" width="16" height="46" rx="3" fill="#6E7888" {S}/><rect x="136" y="22" width="16" height="46" rx="3" fill="#6E7888" {S}/><path d="M10,38 L180,38" stroke="#fff" stroke-width="3" opacity=".5"/>'
    return 64, 64, ""

GREEN = "#57B87A"
SCHOOL_PROPS = ["wet_sign", "cone", "bucket", "trash_bag", "trash_bin", "recycle_bin", "toilet_paper", "spray_blue",
                "desk", "chair", "blackboard", "locker", "window", "cafe_table", "cafe_chair", "food_stall",
                "slide", "bench", "tree", "bush", "boss_door", "pipe"]

FLOORS = {  # name -> (a, b) checker colours
    "school": ("#E9DCC5", "#DDCCB0"), "wood": ("#E4B57E", "#D9A66D"), "cafeteria": ("#F3E6CF", "#C9DCEF"),
    "grass": ("#8FD47A", "#83C96E"), "sand": ("#F2DDA6", "#EBD196"), "boiler": ("#9FA3AE", "#8F93A0"),
}

def floor_tile(a, b, kind):
    o = f'<rect width="64" height="64" fill="{a}"/>'
    if kind == "wood":
        o += "".join(f'<rect x="0" y="{y}" width="64" height="15" fill="{a if i % 2 else b}"/><path d="M{(i*23)%64},{y} l0,15" stroke="#B9824A" stroke-width="1.5"/>' for i, y in enumerate((0, 16, 32, 48)))
        o += '<path d="M0,16 H64 M0,32 H64 M0,48 H64" stroke="#B9824A" stroke-width="1.5"/>'
    elif kind in ("grass", "sand"):
        o += "".join(f'<circle cx="{x}" cy="{y}" r="2" fill="{b}"/>' for x, y in ((8, 10), (30, 22), (52, 8), (18, 44), (44, 52), (58, 34)))
    else:
        o += f'<rect width="32" height="32" fill="{b}"/><rect x="32" y="32" width="32" height="32" fill="{b}"/><path d="M0,0 H64 V64" fill="none" stroke="#fff" stroke-opacity=".35" stroke-width="2"/>'
    return o

def dirt_overlay(seed=0):
    rnd = [(13, 40, 14), (46, 18, 9), (50, 50, 6)]
    o = ""
    for i, (x, y, r) in enumerate(rnd):
        o += f'<path d="{blob(x, y, lambda a, r=r, i=i: (r + 3*math.sin(a*5+i+seed), r*0.7 + 2*math.sin(a*3+seed)))}" fill="#7A4A22" fill-opacity=".38"/>'
    o += f'<circle cx="30" cy="12" r="2.5" fill="#7A4A22" fill-opacity=".45"/><path d="M36,58 l8,-3" stroke="#6B6E86" stroke-width="2" opacity=".5"/>'
    return o

def decal(name):
    if name == "mud":
        return f'<path d="{blob(32, 34, lambda a: (24 + 5*math.sin(a*5), 17 + 4*math.sin(a*3+1)))}" fill="#8A5226" fill-opacity=".8" stroke="#5E3516" stroke-width="2"/><circle cx="56" cy="16" r="4" fill="#8A5226" fill-opacity=".8"/><circle cx="8" cy="54" r="3" fill="#8A5226" fill-opacity=".8"/>'
    if name == "slime":
        return f'<path d="{blob(32, 34, lambda a: (26 + 3*math.sin(a*6), 16 + 3*math.sin(a*4)))}" fill="#8B4FC8" fill-opacity=".75" stroke="#5B2E9A" stroke-width="2"/><path d="M18,30 Q24,24 32,26" stroke="#fff" stroke-width="3" fill="none" stroke-linecap="round" opacity=".7"/>'
    if name == "goo":   # green floor goo (boiler room)
        return f'<path d="{blob(32, 34, lambda a: (28 + 4*math.sin(a*4), 18 + 3*math.sin(a*5)))}" fill="#9BD13E" fill-opacity=".8" stroke="#5E8F1E" stroke-width="2"/>'
    if name == "trash":
        return (f'<path d="M10,40 L22,34 L26,46 L14,50 Z" fill="#fff" {S}/><path d="M36,20 q8,-6 14,2 q-4,8 -14,-2 Z" fill="#F2C14E" {S}/><rect x="36" y="42" width="16" height="10" rx="2" fill="{RED}" {S}/>')
    if name == "germ":
        return "".join(f'<circle cx="{x}" cy="{y}" r="{r}" fill="#7BC144" fill-opacity=".8" stroke="#4E8F2A" stroke-width="1.5"/>' for x, y, r in ((18, 22, 6), (40, 16, 4), (30, 42, 7), (50, 46, 4)))
    if name == "puddle":
        return f'<path d="{blob(32, 34, lambda a: (26 + 4*math.sin(a*4), 15 + 3*math.sin(a*3)))}" fill="#9ED8F5" fill-opacity=".75" stroke="{WATER}" stroke-width="2"/><path d="M20,32 Q26,26 34,28" stroke="#fff" stroke-width="3" fill="none" stroke-linecap="round"/>'
    return ""

def make_world():
    for it in ITEMS:
        sprite(f"Sprites/Items/item_{it}", 96, 96, item_icon(it), group="items")
    # bobbing coin spin sheet for pickups
    spin = [f'<g transform="translate(48,0) scale({f(max(0.12, abs(math.cos(i/8*math.pi))))},1) translate(-48,0)">{item_icon("coin")}</g>' for i in range(8)]
    sheet("Sprites/Items/item_coin_spin", 96, 96, spin, fps=12, loop=True, group="items")
    for name in SCHOOL_PROPS:
        w, h, body = school_prop(name)
        sprite(f"Sprites/Props/School/{name}", w, h, body, group="props", pivot=[0.5, 0.06])
    for name, (a, b) in FLOORS.items():
        sprite(f"Sprites/Environments/Floors/floor_{name}", 64, 64, floor_tile(a, b, name), group="env", tile=True)
        sprite(f"Sprites/Environments/Floors/floor_{name}_dirty", 64, 64,
               f'<g filter="url(#dull)">{floor_tile(a, b, name)}</g>{dirt_overlay(len(name))}'.replace('<g filter="url(#dull)">', DULL_FILTER + '<g filter="url(#dull)">'), group="env", tile=True)
    for d in ("mud", "slime", "goo", "trash", "germ", "puddle"):
        sprite(f"Sprites/Environments/Decals/decal_{d}", 64, 64, decal(d), group="env")

DULL_FILTER = '<defs><filter id="dull"><feColorMatrix type="saturate" values="0.55"/><feComponentTransfer><feFuncR type="linear" slope="0.88"/><feFuncG type="linear" slope="0.84"/><feFuncB type="linear" slope="0.8"/></feComponentTransfer></filter></defs>'

# ---------------------------------------------------------------- concept: stage backdrops (clean / dirty)
def backdrop(name):
    W, H = 384, 216
    sky = f'<defs><linearGradient id="sky" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#8EC9F5"/><stop offset="1" stop-color="#DDEBFA"/></linearGradient></defs>'
    if name == "school":
        o = sky + f'<rect width="{W}" height="{H}" fill="url(#sky)"/><circle cx="60" cy="40" r="16" fill="#fff" opacity=".7"/><circle cx="80" cy="40" r="20" fill="#fff" opacity=".7"/>'
        o += f'<rect x="0" y="170" width="{W}" height="46" fill="#8FD47A"/><rect x="150" y="160" width="84" height="56" fill="#E9DCC5"/>'
        o += f'<rect x="86" y="60" width="212" height="112" fill="#F2D29B" {S}/><path d="M76,64 L192,26 L308,64 Z" fill="#5B7FD6" {S}/><circle cx="192" cy="52" r="12" fill="#fff" {S}/><path d="M192,52 L192,45 M192,52 L197,55" stroke="{INK}" stroke-width="2"/>'
        for r in range(2):
            for c in range(6):
                if c in (2, 3) and r == 1: continue
                o += f'<rect x="{100+c*32}" y="{78+r*36}" width="22" height="22" rx="3" fill="#BFE6FA" stroke="{INK}" stroke-width="2.5"/>'
        o += f'<rect x="170" y="128" width="44" height="44" rx="4" fill="#8A5A3C" {S}/>'
        o += f'<g transform="translate(6,60) scale(.55)">{school_prop("tree")[2]}</g><g transform="translate(296,60) scale(.55)">{school_prop("tree")[2]}</g>'
        return W, H, o
    if name in ("classroom", "cafeteria", "boiler"):
        wall, wall2, floor = {"classroom": ("#F6E3C4", "#EAD0A6", "wood"), "cafeteria": ("#FCEBD5", "#F2D2A9", "cafeteria"), "boiler": ("#8C8A98", "#77758A", "boiler")}[name]
        a, b = FLOORS[floor]
        o = f'<rect width="{W}" height="{H}" fill="{wall}"/><rect y="70" width="{W}" height="10" fill="{wall2}"/>'
        o += f'<path d="M0,96 L{W},96 L{W},{H} L0,{H} Z" fill="{a}"/>'
        for i in range(13):
            o += f'<path d="M{i*32},96 L{-60+i*40},{H}" stroke="{b}" stroke-width="3"/>'
        for y in (120, 150, 186):
            o += f'<path d="M0,{y} H{W}" stroke="{b}" stroke-width="3"/>'
        o += f'<path d="M0,96 H{W}" stroke="{INK}" stroke-width="3"/>'
        if name == "classroom":
            o += f'<g transform="translate(120,6) scale(.75)">{school_prop("blackboard")[2]}</g>'
            o += f'<g transform="translate(14,4) scale(.6)">{school_prop("window")[2]}</g><g transform="translate(292,4) scale(.6)">{school_prop("window")[2]}</g>'
            for x, y in ((40, 104), (150, 104), (260, 104), (70, 146), (190, 146), (300, 146)):
                o += f'<g transform="translate({x},{y}) scale(.5)">{school_prop("desk")[2]}</g>'
        elif name == "cafeteria":
            o += f'<g transform="translate(100,-6) scale(.72)">{school_prop("food_stall")[2]}</g>'
            for x, y in ((24, 120), (220, 120), (110, 160)):
                o += f'<g transform="translate({x},{y}) scale(.75)">{school_prop("cafe_table")[2]}</g>'
        else:
            o += f'<g transform="translate(0,10) scale(1)">{school_prop("pipe")[2]}</g><g transform="translate(220,24) scale(.9)">{school_prop("pipe")[2]}</g>'
            o += f'<g transform="translate(148,-8) scale(.5)">{school_prop("boss_door")[2]}</g>'
            o += f'<g transform="translate(150,140) scale(1.6)">{decal("goo")}</g>'
        return W, H, o
    if name == "playground":
        o = sky + f'<rect width="{W}" height="{H}" fill="url(#sky)"/><rect y="110" width="{W}" height="106" fill="#8FD47A"/><path d="{blob(220, 170, lambda a: (120, 34))}" fill="#F2DDA6" stroke="#D9BF7E" stroke-width="3"/>'
        o += f'<g transform="translate(-10,40) scale(.6)">{school_prop("tree")[2]}</g><g transform="translate(300,36) scale(.6)">{school_prop("tree")[2]}</g>'
        o += f'<g transform="translate(150,60) scale(.75)">{school_prop("slide")[2]}</g><g transform="translate(40,150) scale(.55)">{school_prop("bench")[2]}</g><g transform="translate(110,96) scale(.4)">{school_prop("bush")[2]}</g>'
        return W, H, o
    return W, H, ""

def make_backdrops():
    for name in ("school", "classroom", "cafeteria", "playground", "boiler"):
        W, H, o = backdrop(name)
        sprite(f"Sprites/Environments/Backdrops/bg_{name}", W, H, o, group="env")
        dirt = ""
        for i, (x, y, d) in enumerate(((60, 150, "mud"), (210, 170, "slime"), (300, 130, "trash"), (130, 120, "mud"), (330, 190, "germ"), (20, 190, "slime"))):
            dirt += f'<g transform="translate({x-32},{y-32}) scale(1.2)">{decal(d)}</g>'
        sprite(f"Sprites/Environments/Backdrops/bg_{name}_dirty", W, H, DULL_FILTER + f'<g filter="url(#dull)">{o}</g>' + dirt, group="env")

# ---------------------------------------------------------------- concept: extra FX
def make_fx_concept():
    fr = []
    for i in range(7):  # water splash
        p = i / 6
        g = f'<ellipse cx="64" cy="100" rx="{f(14+30*p)}" ry="{f(5+8*p)}" fill="#9ED8F5" fill-opacity="{f(0.9*(1-p))}" stroke="{WATER}" stroke-width="2.5" stroke-opacity="{f(1-p)}"/>'
        for j in range(7):
            a = math.radians(-160 + j * 23)
            r = 14 + 50 * p
            x, y = 64 + math.cos(a) * r, 96 + math.sin(a) * r * 1.1 + 60 * p * p
            g += f'<ellipse cx="{f(x)}" cy="{f(y)}" rx="{f(8*(1-p*0.5))}" ry="{f(11*(1-p*0.5))}" fill="#9ED8F5" stroke="{WATER}" stroke-width="2" opacity="{f(1-p**2)}"/>'
        fr.append(g)
    sheet("Sprites/FX/fx_splash", 128, 128, fr, fps=16, loop=False, group="fx")
    fr = []
    for i in range(6):  # golden swipe arc (กวาด)
        p = i / 5
        a0, a1 = -200 + 150 * p, -200 + 150 * min(1, p + 0.45)
        def pt(a, r): return f"{f(64+math.cos(math.radians(a))*r)},{f(70+math.sin(math.radians(a))*r*0.7)}"
        d = f"M{pt(a0,50)} A50,35 0 0 1 {pt(a1,50)} L{pt(a1,34)} A34,24 0 0 0 {pt(a0,34)} Z"
        fr.append(f'<path d="{d}" fill="#FFE27A" fill-opacity="{f(1-p*0.7)}" stroke="{GOLD_D}" stroke-width="2.5" stroke-opacity="{f(1-p)}"/>' + (sparkle(*map(float, pt(a1, 44).split(",")), 7, "#fff", 2) if p < 0.9 else ""))
    sheet("Sprites/FX/fx_swipe", 128, 128, fr, fps=18, loop=False, group="fx")
    fr = []
    for i in range(6):  # spray mist cone (สเปรย์)
        g = ""
        for j in range(14):
            t = ((j * 0.137 + i / 6) % 1.0)
            x = 10 + t * 110
            y = 64 + math.sin(j * 2.3) * t * 30
            g += f'<circle cx="{f(x)}" cy="{f(y)}" r="{f(2+t*6)}" fill="#BFE6FA" fill-opacity="{f(0.9-t*0.7)}" stroke="{WATER}" stroke-width="1.5" stroke-opacity="{f(1-t)}"/>'
        fr.append(g)
    sheet("Sprites/FX/fx_spray_mist", 128, 128, fr, fps=14, loop=True, group="fx")
    fr = []
    for i in range(5):  # hit burst
        p = i / 4
        g = f'<path d="{star(64, 64, 16+30*p, 8+12*p, 8)}" fill="#fff" stroke="{GOLD_D}" stroke-width="3" opacity="{f(1-p**1.5)}"/>'
        for j in range(5):
            a = j / 5 * 2 * math.pi
            g += f'<path d="{star(64+math.cos(a)*(14+40*p), 64+math.sin(a)*(14+40*p), 7*(1-p)+2, 3*(1-p)+1)}" fill="{GOLD}" stroke="{INK}" stroke-width="1.5"/>'
        fr.append(g)
    sheet("Sprites/FX/fx_hit", 128, 128, fr, fps=16, loop=False, group="fx")
    fr = []
    for i in range(8):  # tornado (skill กวาด)
        g = ""
        for j in range(6):
            y = 108 - j * 16
            w = 14 + j * 9
            off = math.sin(i / 8 * 2 * math.pi + j) * 6
            g += f'<ellipse cx="{f(64+off)}" cy="{y}" rx="{w}" ry="{f(5+j)}" fill="none" stroke="#9ED8F5" stroke-width="5" opacity=".85"/><ellipse cx="{f(64+off)}" cy="{y}" rx="{w}" ry="{f(5+j)}" fill="none" stroke="#fff" stroke-width="2"/>'
        fr.append(g + sparkle(30 + i * 8 % 70, 30, 5, "#fff", 1.5))
    sheet("Sprites/FX/fx_tornado", 128, 128, fr, fps=14, loop=True, group="fx")
    fr = []
    for i in range(8):  # bubble dome (skill ฟอง)
        p = i / 7
        r = 30 + 22 * min(1, p * 1.6)
        g = f'<path d="M{f(64-r)},104 A{f(r)},{f(r)} 0 0 1 {f(64+r)},104 Z" fill="#DFF4FF" fill-opacity=".45" stroke="{WATER}" stroke-width="3"/>'
        g += f'<path d="M{f(64-r*0.7)},{f(104-r*0.4)} Q{f(64-r*0.5)},{f(104-r*0.85)} {f(64-r*0.1)},{f(104-r*0.95)}" fill="none" stroke="#fff" stroke-width="5" stroke-linecap="round"/>'
        g += "".join(f'<circle cx="{f(64+math.cos(a)*r)}" cy="{f(104-abs(math.sin(a))*r)}" r="4" fill="#fff" stroke="{WATER}" stroke-width="1.5"/>' for a in (0.4 + p, 1.6 + p, 2.6 + p))
        fr.append(g)
    sheet("Sprites/FX/fx_bubble_dome", 128, 128, fr, fps=12, loop=False, group="fx")
    fr = []
    for i in range(8):  # time freeze (skill ถู)
        p = i / 7
        g = f'<circle cx="64" cy="64" r="{f(20+30*p)}" fill="none" stroke="#9ED8F5" stroke-width="{f(8*(1-p)+1)}" opacity="{f(1-p*0.6)}"/>'
        g += f'<g transform="translate(16,16)">{item_icon("stopwatch")}</g><circle cx="64" cy="70" r="25" fill="#BFE6FA" fill-opacity="{f(0.6*p)}"/>'
        g += sparkle(30, 26, 6 * p + 1, "#fff", 1.5) + sparkle(100, 96, 5 * p + 1, "#fff", 1.5)
        fr.append(g)
    sheet("Sprites/FX/fx_time_freeze", 128, 128, fr, fps=12, loop=False, group="fx")
    fr = []
    for i in range(8):  # clean wave (round cleared)
        p = i / 7
        x = -20 + 170 * p
        fr.append(f'<rect x="{f(x-30)}" y="0" width="30" height="128" fill="#fff" fill-opacity=".6"/>' + sparkle(x - 10, 30, 9, "#fff", 2) + sparkle(x - 22, 90, 7, GOLD, 2) + sparkle(x, 64, 6, "#fff", 2))
    sheet("Sprites/FX/fx_clean_wave", 128, 128, fr, fps=14, loop=False, group="fx")

# ---------------------------------------------------------------- concept: UI
SKILL_ICON = {
    "sweep":  lambda: f'<path d="M30,70 Q48,20 82,30" fill="none" stroke="{GOLD}" stroke-width="10" stroke-linecap="round"/><path d="M26,58 L42,74 M60,64 L74,46" stroke="#fff" stroke-width="5" stroke-linecap="round"/><path d="M64,22 L80,30 L70,42" fill="none" stroke="{INK}" stroke-width="4" stroke-linejoin="round" stroke-linecap="round"/>',
    "bubble": lambda: f'<path d="M20,74 A28,28 0 0 1 76,74 Z" fill="#DFF4FF" stroke="{INK}" stroke-width="3.5"/><circle cx="36" cy="40" r="7" fill="#fff" stroke="{WATER}" stroke-width="2"/><circle cx="62" cy="34" r="5" fill="#fff" stroke="{WATER}" stroke-width="2"/>',
    "spray":  lambda: f'<circle cx="48" cy="50" r="26" fill="none" stroke="#fff" stroke-width="5"/><circle cx="48" cy="50" r="12" fill="none" stroke="#fff" stroke-width="5"/><path d="M48,14 L48,30 M48,70 L48,86 M12,50 L28,50 M68,50 L84,50" stroke="#fff" stroke-width="5" stroke-linecap="round"/>',
    "press":  lambda: f'<g transform="translate(6,4) scale(.88)">{item_icon("stopwatch")}</g><path d="M66,64 l10,10 M76,64 l-10,10" stroke="#BFE6FA" stroke-width="4" stroke-linecap="round"/>',
}

def round_button(col, inner, r=40, sz=96):
    c = sz / 2
    return (f'<circle cx="{c}" cy="{c+4}" r="{r}" fill="{INK}"/><circle cx="{c}" cy="{c}" r="{r}" fill="{col}" {S}/>'
            f'<path d="M{c-r*0.6},{c-r*0.45} Q{c-r*0.3},{c-r*0.8} {c+r*0.2},{c-r*0.8}" stroke="#fff" stroke-width="5" fill="none" stroke-linecap="round" opacity=".6"/>' + inner)

def make_ui_concept():
    # BOB logo: red ribbon, yellow/blue badge, crossed broom + mop
    o = f'<circle cx="256" cy="150" r="118" fill="{WATER}" stroke="{INK}" stroke-width="8"/><circle cx="256" cy="150" r="96" fill="{GOLD}" stroke="{INK}" stroke-width="6"/>'
    o += f'<g transform="translate(150,20) rotate(-35,60,80) scale(1.6)">{held_prop("broom", 30, 60, 0)}</g><g transform="translate(330,10) rotate(35,30,80) scale(1.6)">{held_prop("mop", 30, 60, 0)}</g>'
    o += f'<path d="M30,150 L90,132 L90,232 L30,250 L56,200 Z M482,150 L422,132 L422,232 L482,250 L456,200 Z" fill="#C81E33" stroke="{INK}" stroke-width="7" stroke-linejoin="round"/>'
    o += f'<path d="M70,128 Q256,96 442,128 L442,236 Q256,206 70,236 Z" fill="{RED}" stroke="{INK}" stroke-width="8" stroke-linejoin="round"/>'
    o += f'<text x="256" y="208" text-anchor="middle" font-family="Arial Rounded MT Bold, Mitr, Kanit, sans-serif" font-weight="900" font-size="104" fill="#FEEECF" stroke="{INK}" stroke-width="7" paint-order="stroke">BOB</text>'
    o += f'<rect x="96" y="246" width="320" height="50" rx="16" fill="{NAVY}" stroke="{INK}" stroke-width="6"/><text x="256" y="281" text-anchor="middle" font-family="Arial Rounded MT Bold, Mitr, sans-serif" font-weight="800" font-size="27" fill="#fff" textLength="290" lengthAdjust="spacingAndGlyphs">BATTLE OF THE BROOM</text>'
    sprite("Sprites/UI/logo_bob", 512, 320, o, group="ui")
    # big glossy buttons (9-slice)
    for name, top, bot in (("ui_button_play", "#FFE27A", "#F5A623"), ("ui_button_blue", "#7CC0FF", "#2F7FD8")):
        o = (f'<defs><linearGradient id="g{name}" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="{top}"/><stop offset="1" stop-color="{bot}"/></linearGradient></defs>'
             f'<rect x="4" y="10" width="120" height="44" rx="18" fill="{INK}"/><rect x="4" y="4" width="120" height="44" rx="18" fill="url(#g{name})" {S}/><path d="M20,14 L80,14" stroke="#fff" stroke-width="5" stroke-linecap="round" opacity=".7"/>')
        sprite(f"Sprites/UI/{name}", 128, 58, o, group="ui", border=[22 * SCALE] * 4)
    sprite("Sprites/UI/ui_button_skill", 128, 128, round_button(WATER, f'<g transform="translate(16,14)">{SKILL_ICON["bubble"]()}</g>', 56, 128), group="ui")
    sprite("Sprites/UI/ui_button_round", 96, 96, round_button("#FEEECF", "", 40), group="ui")
    sprite("Sprites/UI/ui_portrait_frame", 96, 96, f'<circle cx="48" cy="48" r="42" fill="none" stroke="{INK}" stroke-width="9"/><circle cx="48" cy="48" r="42" fill="none" stroke="#fff" stroke-width="4"/>', group="ui")
    sprite("Sprites/UI/ui_ribbon", 192, 48, f'<path d="M4,8 L24,8 L24,40 L4,40 L14,24 Z M188,8 L168,8 L168,40 L188,40 L178,24 Z" fill="#C81E33" {S}/><rect x="20" y="4" width="152" height="36" rx="6" fill="{RED}" {S}/>', group="ui", border=[28 * SCALE, 8 * SCALE, 28 * SCALE, 8 * SCALE])
    sprite("Sprites/UI/ui_panel_blue", 96, 96, f'<rect x="3" y="3" width="90" height="90" rx="18" fill="#EAF3FD" {S}/><rect x="3" y="3" width="90" height="22" rx="12" fill="{NAVY}" {S}/>', group="ui", border=[26 * SCALE, 20 * SCALE, 20 * SCALE, 20 * SCALE])
    for name, body in (("icon_heart", item_icon("heart")), ("icon_coin", item_icon("coin")), ("icon_gem", item_icon("gem"))):
        sprite(f"Sprites/UI/{name}", 96, 96, body, group="ui")
    # per-hero: skill icon, character-select card
    bg = {"sweep": "#FFC94A", "bubble": "#B79CF0", "spray": "#5BA8F5", "press": "#6BD39A"}  # card colours as in the concept Character Select row
    for cid in CHARS:
        sprite(f"Sprites/UI/Skills/icon_skill_{cid}", 96, 96, round_button(CHARS[cid]["col"], SKILL_ICON[cid](), 40), group="ui", owner=cid)
        card = (f'<rect x="4" y="8" width="120" height="152" rx="16" fill="{INK}"/><rect x="4" y="4" width="120" height="152" rx="16" fill="{bg[cid]}" {S}/>'
                f'<path d="M14,20 L60,14" stroke="#fff" stroke-width="5" stroke-linecap="round" opacity=".6"/>'
                f'<g transform="translate(-16,2) scale(1.25)">{character(cid, expr="happy")}</g>')
        sprite(f"Sprites/UI/CharacterSelect/card_{cid}", 128, 164, card, group="ui", owner=cid)
    # stage cards use the clean backdrops
    for name in ("school", "classroom", "cafeteria", "playground", "boiler"):
        W, H, o = backdrop(name)
        card = (f'<clipPath id="c{name}"><rect x="8" y="8" width="176" height="104" rx="10"/></clipPath>'
                f'<rect x="4" y="8" width="184" height="140" rx="14" fill="{INK}"/><rect x="4" y="4" width="184" height="140" rx="14" fill="#FEEECF" {S}/>'
                f'<g clip-path="url(#c{name})"><g transform="translate(8,8) scale({f(176/W)},{f(104/H*1.0)})">{o}</g></g><rect x="8" y="8" width="176" height="104" rx="10" fill="none" stroke="{INK}" stroke-width="3"/>'
                + "".join(f'<path d="{star(66+i*30, 128, 11, 5)}" fill="#E8DFC4" stroke="{INK}" stroke-width="2.5"/>' for i in range(3)))
        sprite(f"Sprites/UI/StageSelect/stage_{name}", 192, 152, card, group="ui")

if __name__ == "__main__":
    make_characters(); make_enemies(); make_parts(); make_props(); make_ui(); make_fx()
    make_world(); make_backdrops(); make_fx_concept(); make_ui_concept()
    with open(os.path.join(ART, "bob-art-manifest.json"), "w") as fh:
        json.dump(manifest, fh, indent=1)
    print(len(manifest["sprites"]), "sprites/sheets written to", SRC)

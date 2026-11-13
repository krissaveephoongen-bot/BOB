#!/usr/bin/env python3
"""Rebuild the tables in 23_Asset_Master_Table.md from 23_Asset_Master_Table.csv.

The CSV is the source of truth. Edit it (Excel / Sheets / text), then run:
    python3 art/docs/tools/asset_table.py
Everything between the <!-- asset-table:start --> and <!-- asset-table:end -->
markers in the .md is regenerated; the rest of the document is left alone.
"""
import csv, pathlib, re

DOCS = pathlib.Path(__file__).resolve().parent.parent
CSV = DOCS / "23_Asset_Master_Table.csv"
MD = DOCS / "23_Asset_Master_Table.md"

# Room order = order of the game (GDD 20). Codes used in the "rooms" column.
ROOMS = [
    ("L1", "ห้องเช่า", "บทบ้าน ด่าน 1 (P0)"),
    ("L2", "คอนโด", "บทบ้าน ด่าน 2"),
    ("L3", "ร้านกาแฟ", "บทบ้าน ด่าน 3 (ยังไม่มีผัง)"),
    ("L3b", "ครัวร้านอาหาร", "บทบ้าน ด่านบอส (ข้อเสนอ)"),
    ("L4", "บ้านไม้", "บท 4 (ยังไม่มีผัง)"),
    ("L5a", "ห้องเรียน", "บท 5 โรงเรียน"),
    ("L5b", "โรงอาหาร", "บท 5 โรงเรียน"),
    ("L5c", "สนามเด็กเล่น", "บท 5 โรงเรียน"),
    ("L5d", "ห้องใต้ดิน", "บท 5 โรงเรียน (บอส)"),
    ("L6", "ศาลา", "บทหลัง (ยังไม่มีผัง)"),
    ("L7", "ตลาด", "บทหลัง (ยังไม่มีผัง)"),
]
CODES = [r[0] for r in ROOMS]
GROUPS = [
    ("core", "ชั้น 1 · Core (ทุกห้องหรือหลายบท ใช้ซ้ำมากที่สุด)"),
    ("home", "ชั้น 2 · ชุดบ้าน (home kit)"),
    ("kitchen", "ชั้น 2 · ชุดครัวโมดูล 0.6 m (kitchen kit)"),
    ("shop", "ชั้น 2 · ชุดร้าน (shop kit)"),
    ("school", "ชั้น 2 · ชุดโรงเรียน (school kit)"),
    ("outdoor", "ชั้น 2 · ชุดกลางแจ้ง (outdoor kit)"),
    ("unique", "ชั้น 3 · ของเด่นประจำห้อง (ของหนัก T13)"),
    ("minigame", "ของในการ์ดมินิเกม 3D (Krits เลือก 13 พ.ย. 2026)"),
    ("prop", "พร็อพเล็กและของในมินิเกม"),
]
PRI = ["AA-0", "AA-1", "AA-2"]


def rooms_of(r):
    return CODES if r["rooms"].strip() == "ALL" else r["rooms"].split()


def credits(r):
    try:
        return int(r["tripo_credits"])
    except ValueError:
        return 0


def pri(r):
    return r["priority"].split()[0]


def cell(s):
    return s.replace("|", "\\|")


def build(rows):
    out = []
    out.append("### ตาราง asset ในฉาก 3D (สร้างจาก CSV)\n")
    out.append("สาย A = สคริปต์ Blender (0 เครดิต) · B = Tripo + `bob_prop_cleanup.py` (~25 เครดิตต่อชิ้น) · C = ชุด CC0 · "
               "สถานะ: ✓ มีไฟล์แล้ว · PH = กล่องแทนใน `bob_room.py` · คิว Tripo = รอเธรดสร้าง · – = ยังไม่เริ่ม\n")
    for g, title in GROUPS:
        gr = [r for r in rows if r["group"] == g]
        if not gr:
            continue
        out.append(f"\n#### {title}\n")
        out.append("| รหัส | ของ | file id (`SM_prop_<id>`) | ห้อง | จุดงาน | ขนาด ก×ล×ส (m) | tris | สาย | เครดิต | ลำดับ | สถานะ | หมายเหตุ |")
        out.append("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |")
        for r in gr:
            rooms = "ทุกห้อง" if r["rooms"] == "ALL" else r["rooms"]
            out.append("| " + " | ".join(cell(x) for x in [
                r["id"], r["name_th"], f"`{r['file_id']}`", rooms, r["station"], r["size_m"],
                r["budget_tris"], r["line"], r["tripo_credits"], r["priority"], r["status"], r["note"]]) + " |")

    # Room bill of materials: what each room needs, and what is new when rooms are built in game order.
    out.append("\n### ของที่แต่ละห้องต้องใช้ (ทำตามลำดับห้อง)\n")
    out.append("\"ใหม่\" = ชิ้นที่ห้องก่อนหน้ายังไม่ได้สร้าง ส่วนที่เหลือใช้ซ้ำฟรี เครดิตนับเฉพาะชิ้นใหม่สาย B\n")
    out.append("| ห้อง | ชื่อ | บท | ใช้ทั้งหมด | ใหม่ | ใช้ซ้ำ | เครดิต Tripo ชิ้นใหม่ | ชิ้นใหม่ |")
    out.append("| --- | --- | --- | --- | --- | --- | --- | --- |")
    built = set()
    for code, name, chap in ROOMS:
        need = [r for r in rows if code in rooms_of(r)]
        new = [r for r in need if r["id"] not in built and r["status"] not in ("✓", "✓ mesh")]
        reuse = len(need) - len(new)
        cr = sum(credits(r) for r in new)
        built.update(r["id"] for r in need)
        out.append(f"| {code} | {name} | {chap} | {len(need)} | {len(new)} | {reuse} | {cr} | "
                   + ", ".join(r["id"] for r in new) + " |")

    out.append("\n### สรุปตามลำดับความสำคัญ\n")
    out.append("| ลำดับ | จำนวนแถว | สาย A | สาย B | สาย C | เสร็จแล้ว | เครดิต Tripo ที่ยังต้องใช้ |")
    out.append("| --- | --- | --- | --- | --- | --- | --- |")
    tot = 0
    for p in PRI:
        pr = [r for r in rows if pri(r) == p]
        a = sum(1 for r in pr if r["line"].startswith("A"))
        b = sum(1 for r in pr if r["line"].startswith("B"))
        c = sum(1 for r in pr if r["line"].startswith("C"))
        done = sum(1 for r in pr if r["status"].startswith("✓"))
        cr = sum(credits(r) for r in pr if not r["status"].startswith("✓"))
        tot += cr
        out.append(f"| {p} | {len(pr)} | {a} | {b} | {c} | {done} | {cr} |")
    out.append(f"| **รวม** | {len(rows)} | | | | | **{tot}** |")
    return "\n".join(out) + "\n"


def main():
    rows = list(csv.DictReader(open(CSV, encoding="utf-8-sig")))
    block = build(rows)
    md = MD.read_text(encoding="utf-8")
    new = re.sub(r"(<!-- asset-table:start -->\n).*?(<!-- asset-table:end -->)",
                 lambda m: m.group(1) + block + m.group(2), md, flags=re.S)
    if new == md and block not in md:
        raise SystemExit("markers not found in " + str(MD))
    MD.write_text(new, encoding="utf-8")
    print(f"{len(rows)} rows -> {MD.name}")


if __name__ == "__main__":
    main()

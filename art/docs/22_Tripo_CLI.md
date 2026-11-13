# 22 · Tripo CLI: สร้างโมเดล 3D จาก prompt หรือภาพ (ตั้งค่า 13 พ.ย. 2026)

ใช้คู่กับแนวทาง "ไม่มี artist" (ภาพอ้างอิงจาก Leonardo → image-to-3D → เก็บงานใน Blender → Unity)
เอกสารทางการ: <https://developers.tripo3d.com/en/docs/cli> (ระบุคำติดตั้ง `npm install -g tripo-cli` ตรงกับแพ็กเกจบน npm)

## 1. สถานะปัจจุบัน

| รายการ | ผล |
| --- | --- |
| แพ็กเกจ | `tripo-cli` 0.11.2 (npm, MIT, คำสั่ง `tripo`) ต้องใช้ Node 20 ขึ้นไป |
| คีย์ | อ่านจากตัวแปร `TRIPO_API_KEY` เท่านั้น ไม่มีคีย์ในไฟล์หรือใน repo (`~/.tripo/config.json` เก็บแค่ analytics id) |
| ภูมิภาค | `TRIPO_REGION=ov` (สากล, `https://openapi.tripo3d.ai`) |
| `tripo doctor` | ผ่านทุกข้อสำคัญ เหลือแค่เตือนว่าเครดิตเป็น 0 |
| เครดิต | **0** (`tripo balance` → `{"balance":0,"frozen":0}`) ต้องเติมก่อนสร้างโมเดลจริง (`tripo topup` เปิดหน้าเติมเงิน) |
| เครือข่าย cloud | เพิ่ม `openapi.tripo3d.ai` ใน allowlist ของ environment แล้ว (V3 API ใช้โดเมนนี้ ไม่ใช่ `api.tripo3d.ai` แบบ V2) |

## 2. ติดตั้งและตั้งค่า

```bash
npm install -g tripo-cli
tripo --version
```

ตั้งคีย์เป็นตัวแปรสภาพแวดล้อม **ห้ามพิมพ์คีย์ลงแชต ไฟล์ หรือ commit** คีย์ที่เคยหลุดให้ลบทิ้งในหน้า API Keys ของ Tripo แล้วสร้างใหม่

- Cloud (Claude): Project settings > Cloud environment > ตัวแปร `TRIPO_API_KEY`
- Windows (PowerShell, เฉพาะผู้ใช้):
  ```powershell
  setx TRIPO_API_KEY "<คีย์ใหม่>"
  setx TRIPO_REGION "ov"
  ```
  แล้วเปิดเทอร์มินัลใหม่ หรือใช้ `tripo login` (เปิดเบราว์เซอร์ล็อกอิน เก็บคีย์ใน `%USERPROFILE%\.tripo` สิทธิ์ 0600)

## 3. คำสั่งที่ไม่เสียเครดิต

| คำสั่ง | ใช้ทำอะไร |
| --- | --- |
| `tripo doctor` | เช็ก Node, คีย์, การเชื่อมต่อ API และยอดเครดิต (ไม่ส่งงาน) |
| `tripo balance` | ยอดเครดิตคงเหลือ / ที่ถูกกันไว้ |
| `tripo usage` | งานที่ใช้เครดิตล่าสุด |
| `tripo models` | รายชื่อโมเดล (ไม่เรียก API) |
| `tripo make "<prompt>" --dry-run` | ดูแผนงาน payload และประมาณการเครดิต โดยไม่ส่งงาน |

## 4. สูตรใช้กับ BOB

ใส่ `--dry-run` ก่อนเสมอเพื่อดูราคา แล้วค่อยรันจริง

```bash
# prop จากข้อความ: v3.1 + PBR + texture ละเอียด, preset มือถือ (face_limit 15000 แล้วแปลงเป็น FBX)
tripo make "a red plastic mop bucket, chunky toy-like, game prop" \
  --model v3.1 -p texture=true -p pbr=true -p texture_quality=detailed \
  --for game-mobile -o art/incoming/tripo/bucket --dry-run

# prop จากภาพอ้างอิง Leonardo (พื้นหลังโปร่ง/ขาวได้ผลดีที่สุด)
tripo make leonardo_raw/props/sponge_0.png --model v3.1 --for game-mobile -o art/incoming/tripo/sponge

# ภาพหน้า + หลัง ให้รูปทรงแม่นขึ้น
tripo make front.png back.png --for game-mobile

# ต่องานจากผลล่าสุด: ทำ rig แล้วแปลงเป็น FBX (ใช้กับฮีโร่/ศัตรู)
tripo make @last --then rig,convert:fbx
```

ประมาณการจาก `--dry-run` (13 พ.ย. 2026, ราคาประกาศ ไม่ใช่ยอดหักจริง):

| ตั้งค่า | เครดิตโดยประมาณ |
| --- | --- |
| text-to-model, โมเดล P2 (ค่าเริ่มต้น), ไม่มี texture | 110 |
| text-to-model, v3.1 + texture detailed + PBR (`--for game-mobile`) | 30 + ค่าแปลง FBX (ไม่รวมในประมาณการ) |

ข้อสังเกต: ค่าเริ่มต้นของ CLI คือ P2 ซึ่งแพงกว่า ให้ระบุ `--model v3.1` สำหรับ prop ทั่วไป

ไฟล์ที่ได้ลง `art/incoming/tripo/` ก่อน แล้วเก็บงานตาม `03_Unity_Spec.md` (poly budget, pivot, สเกล, ตั้งชื่อ) ก่อนย้ายเข้า Unity ห้าม commit ไฟล์ดิบขนาดใหญ่โดยไม่ผ่าน Git LFS

## 5. เรียก V3 API ตรงด้วย Python (แทน CLI)

endpoint เดียวกับที่ CLI ใช้ คีย์มาจากตัวแปรสภาพแวดล้อม:

```python
import os, requests

BASE = "https://openapi.tripo3d.ai/v3"
headers = {"Authorization": f"Bearer {os.environ['TRIPO_API_KEY']}"}

# text-to-model
r = requests.post(f"{BASE}/generation/text-to-model", headers=headers, json={
    "prompt": "a red plastic mop bucket, chunky toy-like, game prop",
    "model": "v3.1-20260211",
    "texture": True, "pbr": True, "texture_quality": "detailed",
})
print(r.json())  # ได้ task id แล้วดูสถานะที่ GET {BASE}/tasks/<task_id>

# image-to-model: เปลี่ยน endpoint เป็น /generation/image-to-model และส่ง "input": "<URL ภาพ>"
```

ยอดเครดิต: `GET {BASE}/account/balance`

## 6. ปัญหาที่เจอ

- `tripo doctor` ขึ้น `api reachability ... fetch failed` ใน cloud: โดเมน `openapi.tripo3d.ai` ไม่อยู่ใน allowlist (แก้แล้ว 13 พ.ย. 2026)
- การตั้ง `TRIPO_API_BASE_URL=https://api.tripo3d.ai` ใช้ไม่ได้ โดเมนเก่าไม่มี path `/v3` (ได้ 404)

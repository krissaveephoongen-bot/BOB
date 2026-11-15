# BOB online — ขึ้น cloud แบบ free plan (16 พ.ย. 2026)

เซิร์ฟเวอร์เกมคือ **Nakama 3.22 + Postgres** (`server/modules/index.js` เป็น entrypoint, ห้อง `bob_coop` = ตรรกะใน `server/match.js`)
Vercel/Netlify **host ไม่ได้** เพราะเป็น process ที่ถือ WebSocket + match loop ตลอดเวลา ไม่ใช่ serverless
โฟลเดอร์นี้จึงเป็นชุด deploy ที่ยกสแตกเดิมขึ้น host ที่รัน container ได้ และฐานข้อมูลแยกของตัวเอง (`bob_pg` + database `nakama`)

## ทางฟรีที่ใช้ได้จริง

| ทาง | ค่าใช้จ่าย | เปิดตลอดไหม | เหมาะกับ |
| --- | --- | --- | --- |
| **A. Cloudflare Quick Tunnel** (ต่อจากสแตก local) | 0 | ไม่ (เปิดเท่าที่ PC เปิด) | ทดสอบวันนี้จากมือถือ/เน็ตนอกบ้าน ไม่ต้องสมัครอะไร |
| **B. Oracle Cloud Always Free VM** + Docker | 0 (ต้องผูกบัตรเพื่อยืนยันตัวตน ไม่ตัดเงิน) | ใช่ | โฮสต์จริงให้ tester เล่นได้ตลอด |
| C. Render / Koyeb free web service | 0 | **ไม่** — sleep หลัง idle | ❌ ไม่ใช้: socket หลุด + ห้องในหน่วยความจำหาย |
| D. Fly.io | ไม่ฟรีแล้ว (ต้องผูกบัตร, จ่ายตามใช้) | ใช่ | ทางสำรองถ้า B ติด |
| Neon / Supabase free Postgres | 0 | ใช่ (DB ไม่ sleep) | ไม่จำเป็น — สแตกนี้มี Postgres ในตัวแล้ว; ค่อยย้ายถ้าอยากได้ managed DB |

## A. Cloudflare Quick Tunnel — ทดสอบวันนี้ (ไม่ต้องสมัคร)

สแตก local รันอยู่แล้ว (`docker ps` → `bobpr5-nakama-1` healthy, 7350):

```sh
winget install --id Cloudflare.cloudflared      # ติดตั้งครั้งแรก (หรือโหลด binary จาก GitHub releases)
cloudflared tunnel --url http://127.0.0.1:7350  # พิมพ์ URL https://xxxx.trycloudflare.com
```
หรือไม่ติดตั้งอะไรเลย ใช้คอนเทนเนอร์ (Docker มีอยู่แล้ว) — URL อยู่ใน log ของ container:
```sh
docker run --rm --name bob-tunnel cloudflare/cloudflared:latest tunnel --url http://host.docker.internal:7350
```

- ในเกม: **ตั้งค่า → ที่อยู่เซิร์ฟเวอร์** ใส่ `https://xxxx.trycloudflare.com` (ช่องนี้รับ `https://` และตัด port 443 ให้เอง)
- bot: `BOB_HOST=xxxx.trycloudflare.com BOB_TLS=1 node mp-host.js 2`
- URL เปลี่ยนทุกครั้งที่เปิด tunnel ใหม่ — พอสำหรับรอบทดสอบ ไม่พอสำหรับปล่อยจริง
- ทางเลือกฟรีที่เครื่องนี้มีอยู่แล้ว: **Tailscale** — ลงแอปบนมือถือ เข้า tailnet เดียวกัน แล้วใส่ IP ของ PC (`host:7350`) ไม่ต้องมีโดเมน/ไม่ต้องเปิดพอร์ต (แต่ต้องติดตั้งแอปบนเครื่อง tester ทุกคน)

## B. Oracle Cloud Always Free — เปิดตลอด 24 ชม.

1. สมัคร Oracle Cloud (ต้องผูกบัตรเครดิตเพื่อยืนยัน; Always Free ไม่คิดเงิน) แล้วสร้าง VM: **VM.Standard.A1.Flex 4 OCPU / 24 GB** หรือ `E2.1.Micro`, image **Ubuntu 24.04**
2. VCN → Security List เปิด ingress **TCP 22, 80, 443** (ไม่ต้องเปิด 7350/7351)
3. เข้า SSH แล้วติดตั้ง Docker: `curl -fsSL https://get.docker.com | sh`
4. คัดลอกโฟลเดอร์ `server/` ขึ้นเครื่อง (`git clone` หรือ `scp -r`) แล้ว:
   ```sh
   cd server/deploy
   cp .env.example .env && nano .env     # ตั้งรหัส postgres/console, SITE_ADDRESS
   docker compose up -d
   docker compose ps                     # postgres + nakama healthy, caddy running
   curl -s https://<โดเมน>/healthcheck    # 200
   ```
5. โดเมนฟรี: สมัคร **DuckDNS** → ตั้ง subdomain ชี้ A record ไป public IP ของ VM → ใส่ชื่อนั้นใน `SITE_ADDRESS`
   Caddy จะขอ certificate Let's Encrypt ให้เอง (ถ้าไม่มีโดเมนเลย ใช้ `SITE_ADDRESS=:80` แล้วเกมใส่ `http://<IP>` — ใช้ทดสอบเท่านั้น)
6. ทดสอบด้วย bot จากเครื่องเรา: `BOB_HOST=<โดเมน> BOB_TLS=1 node mp-test.js 2`
7. ในเกม: **ตั้งค่า → ที่อยู่เซิร์ฟเวอร์** = `https://<โดเมน>`

> หมายเหตุ: Oracle อาจเรียกคืนเครื่อง Always Free ที่ idle นาน — ถ้าเป็นเซิร์ฟเวอร์จริง ให้เช็กเป็นระยะหรืออัปเป็น pay-as-you-go ระดับล่างสุด

## Bot / เทสต์ที่มีอยู่

| คำสั่ง | ทำอะไร | ต้องมีเซิร์ฟเวอร์ |
| --- | --- | --- |
| `node task-race-test.js` | ตรวจสัญญา Task Race (opcode 16–21) ในตัว match โดยไม่ใช้ Nakama — 39 checks | ไม่ |
| `node mp-test.js 2` | bot 2–4 ตัวเข้า Nakama เล่นครบ 3 รอบ ต้องเห็นผลตรงกันทุก client | ใช่ |
| `node mp-bots-queue.js 1` | bot เข้าคิว **matchmaker** → คนจริงกดปุ่มออนไลน์แล้วเจอห้องทันที (bot ที่เหลือ join ตาม match id) | ใช่ |
| `node mp-bots-queue.js 2 90 --all` | self-test ของคิว: bot ทุกตัวเข้าคิวเอง เล่นกันเองจนจบ | ใช่ |
| `node mp-host.js 2` | สร้างห้องส่วนตัว + bot N ตัว แล้วรอผู้เล่นจริง; เขียน match id ลง `mp-match.txt` | ใช่ |
| Unity: menu **Bob > Run Online Test** | client จริง join ห้องของ `mp-host.js` ผ่านโค้ด online ตัวจริง (`Game/BobGame.OnlineDev.cs`) | ใช่ |

`BOB_HOST`, `BOB_TLS=1`, `BOB_KEY` ใช้ได้กับทั้ง `mp-test.js` และ `mp-host.js` (ค่าเริ่มต้น = local `127.0.0.1:7350` + `defaultkey`)

> **ทางเข้าเล่นออนไลน์ของเกม:** ปุ่ม `ออนไลน์ 2-4 คน` อยู่ใน `MainMenu()` (`Game/BobGame.Hud.cs:592`) ซึ่ง**ยังไม่ถูกเรียกจาก flow ปัจจุบัน** (Home → บท → TaskRace) — ผู้เล่นจึงเข้าไม่ได้จนกว่าจะเพิ่มจุดเข้า (เช่น ปุ่มบนหน้า S16 หรือต่อจากHome) การทดสอบตอนนี้ใช้ `Bob > Run Online Test` หรือ `mp-bots-queue.js` แทน
> **ขอบเขต:** ออนไลน์ที่มีคือโหมดรถเข็นเดิม (`bob_coop`) — Task Race ยังไม่มีลูปเน็ตเวิร์กในซีน แม้ฝั่ง server ตรวจ opcode 16–21 แล้วก็ตาม

## ก่อนปล่อยจริง (ยังไม่ทำ)

- เปลี่ยน `NAKAMA_SERVER_KEY` แล้วอัปเดตฝั่งเกม (ตอนนี้ `BobServer.FromAddress` อ่านคีย์จากฟิลด์ `serverKey` ของฉาก)
- เปลี่ยนรหัส console + ไม่เปิดพอร์ต 7351 ออกนอก
- เก็บ analytics: ตาราง `bob_events` เขียนอยู่แล้วผ่าน `nk.sqlExec` (เข้าดูด้วย RPC `bob_analytics` + http key)

# 28 · LINE Login (บัญชีจริงบนมือถือ)

สถานะ: **ฝั่งเซิร์ฟเวอร์ยังอยู่ — ฝั่ง Unity ถูกถอดออกแล้ว 16 พ.ย. 2026** (ตอนตัด Nakama ออกทั้งโปรเจกต์ เหลือ pure UGS ตาม `27_UGS_Multiplayer.md`)
RPC `line_status` / `line_login` ใน `server/` ยังใช้ได้ (session ขนานถืออยู่) แต่ `BobServer` / `BobLineLogin` / ปุ่ม LINE บนหน้า Login และ deep link ในแอป **ถูกลบออกจากบิลด์ Unity แล้ว** — ถ้าจะเอากลับ ต้องต่อ RPC ผ่าน UGS Cloud Code หรือเรียก Nakama จากฝั่งแอปใหม่ (ไม่ใช่ผ่าน `Bob.Net` เดิม)
Channel: LINE Login channel `2012623256` (Channel ID เป็นข้อมูลสาธารณะ ปรากฏใน authorize URL อยู่แล้ว)

## 1. ภาพรวมการไหล

```
มือถือ                         เบราว์เซอร์ / แอป LINE                    Nakama (local)
  │  line_status  ────────────────────────────────────────────────────►  อ่าน env: channel id, redirect
  │  ◄── {ok, channelId, redirect}                                        (ไม่ส่ง secret กลับ)
  │  สร้าง PKCE (verifier 43 ตัว, challenge S256) + state
  │  Application.OpenURL(authorize?client_id&redirect_uri&code_challenge) ─► ผู้เล่นกด "อนุญาต"
  │                                                                        LINE 302 กลับ redirect?code&state
  │  ◄── deep link bob://line-login?code=…  (หรือ "วางลิงก์" จากคลิปบอร์ด)
  │  line_login {code, verifier} ──────────────────────────────────────►  POST /oauth2/v2.1/token (secret อยู่ที่นี่)
  │                                                                        GET  /v2/profile
  │                                                                        customId = "line_" + hmac(userId, secret)
  │  ◄── {customId, userId, lineUserId, displayName, picture}              authenticateCustom(customId, create)
  │  AuthenticateCustomAsync(customId)  ────────────────────────────────►  session ของผู้เล่น
  │  เก็บ PlayerPrefs bob.line.custom / bob.line.name; Connect() ใช้ customId นี้ทุกครั้งหลังเปิดแอป
```

เหตุผลที่ต้องแลกโค้ดฝั่งเซิร์ฟเวอร์: LINE บังคับให้ส่ง `client_secret` ตอนแลก token แม้ใช้ PKCE — ถ้าใส่ในแอป secret จะหลุดทั้ง APK
ตัวแอปจึงเห็นแค่ `customId` ที่ hash แล้ว ซึ่งใช้ล็อกอินซ้ำได้แต่เดาบัญชีคนอื่นไม่ได้

## 2. สิ่งที่เขียนไปแล้ว

| ไฟล์ | หน้าที่ |
| --- | --- |
| `server/line-auth.js` | RPC `line_status` (บอกว่าเซิร์ฟเวอร์พร้อมไหม + redirect ที่ต้องใช้) และ `line_login` (แลกโค้ด → โปรไฟล์ → custom id) — อ่าน `ctx.env` เท่านั้น |
| `server/rpc.js` | ลงทะเบียน `line_status` / `line_login` ใน `InitModule` |
| `server/build-module.js` | ต่อท้าย `line-auth.js` ตอนสร้าง `modules/index.js` |
| `server/line-auth-test.js` | เทสต์ออฟไลน์ (fake `nk`): status, happy path, โค้ดหมดอายุ, fallback sha256 — `node line-auth-test.js` |
| `server/.env.example`, `server/deploy/.env.example` | `LINE_CHANNEL_ID`, `LINE_CHANNEL_SECRET`, `LINE_REDIRECT_URI` (ค่าจริงอยู่ใน `.env` ที่ถูก ignore) |
| `server/docker-compose.yml`, `server/deploy/docker-compose.yml` | ส่ง env สามตัวเข้า container `nakama` |
| `unity/Assets/Scripts/Net/BobLineLogin.cs` | ~~PKCE S256, state, เปิด authorize URL, รับ deep link, รอสูงสุด 180 วิ, แลกโค้ดผ่าน RPC, เก็บ prefs, ปุ่มวางลิงก์~~ **ถอดออก 16 พ.ย. 2026** — สำเนาอยู่ที่ `unity/Logs/removed-nakama/` |
| `unity/Assets/Scripts/Net/BobServer.cs` | ~~`SignInCustomAsync(customId)`, `LineStatusAsync()`, `LineLoginAsync(code, verifier)`~~ **ถอดออกพร้อมทั้ง `Net/` 16 พ.ย. 2026** |
| `unity/Assets/Scripts/Game/BobGame.Menus.cs` | ~~ปุ่มเขียว `#06C755` "เข้าสู่ระบบด้วย LINE" + ปุ่มวางลิงก์~~ **ถอดออก 16 พ.ย. 2026** (หน้า Login เหลือ Guest) |
| `unity/Assets/Scripts/Game/BobGame.cs` | ~~`Connect()` เลือก `SignInCustomAsync(...)`~~ **ถอดออก 16 พ.ย. 2026** (ไม่มีเน็ตในแอปแล้ว) |
| `unity/Assets/Scripts/Editor/BobAndroidDeepLinks.cs` | `IPostGenerateGradleAndroidProject` เติม `<intent-filter>` `VIEW/DEFAULT/BROWSABLE` scheme `bob` host `line-login` ลง manifest ที่ Unity สร้าง — ไฟล์ยังอยู่ แต่ไม่มีตัวรับ deep link ในเกมแล้ว |

## 3. ตั้งค่าครั้งแรก (ทำครั้งเดียว)

1. **LINE Developers Console** → channel `2012623256` → แท็บ *LINE Login* → **Callback URL**
   ใส่ค่าเดียวกับ `LINE_REDIRECT_URI` เช่น `bob://line-login`
   - ถ้าคอนโซลไม่รับ custom scheme (บางบัญชีรับเฉพาะ `https://`): ใส่ `https://<โดเมนของ Nakama>/line-callback`
     แล้วตั้ง `LINE_REDIRECT_URI` เป็นค่านั้น — ต้องมีหน้าเว็บที่พาผู้เล่นกลับแอป (ดูข้อ 6)
2. **`server/.env`** (ก็อปจาก `server/.env.example`; ไฟล์นี้ถูก git ignore):
   ```
   LINE_CHANNEL_ID=2012623256
   LINE_CHANNEL_SECRET=<secret จากคอนโซล>
   LINE_REDIRECT_URI=bob://line-login
   ```
   **secret ที่เคยพิมพ์ในแชทถือว่ารั่วแล้ว — กด *Issue/Regenerate* ในคอนโซล แล้วใช้ค่าใหม่นี้**
3. รีสตาร์ทโมดูล: `cd server && node build-module.js && docker compose restart nakama`
4. ตรวจว่าเซิร์ฟเวอร์พร้อม (ไม่ต้องมี session):
   ```
   curl -s -X POST "http://127.0.0.1:7350/v2/rpc/line_status?http_key=defaulthttpkey&unwrap=true" -d '{}'
   # {"ok":true,"channelId":"2012623256","redirect":"bob://line-login","note":"ready"}
   ```
   ถ้าได้ `"ok":false` = container ยังไม่เห็น env (`.env` ต้องอยู่ข้าง `docker-compose.yml` ที่ใช้รัน)
5. ในเกม: หน้า Login → **เข้าสู่ระบบด้วย LINE** เบราว์เซอร์/แอป LINE เปิด → อนุญาต → กลับเข้าเกมเอง
   ชื่อ LINE จะขึ้นในบรรทัดสถานะ และ `Connect()` จะเซ็นอินด้วยบัญชี LINE ทุกครั้งหลังเปิดแอป

## 4. ทดสอบบนมือถือ (local)

- มือถือต้องเข้าถึง Nakama ได้: ตั้งที่อยู่เซิร์ฟเวอร์ในหน้า Settings เป็น IP ใน LAN ของ PC (`192.168.x.x`)
  หรือ URL ของ tunnel — `127.0.0.1` ใช้จากมือถือไม่ได้
- ต้องมีเน็ตสำหรับคุยกับ `access.line.me` / `api.line.me` (ตัวแลกโค้ดวิ่งจาก PC ผ่าน Docker)
- ถ้าเบราว์เซอร์ไม่เด้งกลับแอป: คัดลอก URL จากแถบที่อยู่ (มี `?code=…`) → กลับเข้าเกม → **วางลิงก์ที่ LINE ส่งกลับ**
  (ปุ่มนี้จะโผล่เฉพาะตอนกำลังรอผล) วิธีนี้ใช้ได้ทั้งมือถือและ Editor บน Windows
- APK ใหม่ต้อง build ผ่าน `Bob > Android/Build APK` เพื่อให้ `BobAndroidDeepLinks` เติม manifest — APK เก่าไม่มี filter นี้
- Editor บน Windows: deep link ใช้ไม่ได้ (ไม่มีตัวรับ scheme) ให้ใช้ปุ่มวางลิงก์แทน — ต้องตั้ง Callback URL ให้ตรงกับที่เปิดจริง

## 5. ข้อจำกัดที่ต้องรู้ (สถานะ local)

- **บัญชีแยกจาก Guest:** ผู้เล่นที่เคยเล่นแบบ device id จะมีอีกบัญชีเมื่อล็อกอิน LINE (progress/coins/XP ไม่ย้ายตาม)
  การผูกบัญชี (link device ↔ LINE) ยังไม่ได้ทำ
- **ไม่มี refresh/revoke:** เก็บแค่ custom id; ถ้าผู้เล่นกด *ยกเลิกการเชื่อมต่อ* ใน LINE console เกมยังล็อกอินได้
  (ไม่เรียก LINE API อีกหลังล็อกอินครั้งแรก) — ถ้าต้องการตัดสิทธิ์จริงต้องเพิ่มการตรวจ token หรือลบ custom id ฝั่งเซิร์ฟเวอร์
- **PC ต้องออนไลน์ตอนล็อกอิน:** ตัวแลกโค้ดอยู่บน Nakama local; ถ้าปิด PC กลางทางจะได้ข้อความ timeout (ไม่ crash)
- **ไม่ได้ฝัง LINE SDK:** ใช้ web OAuth + deep link จึงไม่ต้องเพิ่ม AAR; แลกกับการที่ LINE app อาจเปิดในเบราว์เซอร์แทนแอป
- **iOS:** ยังไม่ทำ (ต้องเพิ่ม `CFBundleURLTypes` และ `Application.deepLinkActivated` มีอยู่แล้ว)
- `launchMode` ของ activity มาจาก Unity 6 (`singleTask`) — อย่าแก้ใน manifest เพราะ deep link จะเปิด activity ใหม่ทับเกม

## 6. ถ้าคอนโซลบังคับ https callback

ทางที่แนะนำเมื่อมีโดเมนแล้ว (Caddy อยู่ใน `server/deploy`):

1. เพิ่ม route ใน `deploy/Caddyfile` ให้ `/line-callback` ตอบหน้า HTML เล็ก ๆ ที่มีปุ่ม
   `<a href="bob://line-login?code=…&state=…">กลับเข้าเกม</a>` (สร้างลิงก์จาก query string ของ request)
2. ตั้ง Callback URL = `https://<โดเมน>/line-callback` และ `LINE_REDIRECT_URI` ค่าเดียวกัน
3. ผู้เล่นกดปุ่มหนึ่งครั้ง — ผู้ใช้เป็นคนกดเอง เบราว์เซอร์จึงยอมเปิด custom scheme (Chrome บล็อกการเด้งเองโดยไม่มี gesture)

ทางลัดที่ไม่ต้องมีโดเมน: ใช้ปุ่ม "วางลิงก์ที่ LINE ส่งกลับ" ในเกม (ข้อ 4) — เหมาะกับการทดสอบ ไม่เหมาะกับผู้เล่นจริง

## 7. ตรวจสอบที่ทำไปแล้ว / ที่ยังไม่ได้ทำ

- ผ่าน: `node line-auth-test.js` (10 checks) · `node --check modules/index.js` · Unity batchmode compile 0 error (`unity/Logs/line-login-compile2.log`)
- ยังไม่ได้ทำ: รันกับ LINE จริง (ต้องมี `.env` + secret ใหม่ + มือถือที่ต่อ adb) · ทดสอบ manifest ที่ build แล้วด้วย `adb shell am start -a android.intent.action.VIEW -d "bob://line-login?code=test"` · หน้า callback บน Caddy

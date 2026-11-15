# 27 · Multiplayer พื้นฐานด้วย Unity Gaming Services (Relay + Lobby + Netcode)

สถานะ: **โครงพร้อมทดสอบ** (16 พ.ย. 2026) — ตรวจกับ Unity Cloud จริงแล้ว (sign-in / relay / lobby / host+leave ผ่าน `Bob > Run UGS Check`, log `unity/Logs/ugs-check6.log`) แต่ยังไม่ผูกกับ Home/S16 และยังไม่ได้เล่นสองเครื่องพร้อมกัน

## 1. ทำไมมีทั้ง Nakama และ UGS

| ระบบ | ใช้เมื่อ |
| --- | --- |
| Nakama (`Bob.Net`, `server/`) | โหมดรถเข็นออนไลน์เดิม `bob_coop` — server-authoritative, ใช้ร่วมกับ bot scripts |
| **UGS (ชุดนี้)** | ทดสอบ multiplayer แบบ **player-hosted** เร็ว ๆ: host เครื่องหนึ่งถือเกม, Relay เป็นท่อ UDP/TLS, Lobby เป็นป้ายประกาศห้อง |

ทั้งสองชุดแยกกันสนิท: ชุด UGS อยู่ที่ `Assets/Scripts/Networking/` และไม่แตะ `Bob.Net`, ฉาก, หรือกติกาเดิม

## 2. Package ที่เพิ่ม (`unity/Packages/manifest.json`)

```
com.unity.netcode.gameobjects   2.14.0
com.unity.services.authentication 3.9.0
com.unity.services.core         1.19.0
com.unity.services.lobby        1.3.0
com.unity.services.relay        1.2.0
```

Transport (`com.unity.transport` 6.6.0) เป็น **builtin ของ Unity 6000.6** ไม่ต้องใส่ใน manifest — resolver หยิบให้เอง

> **หมายเหตุจาก Package Manager (สำคัญ):** Unity ขึ้นเตือนว่า `com.unity.services.lobby` และ `com.unity.services.relay` **deprecated** แล้ว — SDK ถูกรวมใน `com.unity.services.multiplayer` (Sessions API) ชุดนี้ยังใช้ตัวคลาสสิกตามที่ owner สั่ง (`LobbyService` / `RelayService`) และยังทำงานได้ (ผ่าน check 16 พ.ย.) แต่ของใหม่ควรย้ายไป `com.unity.services.multiplayer` เมื่อจะทำเป็นระบบจริง

## 3. สคริปต์ 3 ไฟล์ (`unity/Assets/Scripts/Networking/`)

| ไฟล์ | หน้าที่ |
| --- | --- |
| `UgsBootstrapManager.cs` | `UnityServices.InitializeAsync()` + `SignInAnonymouslyAsync()` ครั้งเดียว, log `PlayerId`, `EnsureReadyAsync()` เรียกซ้ำได้ |
| `RelayLobbyManager.cs` | `CreateGameAsync(name, maxPlayers, isPrivate)` = relay allocation → join code → เก็บใน Lobby Data (`RelayJoinCode`, member visibility) → `StartHost()`; `QuickJoinOrJoinByCodeAsync(code)` = join lobby → อ่าน code → `JoinAllocationAsync` → `StartClient()`; heartbeat 15 วิ; `LeaveOrShutdownAsync()` |
| `SimpleDebugLobbyUI.cs` | IMGUI ปุ่ม Create / Quick Join / Leave + แสดง lobby code, relay code, สถานะเน็ต — **Editor และ Development Build เท่านั้น** (`#if UNITY_EDITOR || DEVELOPMENT_BUILD`) |

รายละเอียดที่ควรรู้:

- Relay allocation ขอ `maxPlayers - 1` connection เพราะ host กินหนึ่งช่อง
- ใช้ **DTLS**: เลือก endpoint ที่ `ConnectionType == "dtls"` จาก `allocation.ServerEndpoints` แล้วส่งเข้า `UnityTransport.SetRelayServerData(host, port, allocationId, key, connectionData, hostConnectionData, isSecure: true)`
  — API ปี 2025 ไม่มี `RelayServerData(allocation, "dtls")` แบบในบทความเก่าแล้ว
- Host ต้อง ping lobby ทุก 15 วิ (`SendHeartbeatPingAsync`) ไม่งั้น lobby หมดอายุที่ 30 วิ
- ปิดเกมโดยไม่เรียก `LeaveOrShutdownAsync` → lobby จะหมดอายุเองใน 30 วิ

## 4. ตั้งค่าใน Unity Editor (ทำตามลำดับ)

1. **เปิด Package Manager** (`Window > Package Manager`) → แท็บ In Project → ตรวจว่ามี Netcode for GameObjects, Relay, Lobby, Authentication, Services Core
2. **ผูก Unity Cloud project**: `Edit > Project Settings > Services`
   - ถ้าโครงการเคยผูกแล้ว (มี `cloudProjectId` = `b48ee5f1-…`) กด **Use an existing Unity project ID** แล้วเลือกบัญชี/org ที่ถือ project นั้น
   - ถ้าเป็น project ใหม่: กด **Create project** (ฟรี) แล้วรอ Unity สร้างให้เสร็จ
   - เปิดบริการใน dashboard: **Relay = ON**, **Lobby = ON** (ทั้งสองมี free tier)
3. **สร้างฉากทดสอบ** (เช่น `Assets/Scenes/UgsTest.unity` ใหม่ หรือฉากเปล่า):
   - `GameObject > Create Empty` ตั้งชื่อ `Network`
   - Add Component: **NetworkManager**
   - Add Component: **UnityTransport** (อยู่บน object เดียวกัน)
   - ที่ NetworkManager → ช่อง **Network Transport** ลาก/เลือก `UnityTransport` ตัวเดียวกัน
   - ที่ UnityTransport → **Protocol Type** = `Relay Unity Transport` (สคริปต์สลับให้เองตอนรัน แต่ตั้งไว้ให้ตรงก็ได้)
4. **Add Component ให้ `Network`**: `RelayLobbyManager` — ช่องทั้งสามเว้นว่างได้ (auto-find จาก `NetworkManager.Singleton` / `UgsBootstrapManager.Instance`)
5. สร้าง object `Bootstrap` → Add Component: **UgsBootstrapManager** (เปิด `Sign in on Awake`)
6. สร้าง object `DebugUI` → Add Component: **SimpleDebugLobbyUI**
7. กด **Play** ใน Editor:
   - Console ต้องเห็น `[UGS] signed in · PlayerId …`
   - กด **Create Room** → เห็น `hosting · lobby ABC123 · 1/4` และ relay code
8. **ทดสอบผู้เล่นที่สอง**: `File > Build Settings > Windows` → Build เป็นโฟลเดอร์แยก → เปิดเกมที่ build → กด Play ใน Editor พร้อมกัน:
   - ฝั่ง client พิมพ์ lobby code (6 ตัว) แล้วกด **Quick Join** หรือปล่อยว่างแล้วกด **Quick Join** เพื่อจับห้องสาธารณะ
   - ทั้งสองฝั่งได้ `HOST` / `CLIENT connected` และ PlayerId ต่างกัน
9. ยังไม่มีตัวละคร/ของ sync ในชุดนี้ — เป็นแค่ท่อเชื่อม ถ้าจะให้มี prefab ขยับ ต้องตั้ง NetworkObject/NetworkBehaviour เพิ่ม (งานถัดไป)

## 5. โควตา free tier (ใช้ให้อยู่ในกรอบ)

- Lobby/Relay ของ Unity Cloud ฟรีแบบมีเพดานรายเดือน (จำนวนผู้เล่นพร้อมกัน + bandwidth ของ Relay) — ดูตัวเลขจริงที่ **Unity Cloud Dashboard > Usage**
- ชุดทดสอบนี้สร้าง lobby ครั้งละห้องและลบทิ้งเมื่อกด Leave → เปลืองน้อยมาก
- อย่าเปิด lobby ค้างเป็นชั่วโมงโดยไม่ ping — ระบบ heartbeat มีไว้กัน lobby ตาย แต่ก็ยังกินช่อง CCU

## 6. ตรวจแบบไม่ต้องกด Play

`Bob > Run UGS Check` (Editor) — เข้า Play Mode ในฉากว่าง → sign-in → สร้าง relay allocation → สร้าง lobby → ลบทิ้ง → **สร้าง NetworkManager + UnityTransport ชั่วคราวแล้วเรียก `RelayLobbyManager.CreateGameAsync` / `LeaveOrShutdownAsync` จริง** (`host ok` → lobby ถูกลบ → transport ปิด) แล้ว log `PASS`
รันแบบไม่ต้องคลิก (ไม่ใส่ `-batchmode` เพราะ Unity Services ใช้ได้เฉพาะ Play Mode):

```bash
unity.exe -projectPath . -logFile Logs/ugs-check.log -executeMethod Bob.EditorTools.UgsCheck.RunBatch
```

## 7. ข้อจำกัดที่รู้อยู่

- `SimpleDebugLobbyUI` เป็น IMGUI (ปุ่มทดสอบ) — ไม่ได้ออกแบบให้เป็น UI จริงของเกม
- ยังไม่มี reconnect / host migration / scene sync — ถ้า host ปิด เกมจบ
- ยังไม่ผูกปุ่มเข้า Home/S16: ชุดนี้แยกจาก flow หลักโดยตั้งใจ
- ถ้า Unity Cloud ยังไม่เปิด Relay/Lobby จะได้ error `Unauthorized` / `Service disabled` ตอน Create Room — กลับไปเปิดที่ dashboard

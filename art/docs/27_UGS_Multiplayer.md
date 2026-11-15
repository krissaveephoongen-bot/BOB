# 27 · Pure UGS — โปรไฟล์คลาวด์ อันดับ คะแนน และห้องผู้เล่น (Unity Gaming Services)

สถานะ: **โครง pure UGS พร้อมทดสอบ** (16 พ.ย. 2026) — ตัด Nakama ออกจากฝั่ง Unity ทั้งหมดแล้ว
สคริปต์ทั้งชุดอยู่ที่ `unity/Assets/Scripts/UGS/` และไม่ผูกกับกติกาในเกม (คะแนนการเล่นยังคิดบนเครื่อง)

## 1. ขอบเขต

| ส่วน | สถานะ |
| --- | --- |
| Nakama ฝั่ง Unity (`using Nakama;`, `Bob.Net`, `Bob.Server`, Coop, ฉาก `Coop`, ปุ่มออนไลน์เดิม, LINE login) | **ลบออกหมด** (16 พ.ย. 2026) |
| โฟลเดอร์ `server/` (Nakama ของ session ขนาน) | **คงไว้** — ไม่อยู่ในบิลด์ Unity และไม่ถูกอ้างจากโค้ด Unity แล้ว |
| UGS: Authentication (anonymous) | ใช้ |
| UGS: Cloud Save (`player_profile`) | ใช้ |
| UGS: Leaderboards (`global_high_score`) | ใช้ |
| UGS: Relay + Lobby + Netcode (player-hosted) | ใช้ (ท่อเชื่อมเท่านั้น ยังไม่ sync ตัวละคร/ของ) |

กติกาการเล่น (BobRules / BobTaskRaceRules / BobMatch) ยังรันบนเครื่อง 100% — UGS เก็บเฉพาะโปรไฟล์ คะแนน และห้อง

## 2. Package ใน `unity/Packages/manifest.json`

```
com.unity.services.core            1.19.0
com.unity.services.authentication  3.9.0
com.unity.services.cloudsave       3.4.2
com.unity.services.leaderboards    2.3.5
com.unity.services.lobby           1.3.0
com.unity.services.relay           1.2.0
com.unity.netcode.gameobjects      2.14.0
```

`com.unity.transport` 6.6.0 เป็น builtin ของ Unity 6000.6 (resolver หยิบให้เอง)

> **หมายเหตุ:** `services.lobby` / `services.relay` ถูกประกาศ deprecated (รวมเข้า `com.unity.services.multiplayer` — Sessions API) แต่ยังทำงานได้และ owner สั่งใช้ตัวคลาสสิก `LobbyService` / `RelayService` ก่อน

## 3. สคริปต์ 6 ไฟล์ (`unity/Assets/Scripts/UGS/`)

| ไฟล์ | หน้าที่ |
| --- | --- |
| `PlayerProfileData.cs` | `[Serializable]` — `coins`, `level`, `unlockedItems` (ค่าที่เก็บใน Cloud Save) |
| `UgsCloudSaveManager.cs` | singleton — โหลด/เซฟ JSON ใต้ key `player_profile`; `LoadProfileAsync`, `SaveProfileAsync`, `AddCoinsAndSaveAsync(int)`, `UnlockItemAndSaveAsync(string)` |
| `UgsLeaderboardManager.cs` | singleton — `leaderboardId = "global_high_score"`; `SubmitScoreAsync(double)` → `AddPlayerScoreAsync`; `GetTopScoresAsync(limit = 10)` → `GetScoresAsync` |
| `UgsRelayLobbyManager.cs` | singleton — `CreateRoomAsync(name, maxPlayers)`, `QuickJoinOrJoinByCodeAsync(code = "")`, heartbeat 15 วิ, `LeaveRoomAsync()` |
| `UgsBootstrapManager.cs` | singleton — `UnityServices.InitializeAsync()` + `SignInAnonymouslyAsync()` ตอนเริ่ม แล้วโหลดโปรไฟล์, log `PlayerId`, `EnsureReadyAsync()` เรียกซ้ำได้ |
| `PureUgsDebugUI.cs` | IMGUI ปุ่มทดสอบ: +100 เหรียญ (Cloud Save), ส่งคะแนนสุ่ม + ดู Top 10, Create Room / Quick Join / Leave — Editor และ Development Build เท่านั้น |

API ที่ใช้ (ยืนยันกับ package ที่ติดตั้งจริง):

- Cloud Save 3.x: `CloudSaveService.Instance.Data.Player.LoadAsync(HashSet<string>)` → `Dictionary<string, Item>` (`Unity.Services.CloudSave.Models`), `item.Value.GetAs<string>()`, `SaveAsync(Dictionary<string, object>)`, exception `CloudSaveException`
- Leaderboards 2.x: `LeaderboardsService.Instance.AddPlayerScoreAsync(id, score)`, `GetScoresAsync(id, new GetScoresOptions { Limit = n })`, exception `Unity.Services.Leaderboards.Exceptions.LeaderboardsException`
- Relay 1.2: allocation ขอ `maxPlayers - 1` เพราะ host กินหนึ่งช่อง; เลือก endpoint `ConnectionType == "dtls"`; `utp.SetRelayServerData(host, port, allocationId, key, connectionData, hostConnectionData, isSecure: true)`
- Lobby 1.3: join code เก็บใน lobby data (`DataObject.VisibilityOptions.Member`), host ต้อง ping ทุก 15 วิ ไม่งั้น lobby หมดอายุที่ 30 วิ

## 4. ตั้งค่าใน Unity Editor (ทำตามลำดับ)

1. **Package Manager** → In Project → ต้องเห็น Services Core / Authentication / Cloud Save / Leaderboards / Lobby / Relay / Netcode for GameObjects
2. **ผูก Unity Cloud project**: `Edit > Project Settings > Services` → project ที่ผูกไว้คือ `b48ee5f1-49d8-423b-872b-ba907bd41bf6` (`unity/ProjectSettings/ProjectSettings.asset` → `cloudProjectId`)
3. **เปิดบริการใน Unity Cloud Dashboard** (Project → Services):
   - **Authentication** = ON (anonymous)
   - **Cloud Save** = ON
   - **Leaderboards** = ON → สร้าง leaderboard id **`global_high_score`** (sort: High to Low, type: Score)
   - **Relay** = ON, **Lobby** = ON
4. **สร้างฉาก/object สำหรับทดสอบ** (เช่นใน `Assets/Scenes/Game.unity` หรือฉากเปล่า):
   - `GameObject > Create Empty` ชื่อ `UGS` → Add Component: **UgsBootstrapManager** (คง `Sign in on Awake`)
   - Add Component: **UgsCloudSaveManager**, **UgsLeaderboardManager**, **PureUgsDebugUI**
5. **NetworkManager + UnityTransport** (สำหรับ Relay/Lobby):
   - `GameObject > Create Empty` ชื่อ `Network` → Add Component: **NetworkManager**
   - Add Component: **UnityTransport** บน object เดียวกัน
   - ที่ NetworkManager → ช่อง **Network Transport** = `UnityTransport` ตัวเดียวกัน
   - ที่ UnityTransport → **Protocol Type** = `Relay Unity Transport` (สคริปต์สลับให้ตอนรัน แต่ตั้งไว้ให้ตรงก็ได้)
   - Add Component: **UgsRelayLobbyManager** — ช่อง `networkManager` / `transport` เว้นว่างได้ (auto-find จาก `NetworkManager.Singleton`)
   - NetworkManager ไม่ต้องมี prefab ผู้เล่นในชุดนี้ (ยังไม่มี sync ของ gameplay)
6. กด **Play** → Console ต้องเห็น `[UGS] signed in · PlayerId …` แล้ว `PureUgsDebugUI` มุมจอแสดง PlayerId / Coins / Level
7. ทดสอบ Cloud Save: กด **+100 Coins** → กด Play ซ้ำ → Coins ยังอยู่ (โหลดจาก cloud)
8. ทดสอบ Leaderboard: กด **Submit Random Score** → เห็น Top 10 ใน log/UI
9. ทดสอบห้อง: กด **Create Room** → เห็น `hosting · lobby ABC123 · 1/4`; เครื่องที่สอง (build หรือ Editor อีกตัว) กด **Quick Join** หรือกรอก lobby code; ทั้งสองฝั่งได้ `HOST` / `CLIENT connected`
10. **ทดสอบสองเครื่อง**: `File > Build Settings > Windows` → Build แยกโฟลเดอร์ → เปิดเกมที่ build + Play ใน Editor พร้อมกัน (PlayerId ต้องต่างกัน)

## 5. โควตา free tier

- Cloud Save / Leaderboards / Lobby / Relay ฟรีแบบมีเพดานรายเดือน — ดูตัวเลขจริงที่ **Unity Cloud Dashboard > Usage**
- ชุดทดสอบสร้าง lobby ครั้งละห้องและลบทิ้งเมื่อ Leave; อย่าเปิดค้างเป็นชั่วโมงโดยไม่มี heartbeat
- Anonymous sign-in สร้าง player ใหม่ต่อการติดตั้ง/ล้างข้อมูล — ไม่มีบัญชีข้ามเครื่องในชุดนี้

## 6. ตรวจแบบไม่ต้องกด Play

`Bob > Run UGS Check` (Editor) — เข้า Play Mode ในฉากว่าง → sign-in → relay allocation → สร้าง/ลบ lobby → สร้าง NetworkManager + UnityTransport ชั่วคราวแล้วเรียก `UgsRelayLobbyManager.CreateRoomAsync` / `LeaveRoomAsync` จริง (`host ok` → lobby ถูกลบ → transport ปิด) แล้ว log `UGS CHECK PASSED`
รันแบบไม่ต้องคลิก (ไม่ใส่ `-batchmode` เพราะ Unity Services ใช้ได้เฉพาะ Play Mode):

```bash
Unity.exe -projectPath . -logFile Logs/ugs-check.log -executeMethod Bob.EditorTools.UgsCheck.RunBatch
```

## 7. ข้อจำกัดที่รู้อยู่

- `PureUgsDebugUI` เป็น IMGUI สำหรับทดสอบ ไม่ใช่ UI จริงของเกม
- ยังไม่มี reconnect / host migration / scene sync — host ปิด = จบห้อง
- ยังไม่ผูกปุ่มห้องเข้า Home/S16: โปรไฟล์และอันดับพร้อมใช้ แต่ปุ่มห้องยังเป็น debug
- ถ้า Unity Cloud ยังไม่เปิด Relay/Lobby/Cloud Save/Leaderboards จะได้ `Unauthorized` / `Service disabled` — กลับไปเปิดที่ dashboard
- Leaderboard id ต้องตรงกับที่สร้างใน dashboard (`global_high_score`) ไม่งั้นได้ error ตอน submit

## 8. สิ่งที่ถูกลบไป (16 พ.ย. 2026)

`Assets/Scripts/Net/` (BobServer, BobOnline, BobLineLogin), `Assets/Scripts/Coop/`, `Assets/Scripts/Networking/` (ชุด UGS รุ่นแรก), `BobGame.Online.cs`, `BobGame.OnlineDev.cs`, `BobSmokeTest.cs`, `BobAutoSmoke.cs`, `BobCoopScene.cs`, ฉาก `Coop.unity` / `SmokeTest.unity` (เอาออกจาก Build Settings แล้ว), package `com.heroiclabs.nakama-unity`
ที่ยังเหลือ: โฟลเดอร์ `server/` ฝั่งเซิร์ฟเวอร์ (ของ session ขนาน) — ไม่ถูกอ้างจาก Unity แล้ว

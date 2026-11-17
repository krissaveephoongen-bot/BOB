# 27 · Pure UGS — โปรไฟล์คลาวด์ อันดับ คะแนน และห้องผู้เล่น (Unity Gaming Services)

สถานะ: **โครง pure UGS พร้อมทดสอบ** (16 พ.ย. 2026) — ตัด Nakama ออกจากฝั่ง Unity ทั้งหมดแล้ว, **compile ใน Unity ผ่าน 0 error** (`Logs/compile-pure-ugs2.log`)
สคริปต์ทั้งชุดอยู่ที่ `unity/Assets/Scripts/UGS/` และไม่ผูกกับกติกาในเกม (คะแนนการเล่นยังคิดบนเครื่อง)

## 1. ขอบเขต

| ส่วน | สถานะ |
| --- | --- |
| Nakama ฝั่ง Unity (`using Nakama;`, `Bob.Net`, `Bob.Server`, Coop, ฉาก `Coop`, ปุ่มออนไลน์เดิม, LINE login) | **ลบออกหมด** (16 พ.ย. 2026) |
| โฟลเดอร์ `server/` (Nakama ของ session ขนาน) | **คงไว้** — ไม่อยู่ในบิลด์ Unity และไม่ถูกอ้างจากโค้ด Unity แล้ว |
| UGS: Authentication (anonymous) | ใช้ |
| UGS: Cloud Save (`player_profile`) | ใช้ |
| UGS: Leaderboards (`global_high_score`) | ใช้ — แมตช์ที่จบแล้วส่งคะแนนรวมอัตโนมัติ (17 พ.ย. 2026, §3 `BobRaceScoreReporter`) |
| UGS: Relay + Lobby + Netcode (player-hosted) | **เล่นได้จริง 2–4 คน** (16 พ.ย. 2026): ห้อง + โค้ด 6 ตัว + ท่าฮีโร่ + งานที่เก็บร่วม — ดู §10 |

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

## 3. สคริปต์ 8 ไฟล์ (`unity/Assets/Scripts/UGS/`)

| ไฟล์ | หน้าที่ |
| --- | --- |
| `PlayerProfileData.cs` | `[Serializable]` — `coins`, `level`, `unlockedItems` (ค่าที่เก็บใน Cloud Save) |
| `UgsCloudSaveManager.cs` | singleton — โหลด/เซฟ JSON ใต้ key `player_profile`; `LoadProfileAsync`, `SaveProfileAsync`, `AddCoinsAndSaveAsync(int)`, `UnlockItemAndSaveAsync(string)` |
| `UgsLeaderboardManager.cs` | singleton — `leaderboardId = "global_high_score"`; `SubmitScoreAsync(double)` → `AddPlayerScoreAsync` และเก็บ `LastEntry` (มี `Rank`); `GetTopScoresAsync(limit = 10)` → `GetScoresAsync` |
| `UgsRelayLobbyManager.cs` | singleton — `CreateRoomAsync(name, maxPlayers)`, `QuickJoinOrJoinByCodeAsync(code = "")`, heartbeat 15 วิ, `LeaveRoomAsync()` |
| `BobOnlineRoom.cs` | singleton ห้องที่เล่นได้จริง — `HostAsync(heroId, maxPlayers)`, `JoinAsync(heroId, code)`, `LeaveAsync()`, `StartMatch(seed, players)`, `SendPose`, `SendTask`; สร้าง `NetworkManager` + `UnityTransport` เองตอนรัน; ข้อความ Netcode `bob.start` / `bob.pose` / `bob.task` (§10) |
| `BobRaceScoreReporter.cs` | ตัวส่งคะแนนของแมตช์ — `Attach(manager)` ตอน `TaskRaceModeManager.Awake`; ฟัง `MatchEnded` แล้วส่ง `manager.LastScore` เข้า `global_high_score` **ครั้งเดียวต่อแมตช์** เฉพาะเมื่อ `LastMatchRecorded` และเครื่องล็อกอินแล้ว; `LastRank` เก็บอันดับที่บอร์ดตอบกลับ (ยังไม่แสดงบน UI) |
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
5. **NetworkManager + UnityTransport** (สำหรับ Relay/Lobby) — **ไม่ต้องสร้างด้วยมือแล้ว**: `BobOnlineRoom.EnsureNetwork()` สร้าง `NetworkManager` + `UnityTransport` (Protocol = Relay, `DontDestroyOnLoad`) ตอนกดสร้าง/เข้าห้องใน S15 และตั้งเป็น `NetworkManager.Singleton` ให้เอง ถ้าอยากเห็นใน Hierarchy หรือแก้ค่าเอง ก็วาง object `Network` แบบเดิมได้ (NetworkManager + UnityTransport object เดียวกัน, ช่อง Network Transport ชี้ตัวเดียวกัน) — room จะใช้ตัวที่มีอยู่และไม่สร้างซ้ำ
   - NetworkManager ไม่ต้องมี prefab ผู้เล่นในชุดนี้ (ไม่มี spawn/sync ของ gameplay; ตัวละครระยะไกลวาดจาก `bob.pose` เอง)
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

- `PureUgsDebugUI` เป็น IMGUI สำหรับทดสอบ ไม่ใช่ UI จริงของเกม — ห้องจริงตอนนี้อยู่ที่ S15 (`BobGame.Menus.cs` `OnlineRoom()`), debug UI ยังใช้ทดสอบ Cloud Save/Leaderboard
- ยังไม่มี reconnect / host migration / scene sync — host ปิด = จบห้อง (client แค่หลุดการเชื่อมต่อแล้วเล่นต่อแบบออฟไลน์จนจบรอบเอง; ไม่มีป๊อปอัปหรือกลับเมนูอัตโนมัติ)
- **Duo/Team เล่นได้แบบออฟไลน์** — Home → S11 → S14 → `TaskRace_Duo`/`TaskRace_Team` พร้อมเพื่อนบอทในเครื่อง (`TaskRace/BobTaskRaceTeammates.cs`, ใบงานเดียวกับผู้เล่น). และ **ห้องออนไลน์ 2–4 คนเล่นได้จริงแล้ว** (16 พ.ย. 2026, §10): S14 ปุ่ม `ออนไลน์ 2–4 คน` → S15; เส้นทางออฟไลน์ยังไม่ต้องมีเน็ตและยังเล่นได้ทุกโหมด (`art/docs/11` §3.2/§4 S14–S15, `art/docs/24` §15.22–15.23)
- ออนไลน์รอบนี้เป็น **co-op บนเกาะของเจ้าของห้อง แข่งกับคู่แข่งจำลอง** — ไม่มีทีมสองฝั่ง/คะแนนรวมบนเซิร์ฟเวอร์ (§10.6)
- ถ้า Unity Cloud ยังไม่เปิด Relay/Lobby/Cloud Save/Leaderboards จะได้ `Unauthorized` / `Service disabled` — กลับไปเปิดที่ dashboard
- Leaderboard id ต้องตรงกับที่สร้างใน dashboard (`global_high_score`) ไม่งั้นได้ error ตอน submit
- **คะแนนบนบอร์ดเป็นความเชื่อใจฝั่งเครื่อง** — UGS Leaderboards รับค่าที่ client ส่ง ไม่มีการตรวจย้อนหลังว่าทำได้จริง (ต่างจาก Nakama ที่ `server/` ตรวจได้) ใครแก้ client ก็ปั๊มคะแนนได้; ถ้าจะกันโกงต้องย้ายสูตร `BobRaceScore` ไปตรวจฝั่งเซิร์ฟเวอร์ก่อน (ยังไม่ทำ)
- `LastRank` ที่บอร์ดตอบกลับยังไม่ถูกวาดบน UI — หน้าผลแสดงเฉพาะ "คะแนน N" (หน้ารายชื่ออันดับเป็นงานถัดไป)

## 8. สิ่งที่ถูกลบไป (16 พ.ย. 2026)

`Assets/Scripts/Net/` (BobServer, BobOnline, BobLineLogin), `Assets/Scripts/Coop/`, `Assets/Scripts/Networking/` (ชุด UGS รุ่นแรก), `BobGame.Online.cs`, `BobGame.OnlineDev.cs`, `BobSmokeTest.cs`, `BobAutoSmoke.cs`, `BobCoopScene.cs`, ฉาก `Coop.unity` / `SmokeTest.unity` (เอาออกจาก Build Settings แล้ว), package `com.heroiclabs.nakama-unity`
ที่ยังเหลือ: โฟลเดอร์ `server/` ฝั่งเซิร์ฟเวอร์ (ของ session ขนาน) — ไม่ถูกอ้างจาก Unity แล้ว

## 9. ตรวจ compile ตอนมี Unity เปิดโปรเจกต์ค้างอยู่

Unity เปิดโปรเจกต์เดียวกันสองตัวไม่ได้ (ตัวที่สองจะออกทันทีด้วย return code 1 ไม่ได้คอมไพล์) ถ้าเจอ `Temp/UnityLockfile` ค้าง:
ก๊อป `Assets/`, `Packages/`, `ProjectSettings/` และ `Library/PackageCache/` ไปโฟลเดอร์แยก (เช่น `compile-check/` ที่ถูก `git/info/exclude` ไว้) แล้วรัน
`Unity.exe -projectPath compile-check -batchmode -quit -logFile Logs/compile.log` — ผลคอมไพล์เชื่อถือได้เท่ากัน (asset เดียวกัน, GUID เดิม) และไม่ต้องปิด editor ที่ owner เปิดอยู่

## 10. ห้องออนไลน์ที่เล่นได้ 2–4 คน (16 พ.ย. 2026)

รอบนี้คือคิว 7b: เปลี่ยนท่อ Relay/Lobby/Netcode ที่มีอยู่ให้เป็น **ห้องที่เล่นด้วยกันได้จริง** โดยไม่ต้องแก้ฉากหรือทำ prefab ผู้เล่น — ใช้ Netcode *named messages* ล้วน ๆ (`CustomMessagingManager`) ฉาก `TaskRace` เดิมจึงไม่ต้อง rebuild

### 10.1 เส้นทางจากเมนูถึงแมตช์

```
Home (S10) → เลือกโหมด Solo/Duo/Team (S11) → S14 เลือกฮีโร่/ทีม
   ├─ กด "เริ่มแมตช์"        → ออฟไลน์เหมือนเดิม (TaskRace / TaskRace_Duo / TaskRace_Team)
   └─ กด "ออนไลน์ 2–4 คน"  → S15 ห้องออนไลน์
                                  ├─ เจ้าของห้อง: "สร้างห้อง" → Relay allocation + Lobby → โค้ด 6 ตัวบนการ์ด
                                  ├─ เพื่อน: "กรอกรหัส" (TouchScreenKeyboard) หรือ "เข้าห้องที่เปิดอยู่" (quick join)
                                  └─ เจ้าของห้องกด "เริ่มแมตช์" → ทุกเครื่องโหลด TaskRace พร้อม seed เดียวกัน
```

- โค้ดห้องคือ `Lobby.LobbyCode` (6 ตัว) — แสดงตัวใหญ่กลางการ์ดซ้ายของ S15, ที่นั่ง 4 ช่องวาดภาพฮีโร่ตามลำดับที่เข้า lobby (เจ้าของห้องก่อน)
- S15 ไม่มี matchmaking จริง: quick join = `QuickJoinLobbyAsync` ของ Lobby service (เลือกห้องที่เปิดอยู่ให้) และห้องรับ 2–4 คน (`BobOnlineRoom.MaxSlots = 4`)
- ออนไลน์ใช้ฉาก `TaskRace` เท่านั้น (ไม่ใช้ `TaskRace_Duo`/`TaskRace_Team`); จำนวนผู้เล่นในห้องคือ `MatchRequest.Players` จึงกำหนดจำนวนงานต่อรอบตามตาราง docs 16 §2.2 (2 คน = 1 common/2 long/4 short, 4 คน = 1/3/7)
- ออกจากห้อง/กด back: host ลบ lobby, client `RemovePlayerAsync`, แล้ว `NetworkManager.Shutdown()` — กลับ Home ได้ทันที

### 10.2 สัญญาข้อความ (Netcode named messages)

| ข้อความ | ผู้ส่ง → ผู้รับ | delivery | payload | ปลายทาง |
| --- | --- | --- | --- | --- |
| `bob.start` | host → ทุกคน | ReliableSequenced | `int seed`, `int players` | `MatchRequest.SetOnline(host:false, players, seed)` แล้ว `LoadScene("TaskRace")` (client ไม่โหลดจาก UI ตัวเอง) |
| `bob.pose` | ทุกคน → host (client) / host → ทุกคน | Unreliable | `int origin`, `float x`, `float z`, `float yaw` | `BobTaskRaceOnline` ขยับตัวละครระยะไกล (`Vector3.Lerp` + `Quaternion.Slerp`) |
| `bob.task` | ทุกคน → host (client) / host → ทุกคน | ReliableSequenced | `int origin`, `byte[] spotId`, `bool completed` | ฝั่งรับ `spot.ForceComplete()` ถ้ายังไม่สะอาด |

- **origin id อยู่ใน payload** เพราะ client ส่งถึง host คนเดียว: ถ้าใช้ client id บนสาย 3–4 คนจะถูกยุบเป็นตัวเดียว — host จึง rebroadcast พร้อม origin เดิม และฝั่งรับข้าม event ที่ `origin == LocalNetId` (กัน apply ซ้ำกับที่ทำเองไปแล้ว)
- client ยอมรับ `bob.task`/`bob.pose` จาก `NetworkManager.ServerClientId` เท่านั้น; host คือผู้ตัดสินว่าสถานีไหนสะอาด (host-authoritative) แต่คะแนนยังคิดบนเครื่องเหมือนเดิม
- ไม่มี prefab/NetworkObject spawn, ไม่มี scene sync, ไม่มี RPC ต่อผู้เล่น — named message พอสำหรับตัวละคร 4 ตัว + สถานี ~10 จุด

### 10.3 seed เดียวกันทั้งห้อง

`TaskRaceRound.Start()` ปกติสุ่ม seed ใหม่เมื่อ `randomSeedEachPlay`; ออนไลน์จึงต้องล็อก:
`BobOnlineRoom.StartMatch(seed, players)` → `MatchRequest.SetOnline(...)` → `TaskRaceModeManager.Awake()` เห็น `Seed > 0` → `ConfigureRound(players, difficulty, seed)` + `LockSeed(seed)` → `Start()` ใช้ seed ที่ล็อก ไม่สุ่มทับ
ผลคือทุกเครื่องได้ **ใบงานชุดเดียวกัน ลำดับเดียวกัน** โดยไม่ต้องส่งรายการงานข้ามเน็ต (log: `online host/client seed N`)

### 10.4 `BobTaskRaceOnline` (ฝั่งฉาก)

- `Attach()` ถูกเรียกจาก `TaskRaceModeManager.Start()` เฉพาะเมื่อ `MatchRequest.Online` — โหมดออฟไลน์ไม่สร้าง component นี้เลย
- ส่งท่าฮีโร่ตัวเอง 10 Hz (`SendPose`), รับของคนอื่นแล้ว lerp ที่ 12 rad/s; ตัวละครระยะไกลใช้ `BobTaskRaceTeammates.Avatar(heroId)` (ร่างเดิมของเพื่อนบอท, ไม่มีปัญญา/ไม่เก็บงาน)
- poll สถานีฝั่งเรา 4 Hz: `Done` เปลี่ยนจาก false → true = ส่ง `bob.task` หนึ่งครั้ง; ฝั่งรับเรียก `ForceComplete()` และจำ `knownDone` เพื่อไม่ส่งกลับ
- สถานีที่สะอาดอยู่แล้วตั้งแต่เฟรมแรกถูกบันทึกเป็น baseline ไม่ใช่ event

### 10.5 ทดสอบสองเครื่อง (ทำได้เลย)

1. เปิด Unity Cloud Dashboard ให้ **Relay** และ **Lobby** = ON (Authentication anonymous ต้องเปิดอยู่แล้วจาก S02)
2. Build Windows (หรือ Android) หนึ่งชุด แล้วเปิดคู่กับ Editor — PlayerId ต้องต่างกัน (anonymous ต่อการติดตั้ง)
3. เครื่อง A: Home → Solo/Duo/Team → S14 → `ออนไลน์ 2–4 คน` → `สร้างห้อง` → ได้โค้ด 6 ตัว (Console: `[UGS] room up · lobby … · relay … · 4 slots`)
4. เครื่อง B: เข้าหน้าเดียวกัน → `กรอกรหัส` → `เข้าร่วมด้วยโค้ดนี้` (หรือ `เข้าห้องที่เปิดอยู่`) → ที่นั่งในห้องขึ้น 2/4 ทั้งสองฝั่ง
5. เครื่อง A กด `เริ่มแมตช์` → ทั้งสองเครื่องเข้า `TaskRace` พร้อมกัน; Console มี `online host seed N` ฝั่ง A และ `online client seed N` ฝั่ง B (เลขเดียวกัน)
6. เดินชนกันบนเกาะ: ตัวละครอีกฝ่ายตามมาแบบ lerp; เก็บงานจุดหนึ่ง → อีกเครื่องเห็นจุดนั้นสะอาดขึ้น
7. จบรอบ/กด back: host ลบ lobby (Console `[UGS] leave lobby` ไม่ควรมี error) — กลับเข้า S15 สร้างห้องใหม่ได้

### 10.6 ข้อจำกัดของเฟสนี้ (ตั้งใจ)

- **co-op บนเกาะของเจ้าของห้อง** แข่งกับคู่แข่งจำลอง (`BobTaskRaceRival`) — ยังไม่มีสองทีมจริง/คะแนนรวมบนเซิร์ฟเวอร์; ข้อความใน S15 เขียนตรง ๆ ว่า "ทุกคนอยู่ฝั่งเราและแข่งกับคู่แข่งจำลอง"
- ความสามารถ/เอฟเฟกต์ของฮีโร่คนอื่นไม่ถูกส่ง — เห็นแค่ร่างกับตำแหน่ง; งานที่อีกฝ่ายเก็บจะสะอาดขึ้นแต่ไม่มีแอนิเมชันผู้เล่นนั้น
- ไม่มี reconnect / host migration / late join หลังกดเริ่ม (คนที่เข้าห้องหลัง `bob.start` จะไม่ถูกโหลดเข้าแมตช์)
- ไม่มีการส่งผลจบแมตช์: แต่ละเครื่องจบเมื่อรายการฝั่งตัวเองครบหรือหมดเวลา; การ์ดผลจึงซ่อนปุ่ม `เล่นอีกครั้ง` เมื่อออนไลน์ และบอกให้กลับไปสร้างห้องใหม่
- เก็บงานเป็นแบบทางเดียว (false→true): การแกล้งทำให้สถานี Dirty (ฝั่งคู่แข่ง/หลังสะพานเปิด) ยังเป็นเรื่องของแต่ละเครื่อง ไม่ถูกส่งข้ามห้อง
- คะแนน/เหรียญยังคิดและบันทึกบนเครื่อง (Cloud Save เป็นรายผู้เล่น ไม่ใช่คะแนนห้อง)
- โควตา free tier ของ Relay/Lobby ตาม §5 — ห้องถูกทิ้งเมื่อ host ออก และไม่มี heartbeat ค้างหลัง Leave

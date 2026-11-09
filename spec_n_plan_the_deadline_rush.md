# THE DEADLINE RUSH — Tài liệu Triển khai & Đặc tả Kỹ thuật cho Agent Code

> **Nhóm 13** · Đồ án môn *Thiết kế và Phát triển Game* · ĐH Thuỷ Lợi · GVHD: ThS. Trương Xuân Nam  
> **Mục đích:** Tài liệu này là **Single Source of Truth (Nguồn sự thật duy nhất)** được thiết kế để **4 AI Coding Agent** (mỗi thành viên điều khiển 1 Agent) làm việc song song theo **Module (M1 → M14)**, không chia theo tuần và không xung đột code.

---

## 0. Nguyên tắc làm việc với AI Agent & Quy ước Kiến trúc

1. **Ranh giới thư mục nghiêm ngặt (Directory Ownership):** Mỗi Agent chỉ được tạo/sửa file trong thư mục thuộc phạm vi Module của mình. Không tự ý sửa code của Agent khác.
2. **Tách biệt Logic và Hiển thị:** Toàn bộ `RunState` và các System lõi (Agent A) là C# thuần (POCO) để chạy được EditMode Unit Test mà không cần mở Scene.
3. **Một nguồn trạng thái duy nhất (`RunState`):** Nghiêm cấm gán trực tiếp `energy`, `stress`, `progress`, `minute` từ UI hay World. Mọi thay đổi bắt buộc đi qua `ActionSystem` / `StatsSystem` / `TimeSystem`.
4. **Giao tiếp qua `EventBus`:** Agent B (UI), Agent C (World), Agent D (Atmosphere) chỉ đọc dữ liệu và lắng nghe sự kiện phát ra từ `EventBus`.
5. **Data-Driven 100%:** Toàn bộ hằng số, hành động, sự kiện, manh mối (Clue) và lời thoại phải đọc từ `ScriptableObject` hoặc `JSON/CSV`, không hard-code con số hay câu thoại trong script.
6. **Không tự thêm tính năng (No Scope Creep):** Agent làm đúng Acceptance Criteria của từng Module. Nếu phát hiện điểm chưa rõ, ghi vào mục *"Cần chốt"*, không tự ý bịa thêm cơ chế.

---

## 1. Phân chia 4 Agent theo Module & Quyền sở hữu Thư mục

| Agent | Thành viên phụ trách | Cụm Module | Thư mục sở hữu độc quyền |
|---|---|---|---|
| **Agent A** | **Thân Đức Minh** (`2351060467`) | **M1, M2, M3, M5, M6, M9, M14** *(Core Engine & Systems)* | `Assets/_Project/Scripts/Core/`<br>`Assets/_Project/Scripts/Systems/`<br>`Assets/_Project/Tests/EditMode/` |
| **Agent B** | **Lô Trà My** (`2351060469`) | **M7, M10, M12** *(UI Layer, Terminal Apps & Narrative Data)* | `Assets/_Project/Scripts/UI/`<br>`Assets/_Project/Data/` *(Dialogue, Clues, Attempts)*<br>`Assets/_Project/Prefabs/UI/` |
| **Agent C** | **Đặng Tuấn Nam** (`2351060470`) | **M4, M8** *(World, Player, Level & World Anomalies)* | `Assets/_Project/Scripts/World/`<br>`Assets/_Project/Scenes/`<br>`Assets/_Project/Prefabs/World/` |
| **Agent D** | **Phạm Ngọc Minh Nam** (`2351060471`) | **M11, M13 + QA** *(Atmosphere, Audio, Pixel Art & QA)* | `Assets/_Project/Scripts/Atmosphere/`<br>`Assets/_Project/Art/`<br>`Assets/_Project/Audio/` |

### Sơ đồ phụ thuộc & Thứ tự Build giữa các Agent

```text
[BƯỚC 0: Chốt Contracts.cs (15 phút đầu)]
       │
       ├──► Agent A (Core):       M1 ──► M2 ──► M3 ──► M5 ──► M6 ──► M9 ──► M14 (Integration)
       ├──► Agent B (UI & Data):  M12 (Data Schema) ──► M7 (Dialogue/Phone) ──► M10 (HUD/Computer/Menus)
       ├──► Agent C (World):      M4 (Player/Interactable/6 Areas/DoorLock) ──► M8 (World Events E01-E07)
       └──► Agent D (Art/Atmos):  M13a (Placeholder sớm) ──► M11 (Light2D/Audio/FX) ──► M13b (Final Art) ──► QA
```

---

## 2. Luật chơi & Bảng số liệu chuẩn (Single Source of Truth)

> Agent A đưa toàn bộ các con số dưới đây vào `GameConfig` và `ActionDefinition`. Không rải số trong code.

### 2.1 Trạng thái khởi tạo đầu Run (`RunState`)

| Biến | Kiểu | Giá trị đầu | Giới hạn & Quy tắc |
|---|---|---|---|
| `minute` | `int` | `0` (18:00) | `0..359` (`359` = 23:59 — tổng cộng **359 phút game**) |
| `progress` | `int` | `0` | `0..100` (%) |
| `energy` | `int` | `100` | `0..100` (Về `0` → **Game Over ngay lập tức**, ưu tiên hơn mọi Ending) |
| `stress` | `int` | `10` | `0..100` (Chạm `100` không Game Over; giữ ở `100` + cảnh báo đỏ HUD) |
| `currentArea` | `AreaId` | `Dorm` | `Dorm, Hallway, Library, Lab, Canteen, SecretArea` |
| `cluesThisRun` | `HashSet<string>` | Rỗng | Danh sách ID clue nhặt trong Run hiện tại |
| `attemptsOpenedThisRun` | `HashSet<int>` | Rỗng | Các tệp `ATTEMPT_xx` đã mở đọc trong Run hiện tại |
| `flags` | `HashSet<string>` | Rỗng | Cờ lựa chọn và trạng thái trong Run hiện tại |
| `firedEvents` | `HashSet<string>` | Rỗng | Các sự kiện có cờ `once = true` đã phát trong Run |
| `thresholdsApplied` | `HashSet<int>` | Rỗng | Đánh dấu mốc giờ (`240, 300, 330`) đã cộng Stress thụ động |
| `secretAreaOpened` | `bool` | `false` | Đã nhập đúng mật mã `2728` ở cửa cuối hành lang hay chưa |

**Dữ liệu giữ lại giữa các Run (`MemoryData` — lưu JSON tại `persistentDataPath`):**
- `unlockedAttempts` (`HashSet<int>`): Danh sách tệp `ATTEMPT_01..28` đã từng mở qua tất cả các lần chơi.
- `settings`: `musicVolume`, `sfxVolume`, `brightness`, `keybinds`.
- **Quy tắc sống còn:** Khi bấm *New Game*, `RunState` reset 100%, còn `MemoryData` giữ nguyên. Tệp ATTEMPT hay Clue từng mở ở Run trước **không** tính vào điều kiện Ending của Run mới (người chơi phải mở lại trong Run hiện tại).

### 2.2 Bảng thông số Hành động (`ActionDefinition`)

| ID | Hành động | Thời gian | Energy | Stress | Progress | Vị trí / Ghi chú |
|---|---|---:|---:|---:|---:|---|
| `act_code` | Code đồ án | `20′` | `-15` | `+3` | `+15` | Máy tính Phòng KTX |
| `act_research` | Tìm tài liệu | `15′` | `-5` | `+2` | `+5` | Thư viện (có thể ra Clue) |
| `act_eat` | Ăn uống | `15′` | `+20` | `-5` | `0` | Quầy Căn tin |
| `act_rest` | Nghỉ ngơi | `30′` | `+30` | `-10` | `0` | Giường Phòng KTX |
| `act_explore` | Khám phá (mỗi điểm) | `15′` | `-5..-10` | `+1..+3` | `0` | Khai báo cố định trên từng điểm khám phá |
| `act_inspect` | Tương tác đơn giản | `0′` | `0` | `0` | `0` | Xem đồ vật thông thường |
| `act_dialogue` | Hội thoại quan trọng | `0..5′` | `0` | `-2` (NPC thường) | `0` | Nói chuyện với NPC |

**Biến động Stress từ Sự kiện:**
- Phát hiện sự kiện bất thường: `+5..+10` Stress
- Đọc tin nhắn bí ẩn (`E02`): `+7` Stress
- Đọc file đáng sợ (`ATTEMPT` lần đầu trong Run): `+10` Stress
- Gặp sự kiện kinh dị trực tiếp (`E04, E05`): `+15` Stress

**Stress thụ động theo ngưỡng thời gian (Mỗi ngưỡng chỉ cộng 1 lần duy nhất trong Run):**
- Đồng hồ `>= 22:00` (`minute >= 240`, còn < 2 giờ): `+5` Stress
- Đồng hồ `>= 23:00` (`minute >= 300`, còn < 1 giờ): `+10` Stress
- Đồng hồ `>= 23:30` (`minute >= 330`, còn < 30 phút): `+15` Stress
*(Nếu 1 hành động nhảy qua nhiều ngưỡng cùng lúc → cộng dồn tất cả các ngưỡng vừa vượt qua).*

### 2.3 Ngưỡng trạng thái & Quy trình 10 bước xử lý Hành động

- **Energy ≤ 24:** Tốc độ di chuyển nhân vật giảm còn `×0.6`; icon pin HUD nhấp nháy + dòng chữ *"Bạn đang kiệt sức"*. **Energy = 0 → Game Over ngay lập tức.**
- **Stress:** `0–30` Bình thường · `31–60` Căng nhẹ · `61–80` Bất an (bắt đầu có hiệu ứng âm thanh/hình ảnh) · `81–100` Cảnh báo đỏ trên HUD, mở khóa sự kiện kinh dị đặc biệt.

**Thứ tự thực thi bắt buộc trong `ActionSystem` sau mỗi hành động:**
```text
1. Kiểm tra điều kiện CanPerform(state) (đúng khu vực, đối tượng, điều kiện tiên quyết)
2. Hiển thị UI xác nhận chi phí dự kiến (Thời gian, Energy, Stress, Progress) -> Chờ người chơi Confirm
3. Tính số phút thực tế: áp dụng luật cắt cửa sổ 23:50 (mục 2.4) và clamp tối đa ở 359 (23:59)
4. TimeSystem.Advance(minutes) -> Cập nhật đồng hồ & phát sự kiện OnTimeChanged
5. StatsSystem.ApplyDelta(dEnergy, dStress, dProgress) -> Clamp tất cả trong khoảng [0..100]
6. Kiểm tra các ngưỡng thời gian (240, 300, 330) mới vượt qua -> Cộng Stress tương ứng (1 lần/ngưỡng)
7. Nếu Energy == 0 -> Kích hoạt GAME OVER ngay lập tức (Dừng pipeline, không xét Ending)
8. EventDirector.Evaluate(state) -> Lọc các sự kiện đủ điều kiện, chọn tối đa 1 sự kiện có priority cao nhất để phát
9. Nếu minute == 359 -> Gọi EndingResolver.Resolve(state, FinalChoice.None)
10. Phát sự kiện cập nhật toàn bộ HUD & Atmosphere
```

### 2.4 Luật cắt cửa sổ 23:50–23:59 & Bảng phân giải Ending (`EndingResolver`)

- **Luật cắt 23:50 (`minute = 350`):** Khi người chơi **đã đủ điều kiện mở lựa chọn True Ending** (`CoreClueCount == 3 && secretAreaOpened == true`), nếu thực hiện một hành động ở `< 350` mà cộng thời gian sẽ vượt quá `350` → **tự động cắt thời gian dừng đúng ở `350` (23:50)** để người chơi không bao giờ bị nhảy vọt qua cửa sổ lựa chọn cuối. Từ `23:50`, đi bộ (`0 phút`) tới máy tính KTX để chọn.

| Thứ tự ưu tiên | Điều kiện kiểm tra | Kết quả |
|---:|---|---|
| **0** | `energy <= 0` tại bất kỳ thời điểm nào | **Game Over** |
| **1** | Trong Run này có đủ `3 Clue cốt lõi` + `secretAreaOpened == true` + Chọn *"Xóa bản sao dự án và ngắt kết nối"* trong khoảng `23:50–23:59` (`minute 350..359`) | **True Ending** |
| **2** | Trong Run này có `>= 3 Clue phụ` + Đã mở đọc `ATTEMPT_27` trong Run này (và không đạt True) | **Secret Ending** |
| **3** | `progress == 100` (và không đạt True / Secret) | **Bad Ending** |
| **4** | Các trường hợp còn lại khi chạm `23:59` (hoặc chọn *"Nộp đồ án"* khi `progress < 100`) | **Normal Ending** |

### 2.5 Danh sách Sự kiện chuẩn (`E01` – `E11`)

| ID | Tên sự kiện | Điều kiện kích hoạt (`Conditions`) | Hiệu ứng (`Effects`) | Priority | Once |
|---|---|---|---|---:|:-:|
| `E01` | Bất thường đầu tiên | `(progress >= 30 \|\| minute >= 120) && InArea(Dorm)` | Đồ vật đổi chỗ, âm lạ; `Stress +5..10` khi kiểm tra | `30` | `true` |
| `E02` | Tin nhắn bí ẩn | `progress >= 50` | Điện thoại báo *"Đừng hoàn thành nó."*; `Stress +7`; Lựa chọn: `Xóa / Trả lời / Điều tra` | `50` | `true` |
| `E03` | NPC đổi lời / biến mất | `progress >= 70` | Mở thoại NPC bản lệch; cho phép mở danh sách ATTEMPT | `40` | `true` |
| `E04` | Tiếng gõ cửa | `(progress >= 85 \|\| stress >= 81) && InArea(Hallway)` | Phát SFX gõ cửa khi hành lang trống; `Stress +15` | `60` | `true` |
| `E05` | Bóng người hành lang | `(progress >= 85 \|\| stress >= 81) && InArea(Hallway)` | Hiện bóng người cuối hành lang (biến thể theo flag); `Stress +15` | `60` | `true` |
| `E06` | Máy tự mở ATTEMPT | `progress >= 95 \|\| (stress >= 81 && InArea(Lab))` | Máy tính tự bật file ATTEMPT, đổi môi trường; `Stress +10` | `70` | `true` |
| `E07` | Project Completed | `progress == 100` | Máy tính hiện *"PROJECT COMPLETED — SUBMISSION SUCCESSFUL"* + tạo `ATTEMPT #28` (game vẫn tiếp tục tới 23:59) | `80` | `true` |
| `E08` | Tìm thấy mật mã | `minute >= 240 && Action(act_explore_library)` | Nhận `clue_core_passcode` (`2728`) | `45` | `true` |
| `E09` | Mở Khu vực bí mật | `HasClue(clue_core_passcode) && InputPasscode("2728")` | `secretAreaOpened = true`, mở cửa sang `SecretArea` | `45` | `true` |
| `E10` | Lựa chọn cuối cùng | `minute >= 350 && CoreClueCount == 3 && secretAreaOpened` | Trên máy tính hiện 2 lựa chọn: *"Nộp đồ án"* vs *"Xóa bản sao dự án và ngắt kết nối"* | `100` | `true` |
| `E11` | Cảnh báo mốc giờ | `minute >= 240 / 300 / 330` | Hiện thông báo HUD + cộng Stress theo mục 2.2 | `20` | `false` |

---

## 3. Hợp đồng Code dùng chung (`Contracts.cs` — Chốt ở Bước 0)

> **Bắt buộc:** Agent A tạo file `Assets/_Project/Scripts/Core/Contracts.cs` này trước tiên và commit lên nhánh `dev`. Mọi Agent B, C, D chỉ gọi các Enum, Struct và Event trong file này để không bao giờ xảy ra lỗi lệch tên hàm.

```csharp
using System;
using System.Collections.Generic;

namespace DeadlineRush.Core
{
    public enum WorldPhase { Phase1_18_20 = 1, Phase2_20_22 = 2, Phase3_22_23 = 3, Phase4_23_24 = 4 }
    public enum AreaId { Dorm, Hallway, Library, Lab, Canteen, SecretArea }
    public enum EndingType { None, GameOver, Normal, Bad, Secret, True }
    public enum FinalChoice { None, SubmitProject, DeleteAndDisconnect }
    public enum ConditionType
    {
        TimeRange, ProgressMin, StressMin, HasClue,
        CoreClueCountMin, MinorClueCountMin, InArea, HasFlag, AttemptOpened
    }

    [Serializable]
    public struct Condition
    {
        public ConditionType type;
        public int intValue;
        public int intValueMax;
        public string stringValue;
        public AreaId areaValue;
    }

    [Serializable]
    public class ActionCostPreview
    {
        public string actionId;
        public string displayName;
        public int minutes;
        public int dEnergy;
        public int dStress;
        public int dProgress;
    }

    /// <summary>
    /// EventBus trung tâm: Agent A phát sự kiện (Raise), Agent B/C/D đăng ký lắng nghe (Subscribe).
    /// </summary>
    public static class EventBus
    {
        public static event Action<int, WorldPhase> OnTimeChanged;               // (minute, currentPhase)
        public static event Action<int, int, int> OnStatsChanged;                // (energy, stress, progress)
        public static event Action<AreaId> OnAreaChanged;                        // (newArea)
        public static event Action<string, bool> OnClueAdded;                    // (clueId, isCore)
        public static event Action<int> OnAttemptOpened;                         // (attemptNumber 1..28)
        public static event Action<string> OnGameEventTriggered;                 // (eventId: E01..E11)
        public static event Action<ActionCostPreview, Action> OnActionConfirmRequested; // (preview, onConfirmCallback)
        public static event Action<string, bool> OnDialogueRequested;            // (dialogueGroupId, isMandatory)
        public static event Action<bool> OnUIModalStateChanged;                  // (isAnyModalOpen -> khoá Player move)
        public static event Action<EndingType> OnGameEnded;                      // (endingType)

        public static void RaiseTimeChanged(int min, WorldPhase phase) => OnTimeChanged?.Invoke(min, phase);
        public static void RaiseStatsChanged(int e, int s, int p) => OnStatsChanged?.Invoke(e, s, p);
        public static void RaiseAreaChanged(AreaId area) => OnAreaChanged?.Invoke(area);
        public static void RaiseClueAdded(string id, bool isCore) => OnClueAdded?.Invoke(id, isCore);
        public static void RaiseAttemptOpened(int num) => OnAttemptOpened?.Invoke(num);
        public static void RaiseGameEventTriggered(string evtId) => OnGameEventTriggered?.Invoke(evtId);
        public static void RaiseActionConfirmRequested(ActionCostPreview p, Action confirm) => OnActionConfirmRequested?.Invoke(p, confirm);
        public static void RaiseDialogueRequested(string group, bool mandatory = false) => OnDialogueRequested?.Invoke(group, mandatory);
        public static void RaiseUIModalStateChanged(bool isOpen) => OnUIModalStateChanged?.Invoke(isOpen);
        public static void RaiseGameEnded(EndingType ending) => OnGameEnded?.Invoke(ending);
    }
}
```

---

## 4. Đặc tả Kỹ thuật & Cách code từng Module (M1 → M14)

---

### PHẦN 1: AGENT A — CORE ENGINE & SYSTEMS (`Thân Đức Minh`)

#### M1 — Foundation (`Scripts/Core/`)
* **Files cần tạo:** `RunState.cs`, `MemoryData.cs`, `GameConfig.cs`, `SaveService.cs`, `GameManager.cs`.
* **Hướng dẫn code:**
  * `RunState`: Class C# thuần (không kế thừa `MonoBehaviour`). Chứa đầy đủ các trường ở bảng 2.1, các thuộc tính chỉ đọc `CoreClueCount`, `MinorClueCount`, và hàm `Reset()` đưa toàn bộ chỉ số về mặc định đầu Run (`18:00, Energy 100, Stress 10, Progress 0%`).
  * `MemoryData`: Class `[Serializable]` chứa `List<int> unlockedAttempts`, `float musicVolume = 0.8f`, `float sfxVolume = 0.8f`, `float brightness = 1.0f`.
  * `SaveService`: Ghi/đọc `MemoryData` ra file `memory_save.json` tại `Application.persistentDataPath`. Tuyệt đối không lưu `RunState`.
  * `GameManager`: Singleton quản lý State Machine (`MainMenu -> Playing -> Ending/GameOver`). Cung cấp `StartNewRun()`, `TriggerEnding(EndingType)`, `ReturnToMainMenu()`.
* **Acceptance Criteria (Xong khi):** Gọi `StartNewRun()` reset đúng `0 / 100 / 10 / 0%`; tắt/mở game giữ nguyên `MemoryData` và `Settings`; không lỗi console.

#### M2 — Time & Stats (`Scripts/Systems/TimeSystem.cs`, `StatsSystem.cs`)
* **Hướng dẫn code:**
  * `TimeSystem.GetPhase(int minute)`: `0..119` → Phase 1; `120..239` → Phase 2; `240..299` → Phase 3; `300..359` → Phase 4.
  * `TimeSystem.Advance(RunState s, int minutes)`:
    1. Kiểm tra **Luật cắt 23:50**: Nếu `s.CoreClueCount == 3 && s.secretAreaOpened && s.minute < 350 && (s.minute + minutes) > 350` → gán `s.minute = 350`. Ngược lại `s.minute = Math.Clamp(s.minute + minutes, 0, 359)`.
    2. Kiểm tra 3 ngưỡng `[240: +5, 300: +10, 330: +15]`: nếu `s.minute >= threshold` và `!s.thresholdsApplied.Contains(threshold)` → thêm vào `thresholdsApplied` và cộng dồn số Stress phạt.
    3. Gọi `EventBus.RaiseTimeChanged(s.minute, GetPhase(s.minute))`.
  * `StatsSystem.ApplyDelta(RunState s, int dE, int dS, int dP)`: Cộng và `Math.Clamp` cả 3 chỉ số trong `[0, 100]`. Gọi `EventBus.RaiseStatsChanged`. Nếu `s.energy <= 0`, gọi ngay `EventBus.RaiseGameEnded(EndingType.GameOver)`.
* **Acceptance Criteria (Xong khi):** EditMode Unit Test pass 100% cho: clamp `0..100`, nhảy qua 2 ngưỡng giờ cùng lúc cộng đủ Stress đúng 1 lần, `Energy = 0` kích hoạt Game Over trước mọi logic khác.

#### M3 — Action System (`Scripts/Systems/ActionDefinition.cs`, `ActionSystem.cs`)
* **Hướng dẫn code:**
  * `ActionDefinition` (`ScriptableObject`): Chứa `actionId, displayName, minutes, dEnergy, dStress, dProgress, requiredArea, conditions[]`. Tạo sẵn 6 asset cho 6 nhóm hành động ở mục 2.2.
  * `ActionSystem.RequestPerform(ActionDefinition def)`:
    * Kiểm tra `CanPerform(state)`. Nếu `def.minutes > 0`, gọi `EventBus.RaiseActionConfirmRequested(preview, () => ExecutePerform(def))`. Nếu `minutes == 0`, chạy thẳng `ExecutePerform(def)`.
    * Trong `ExecutePerform(def)`: Thực thi chính xác 10 bước ở mục 2.3.
* **Acceptance Criteria (Xong khi):** Thực hiện từng hành động trừ/cộng khớp từng con số ở bảng 2.2; có popup xác nhận trước khi trừ thời gian.

#### M5 — Condition & EventDirector (`Scripts/Systems/ConditionEvaluator.cs`, `EventDirector.cs`, `GameEventDefinition.cs`)
* **Hướng dẫn code:**
  * `ConditionEvaluator.Evaluate(Condition c, RunState s)`: Đánh giá từng loại `ConditionType`.
  * `EventDirector.Evaluate(RunState s)`:
    1. Duyệt danh sách `GameEventDefinition` (`E01` → `E11`), lọc các event thỏa mãn `(!evt.once || !s.firedEvents.Contains(evt.id))` và tất cả `conditions` đều `true`.
    2. Sắp xếp giảm dần theo `priority`.
    3. **Chỉ lấy 1 sự kiện có `priority` cao nhất**, thêm vào `s.firedEvents`, áp dụng hiệu ứng thay đổi state (cộng Stress, mở Clue) và phát `EventBus.RaiseGameEventTriggered(evt.id)`. Các sự kiện đủ điều kiện khác tự động chờ ở lần đánh giá kế tiếp.
* **Acceptance Criteria (Xong khi):** Unit Test chứng minh khi 2 sự kiện đủ điều kiện cùng lúc, chỉ sự kiện có `priority` cao hơn phát ra, sự kiện còn lại phát ở hành động kế tiếp; sự kiện `once` không bao giờ lặp lại.

#### M6 — Clue & ATTEMPT (`Scripts/Systems/ClueSystem.cs`)
* **Hướng dẫn code:**
  * Quản lý 3 Clue cốt lõi (`clue_core_passcode`, `clue_core_attempt_data`, `clue_core_loop_record`) và 6 Clue phụ.
  * Hàm `OpenAttempt(RunState s, MemoryData mem, int attemptNum)`:
    * Tệp `ATTEMPT_27` chỉ hiển thị/mở được khi `s.progress >= 70 || s.minute >= 240`.
    * Khi mở một tệp lần đầu trong Run: thêm vào `s.attemptsOpenedThisRun`, cộng `+10` Stress.
    * Thêm `attemptNum` vào `mem.unlockedAttempts` và lưu `SaveService.Save(mem)` ngay lập tức.
* **Acceptance Criteria (Xong khi):** Phân biệt đúng Clue cốt lõi và phụ; đọc `ATTEMPT_27` ghi nhận vào cả Run hiện tại lẫn Ký ức vĩnh viễn.

#### M9 — Ending & Final Choice (`Scripts/Systems/EndingResolver.cs`)
* **Hướng dẫn code:**
  * Viết hàm thuần `EndingType Resolve(RunState s, FinalChoice choice)` theo đúng 5 mức ưu tiên ở mục 2.4.
  * Khi người chơi chọn ở cửa sổ cuối (`E10`) hoặc khi `s.minute >= 359`, gọi `Resolve` và phát `EventBus.RaiseGameEnded(result)`.
* **Acceptance Criteria (Xong khi):** Unit Test pass cả 4 nhánh Ending + Game Over; kiểm chứng trường hợp `progress == 100` nhưng đủ điều kiện Secret (`>=3 Clue phụ + đọc ATTEMPT_27`) thì phải trả về `EndingType.Secret`.

---

### PHẦN 2: AGENT B — UI LAYER, TERMINAL APPS & NARRATIVE DATA (`Lô Trà My`)

#### M12 — Content Data (`Data/Dialogue/`, `Data/Clues/`, `Data/Attempts/`) — *Làm đầu tiên*
* **Files cần tạo:**
  * `Data/Dialogue/dialogues.csv`: Cột `id,groupId,speaker,text,minMinute,maxMinute,minStress,requiredFlag,choicesJson`.
  * `Data/Clues/clues.json`: Khai báo 3 Clue cốt lõi + 6 Clue phụ (`id, title, isCore, description`).
  * `Data/Attempts/attempts.json`: Nội dung chi tiết của 8 tệp quan trọng (`01, 05, 12, 20, 24, 26, 27, 28`) + template sinh tự động cho 20 tệp còn lại.
  * `Data/Endings/endings.json`: Tiêu đề và đoạn văn kết cho 4 Ending + Game Over.
* **Acceptance Criteria (Xong khi):** Toàn bộ file JSON/CSV parse thành công không lỗi; người viết kịch bản chỉnh sửa văn bản không cần mở C#.

#### M7 — Dialogue & Choice (`Scripts/UI/DialogueRunner.cs`, `PhoneUI.cs`)
* **Hướng dẫn code:**
  * `DialogueRunner`: Lắng nghe `EventBus.OnDialogueRequested(groupId, isMandatory)`. Chọn dòng thoại khớp điều kiện thời gian/Stress/Flag hiện tại.
  * Điều khiển lựa chọn dọc bằng `W/S` hoặc `↑/↓`, xác nhận bằng `E` hoặc `Enter`.
  * **Quy tắc chống né lựa chọn:** Khi `isMandatory == true` (tin nhắn bí ẩn `E02`, lựa chọn cuối `E10`), khóa phím `Esc` (không cho đóng hộp thoại hay mở Pause Menu).
  * `PhoneUI`: Khi nhận `E02`, hiển thị tin nhắn *"Đừng hoàn thành nó."* kèm 3 lựa chọn: `Xóa` (`flag_msg_delete`), `Trả lời` (`flag_msg_reply`), `Điều tra` (`flag_msg_investigate`). Khi mở UI, gọi `EventBus.RaiseUIModalStateChanged(true)`.
* **Acceptance Criteria (Xong khi):** Thoại NPC đổi đúng theo 3 giai đoạn; chọn phương án ghi đúng flag vào `RunState`; phím `Esc` bị chặn ở lựa chọn bắt buộc.

#### M10 — UI Layer (`Scripts/UI/HUDController.cs`, `ActionConfirmModal.cs`, `ComputerUI.cs`, `JournalUI.cs`, `MemoryUI.cs`, `PasscodeKeypadUI.cs`, `Menus.cs`)
* **Hướng dẫn code:**
  * `HUDController`: Hiển thị Đồng hồ (đổi màu cảnh báo ở `22:00 / 23:00 / 23:30`), Progress, Energy (nhấp nháy + hiện *"Bạn đang kiệt sức"* khi `<= 24`), Stress (viền đỏ cảnh báo khi `>= 81`), gợi ý `"E — Tương tác"`, và vùng Toast thông báo giữa màn hình.
  * `ActionConfirmModal`: Lắng nghe `OnActionConfirmRequested`, hiện rõ chi phí (`Thời gian, Energy, Stress, Progress`) trước khi người chơi bấm xác nhận.
  * `ComputerUI`: Màn hình máy tính gồm: nút thực hiện `Code`, thanh `Progress` (hiện *"PROJECT COMPLETED — SUBMISSION SUCCESSFUL"* + `ATTEMPT #28` khi đạt 100%), trình đọc 28 file `ATTEMPT`, và hộp lựa chọn cuối cùng (`Nộp đồ án` / `Xóa bản sao dự án và ngắt kết nối`) trong khung giờ `23:50–23:59`.
  * `PasscodeKeypadUI`: Bảng nhập mật mã 4 số (`2728`) cho cửa cuối hành lang.
  * **Tách bạch 2 màn hình:** `JournalUI` chỉ liệt kê Clue của Run hiện tại (chia 2 tab *Cốt lõi* và *Phụ*); `MemoryUI` chỉ liệt kê các tệp `ATTEMPT` trong `MemoryData.unlockedAttempts`.
  * `MainMenu & SettingsUI`: Chỉ có `New Game`, `Settings` (Music, SFX, Brightness, Controls), `Exit` (**tuyệt đối không có nút Continue**).
* **Acceptance Criteria (Xong khi):** Mọi thay đổi state phản ánh tức thì lên HUD; `MemoryUI` giữ nguyên dữ liệu sau khi bấm New Game còn `JournalUI` được làm rỗng.

---

### PHẦN 3: AGENT C — WORLD, PLAYER & LEVEL MECHANICS (`Đặng Tuấn Nam`)

#### M4 — World & Interaction (`Scripts/World/PlayerController.cs`, `Interactable.cs`, `AreaManager.cs`, `DoorLock.cs`)
* **Hướng dẫn code:**
  * `PlayerController`: Di chuyển 2D top-down 4 hướng (`WASD / Arrow Keys`) bằng `Rigidbody2D`. Lắng nghe `OnStatsChanged` để nhân tốc độ với `0.6f` khi `energy <= 24`. Lắng nghe `OnUIModalStateChanged(isOpen)` để khóa di chuyển khi đang mở UI/hộp thoại.
  * `Interactable` (Abstract Base Class): Phát hiện Player trong vùng Trigger, kiểm tra `CanInteract(RunState)`. Chỉ bật icon `"E"` khi trong tầm và đủ điều kiện. Khi bấm `E`, gọi `Interact()`.
  * Dựng 6 khu vực (`Dorm, Hallway, Library, Lab, Canteen, SecretArea`) trên cùng 1 Scene với hệ thống cửa chuyển vùng (`AreaTransition` tốn `0 phút`, gọi `EventBus.RaiseAreaChanged`).
  * Đặt đầy đủ đối tượng tương tác: Máy tính KTX, Giường, Điện thoại, Cửa ổ khóa cuối hành lang (`DoorLock` gọi `PasscodeKeypadUI` kiểm tra mã `"2728"`), Giá sách/Điểm tìm kiếm Thư viện (`>= 22:00` cho `clue_core_passcode`), Máy tính Phòng Lab, Quầy Căn tin, và Bản ghi vòng lặp trong Khu vực bí mật (`clue_core_loop_record`).
* **Acceptance Criteria (Xong khi):** Đi lại mượt qua 6 khu vực; icon `"E"` ẩn/hiện chuẩn xác; nhập đúng `"2728"` mở được cửa vào Khu vực bí mật.

#### M8 — Progression Events phần Thế giới (`Scripts/World/WorldAnomalyController.cs`, `NPCController.cs`)
* **Hướng dẫn code:**
  * Lắng nghe `EventBus.OnGameEventTriggered(eventId)` và `OnTimeChanged`:
    * `E01`: Dịch chuyển vị trí đồ vật trong Phòng KTX và bật điểm tương tác kiểm tra (`+5..10` Stress).
    * `E03`: `NPCController` (bạn cùng nhóm ở Căn tin / Hành lang) chuyển sang nhóm thoại lệch khi `progress >= 70` và biến mất hoàn toàn khi sang Phase 3 (`>= 22:00`).
    * `E04 / E05`: Khi ở Hành lang trống và `progress >= 85 || stress >= 81`, kích hoạt hiệu ứng tiếng gõ cửa từ phòng khóa hoặc bóng người đứng cuối hành lang.
    * `E06`: Màn hình máy tính tự động bật sáng và nhấp nháy tệp ATTEMPT khi `progress >= 95` hoặc `stress >= 81` tại Phòng máy.
* **Acceptance Criteria (Xong khi):** Toàn bộ sự kiện `E01` → `E09` có phản hồi hình ảnh/vật thể rõ ràng trong thế giới game và không xuất hiện ngẫu nhiên vô lý.

---

### PHẦN 4: AGENT D — ATMOSPHERE, ART/AUDIO & QA (`Phạm Ngọc Minh Nam`)

#### M13 — Placeholder sớm & Art/Audio Assets (`Art/`, `Audio/`)
* **Bước 1 (Làm ngay đầu dự án):** Xuất bộ Placeholder chuẩn kích thước (`Tile 16x16`, `Character 16x24`, icon `"E"`, khung UI cơ bản) để Agent C và Agent B ghép Scene ngay lập tức.
* **Bước 2 (Hoàn thiện):** Thay thế bằng Pixel Art hoàn chỉnh cho 6 khu vực, animation 4 hướng của nhân vật chính, NPC, prop và bộ âm thanh (4 bản nhạc nền theo Phase, ambience KTX, tiếng gõ cửa, tiếng thì thầm, âm báo điện thoại/máy tính).
* **Acceptance Criteria (Xong khi):** Thay thế toàn bộ placeholder mà không làm gãy liên kết Prefab/Scene.

#### M11 — Atmosphere (`Scripts/Atmosphere/AtmosphereController.cs`, `AudioManager.cs`)
* **Hướng dẫn code:**
  * `AtmosphereController` lắng nghe `OnTimeChanged(minute, phase)` và `OnStatsChanged(energy, stress, progress)` để điều khiển **2 trục độc lập**:
    1. **Trục Giai đoạn (`WorldPhase 1..4`):** Điều chỉnh màu sắc và cường độ `Global Light 2D` (`18:00–20:00` sáng ấm → `20:00–22:00` dịu xuống → `22:00–23:00` tối, đèn chập chờn → `23:00–23:59` rất tối).
    2. **Trục Tâm lý (`Stress 0..100`):** Điều chỉnh `URP Volume` (`Vignette`, `Film Grain`, `Chromatic Aberration` tăng mạnh khi `stress >= 61` và đạt cực đại ở `stress >= 81`).
  * `AudioManager`: Crossfade nhạc nền theo 4 Phase; trộn thêm kênh tiếng thì thầm/nhịp tim khi `stress >= 61`; đọc/ghi âm lượng từ `MemoryData.settings`.
* **Acceptance Criteria (Xong khi):** 4 khung giờ và 4 mức Stress tạo ra sự khác biệt thị giác/thính giác rõ rệt đúng mô tả GDD.

#### M14 — Integration & QA (Agent A chủ trì ghép nối, Agent D điều phối kiểm thử)
* Kết nối toàn bộ Prefab của 4 Agent vào `MainMenu.unity` và `Game.unity`.
* Chạy toàn bộ Checklist nghiệm thu ở mục 5 và đóng gói bản build Windows hoàn chỉnh.

---

## 5. Checklist Nghiệm thu Tổng (Definition of Done)

**A. Kiểm thử Tự động — EditMode Unit Tests (Agent A):**
- [ ] Clamp `Energy / Stress / Progress` luôn nằm trong `[0..100]`.
- [ ] Ngưỡng thời gian (`22:00, 23:00, 23:30`) cộng Stress đúng 1 lần duy nhất mỗi ngưỡng; nhảy qua 2 ngưỡng cùng lúc cộng đủ cả hai.
- [ ] `Energy == 0` kích hoạt `GameOver` ngay lập tức, ưu tiên trước cả `EndingResolver` dù xảy ra đúng lúc `23:59`.
- [ ] `EndingResolver` trả về đúng thứ tự ưu tiên `True > Secret > Bad > Normal` trên mọi tổ hợp trạng thái.
- [ ] Luật cắt thời gian ở `23:50` (`minute = 350`) hoạt động chính xác khi đã có đủ 3 Clue cốt lõi và mở Khu vực bí mật.
- [ ] `EventDirector`: Khi nhiều sự kiện thỏa điều kiện cùng lúc, chỉ phát 1 sự kiện có `priority` cao nhất; sự kiện `once` không bao giờ lặp lại.

**B. Kiểm thử Playtest Thủ công (Agent D điều phối):**
- [ ] Nút `"E"` chỉ hiện khi đứng trong tầm tương tác và thỏa mãn điều kiện.
- [ ] Bảng xác nhận hành động hiển thị chi phí khớp 100% với số thực tế bị trừ.
- [ ] Phím `Esc` không thể dùng để đóng hoặc né các lựa chọn cốt truyện bắt buộc (`E02`, `E10`).
- [ ] Chơi thử theo 4 chiến thuật mở được đủ **4 Ending (True, Secret, Bad, Normal)** + **Game Over** khi cạn Energy.
- [ ] Bấm *New Game* sau khi kết thúc Run: chỉ số về `100 / 10 / 0 / 18:00`, Nhật ký Clue trống, nhưng mục **Ký ức** vẫn giữ nguyên các tệp `ATTEMPT` đã mở.
- [ ] Tắt và mở lại game: `Settings` và `Ký ức` vẫn còn, Main Menu **không** có nút *Continue*.
- [ ] Bản build Windows chạy mượt trên ít nhất 2 máy tính khác nhau.

---

## 6. Bộ Prompt Mẫu giao việc trực tiếp cho 4 AI Agent

> Copy-paste đoạn prompt tương ứng dưới đây kèm theo file `TheDeadlineRush_Agent_Plan.md` này vào cửa sổ chat của từng AI Agent (Cursor / Cline / Claude Code / Copilot):

### Prompt cho Agent A (Thân Đức Minh — Core Systems)
```text
Bạn là Agent A phụ trách Core Engine & Systems của dự án game Unity 2D "The Deadline Rush".
Hãy đọc kỹ file `TheDeadlineRush_Agent_Plan.md` và triển khai lần lượt các module: M1 (Foundation), M2 (Time & Stats), M3 (Action System), M5 (Condition & EventDirector), M6 (Clue & ATTEMPT), và M9 (EndingResolver).

QUY TẮC BẮT BUỘC:
1. Chỉ tạo và sửa file trong `Assets/_Project/Scripts/Core/`, `Assets/_Project/Scripts/Systems/` và `Assets/_Project/Tests/EditMode/`. Tuyệt đối không sửa code UI hay World.
2. Tạo file `Contracts.cs` đầu tiên theo đúng Mục 3 của tài liệu để khóa interface cho các Agent khác.
3. Toàn bộ số liệu (359 phút, bảng hành động, ngưỡng Stress 22:00/23:00/23:30, luật cắt 23:50, độ ưu tiên 4 Ending) phải khớp 100% với Mục 2 của tài liệu.
4. Viết đầy đủ EditMode Unit Tests kiểm chứng toàn bộ các mục trong Checklist 5A.
```

### Prompt cho Agent B (Lô Trà My — UI & Narrative Data)
```text
Bạn là Agent B phụ trách UI Layer, Terminal Apps & Narrative Data của dự án game Unity 2D "The Deadline Rush".
Hãy đọc kỹ file `TheDeadlineRush_Agent_Plan.md` và triển khai lần lượt các module: M12 (Content Data JSON/CSV), M7 (Dialogue & Phone UI), và M10 (HUD, ActionConfirmModal, ComputerUI, PasscodeKeypadUI, JournalUI, MemoryUI, Menus).

QUY TẮC BẮT BUỘC:
1. Chỉ tạo và sửa file trong `Assets/_Project/Scripts/UI/`, `Assets/_Project/Data/` và `Assets/_Project/Prefabs/UI/`.
2. Chỉ giao tiếp với hệ thống lõi thông qua `Contracts.cs` và `EventBus`. Tuyệt đối không gán trực tiếp các biến trong `RunState`.
3. Tách biệt rõ `JournalUI` (Clue trong Run hiện tại) và `MemoryUI` (danh sách ATTEMPT lưu vĩnh viễn qua các Run).
4. Khóa phím Esc khi đang ở hộp thoại lựa chọn bắt buộc (`isMandatory == true`) và không tạo nút Continue ở Main Menu.
```

### Prompt cho Agent C (Đặng Tuấn Nam — World & Gameplay)
```text
Bạn là Agent C phụ trách World, Player & Level Mechanics của dự án game Unity 2D "The Deadline Rush".
Hãy đọc kỹ file `TheDeadlineRush_Agent_Plan.md` và triển khai các module: M4 (PlayerController, Interactable base, 6 khu vực, DoorLock mật mã 2728) và M8 (WorldAnomalyController, NPCController cho các sự kiện E01–E09).

QUY TẮC BẮT BUỘC:
1. Chỉ tạo và sửa file trong `Assets/_Project/Scripts/World/`, `Assets/_Project/Scenes/` và `Assets/_Project/Prefabs/World/`.
2. Nhân vật giảm tốc độ còn x0.6 khi `Energy <= 24` và tự động khóa di chuyển khi `EventBus.OnUIModalStateChanged(true)`.
3. Icon tương tác "E" chỉ hiển thị khi nhân vật đứng trong vùng Trigger VÀ thỏa mãn `CanInteract(RunState)`.
4. Đi bộ chuyển giữa 6 khu vực tốn 0 phút; mọi hành động tốn phút phải gọi qua `ActionSystem` của Agent A.
```

### Prompt cho Agent D (Phạm Ngọc Minh Nam — Atmosphere, Art/Audio & QA)
```text
Bạn là Agent D phụ trách Atmosphere, Art/Audio & QA của dự án game Unity 2D "The Deadline Rush".
Hãy đọc kỹ file `TheDeadlineRush_Agent_Plan.md` và triển khai các module: M13 (Placeholder sớm & Pixel Art/Audio hoàn chỉnh) và M11 (`AtmosphereController`, `AudioManager`).

QUY TẮC BẮT BUỘC:
1. Chỉ tạo và sửa file trong `Assets/_Project/Scripts/Atmosphere/`, `Assets/_Project/Art/` và `Assets/_Project/Audio/`.
2. Tạo bộ Sprite Placeholder (Tile 16x16, Character 16x24, icon E) trước tiên để Agent C dựng map không bị chặn.
3. Trong `AtmosphereController`, lắng nghe `EventBus.OnTimeChanged` và `OnStatsChanged` để điều khiển Global Light 2D theo 4 giai đoạn (18:00–20:00, 20:00–22:00, 22:00–23:00, 23:00–23:59) kết hợp hiệu ứng Vignette/Glitch/Audio tăng dần theo ngưỡng Stress (61 và 81).
```
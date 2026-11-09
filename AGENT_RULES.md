# THE DEADLINE RUSH — QUY TẮC LÀM VIỆC DÀNH CHO AI AGENT & THÀNH VIÊN
> **Dự án:** The Deadline Rush · Đồ án môn *Thiết kế và Phát triển Game* · Nhóm 13  
> **Tài liệu tham chiếu gốc:** `spec_n_plan_the_deadline_rush.md`

Tài liệu này quy định các **nguyên tắc bắt buộc (Hard Constraints)** mà toàn bộ 4 AI Coding Agent (Agent A, B, C, D) và các thành viên phụ trách phải tuân thủ tuyệt đối trong suốt quá trình phát triển dự án.

---

## 1. QUY TẮC QUẢN LÝ GIT & CHIẾN LƯỢC NHÁNH (GIT WORKFLOW)

### 1.1. Cấm tuyệt đối Push thẳng lên `dev` và `main`
- **KHÔNG ĐƯỢC PUSH TRỰC TIẾP** bất kỳ commit nào lên nhánh `main` hoặc `dev`.
  - Nhánh `main`: Chỉ chứa bản phát hành (Release/Milestone) ổn định, có tag phiên bản.
  - Nhánh `dev`: Nhánh tích hợp chung (Integration branch). Mọi thay đổi đưa vào `dev` **bắt buộc phải thông qua Pull Request (PR)** từ nhánh feature của từng Agent.

### 1.2. Quy ước đặt tên nhánh (Branching Strategy)
Mỗi Agent/Thành viên khi triển khai công việc phải checkout nhánh mới từ `dev` mới nhất:
- **Agent A (Core Engine & Systems):**  
  `feature/agent-a-m1-foundation`  
  `feature/agent-a-m2-time-stats`  
  `feature/agent-a-m3-actions`  
  `feature/agent-a-m5-events`  
  `feature/agent-a-m6-clues`  
  `feature/agent-a-m9-endings`
- **Agent B (UI Layer & Narrative Data):**  
  `feature/agent-b-m12-data-schema`  
  `feature/agent-b-m7-dialogue-phone`  
  `feature/agent-b-m10-hud-menus`
- **Agent C (World, Player & Level Mechanics):**  
  `feature/agent-c-m4-player-world`  
  `feature/agent-c-m8-world-anomalies`
- **Agent D (Atmosphere, Art/Audio & QA):**  
  `feature/agent-d-m13-placeholder-art`  
  `feature/agent-d-m11-atmosphere-audio`  
  `feature/agent-d-m13-final-art`
- Nhánh sửa lỗi chung: `fix/<tên-lỗi-ngắn-gọn>`

### 1.3. Quy trình Pull Request (PR) chuẩn
1. Cập nhật nhánh `dev` mới nhất về máy (`git pull origin dev`).
2. Rebase hoặc merge nhánh `dev` vào nhánh feature của mình trước khi mở PR:
   ```bash
   git checkout feature/agent-x-...
   git merge dev
   ```
3. Đảm bảo toàn bộ **EditMode Tests** chạy pass (không có lỗi compile, không đỏ test).
4. Mở PR vào nhánh `dev` trên GitHub:
   - Tiêu đề PR rõ ràng: `[Agent X] Hoàn thành Module Mx: <Mô tả ngắn>`
   - Đính kèm checklist nghiệm thu của Module theo tài liệu đặc tả.
5. Merge vào `dev` theo hình thức **Squash and Merge** hoặc **Create a merge commit** (tùy nhóm thống nhất).

### 1.4. Quy ước Commit Message
Tuân theo chuẩn **Conventional Commits**:
- `feat(core): khởi tạo RunState và GameManager (M1)`
- `feat(ui): thêm HUDController và ActionConfirmModal (M10)`
- `feat(world): di chuyển nhân vật và DoorLock mật mã (M4)`
- `test(core): thêm unit test cho TimeSystem và luật cắt 23:50`
- `fix(stats): sửa lỗi clamp Stress vượt quá 100`

---

## 2. RANH GIỚI THƯ MỤC NGHIÊM NGẶT (DIRECTORY OWNERSHIP)

Mỗi Agent chỉ được phép **tạo và chỉnh sửa file trong thư mục sở hữu độc quyền** của mình. Nghiêm cấm Agent can thiệp hoặc sửa code trong thư mục của Agent khác!

| Agent | Thành viên | Thư mục sở hữu độc quyền |
|---|---|---|
| **Agent A** | Thân Đức Minh | `Assets/_Project/Scripts/Core/`<br>`Assets/_Project/Scripts/Systems/`<br>`Assets/_Project/Tests/EditMode/` |
| **Agent B** | Lô Trà My | `Assets/_Project/Scripts/UI/`<br>`Assets/_Project/Data/`<br>`Assets/_Project/Prefabs/UI/` |
| **Agent C** | Đặng Tuấn Nam | `Assets/_Project/Scripts/World/`<br>`Assets/_Project/Scenes/`<br>`Assets/_Project/Prefabs/World/` |
| **Agent D** | Phạm Ngọc Minh Nam | `Assets/_Project/Scripts/Atmosphere/`<br>`Assets/_Project/Art/`<br>`Assets/_Project/Audio/` |

> **Ngoại lệ duy nhất:** File `Assets/_Project/Scripts/Core/Contracts.cs` là **Hợp đồng chung**. File này được Agent A chốt ban đầu. Nếu các Agent khác cần bổ sung Event, Enum hay Struct, phải bàn bạc và do Agent A cập nhật thông qua PR.

---

## 3. NGUYÊN TẮC KIẾN TRÚC & LẬP TRÌNH (ARCHITECTURE & CODING STANDARDS)

1. **Một nguồn sự thật duy nhất (`RunState`):**
   - Không được phép tự ý gán hay can thiệp trực tiếp vào các biến `energy`, `stress`, `progress`, `minute` từ UI hoặc World scripts.
   - Mọi thay đổi trạng thái bắt buộc phải thông qua `ActionSystem`, `StatsSystem`, `TimeSystem` của Agent A.
2. **Giao tiếp hoàn toàn qua `EventBus`:**
   - Agent A phát sự kiện (`Raise`).
   - Agent B (UI), C (World), D (Atmosphere) chỉ đăng ký lắng nghe (`Subscribe`) và cập nhật hiển thị.
   - Không gọi chéo (tight coupling) giữa UI Component với World Component.
3. **Data-Driven 100%:**
   - Tuyệt đối không hardcode các thông số (phút, energy, stress, mật mã) hay câu thoại trực tiếp trong file code C#.
   - Toàn bộ chỉ số đọc từ `GameConfig`, `ActionDefinition` (ScriptableObject) hoặc JSON/CSV trong thư mục `Data/`.
4. **Logic thuần POCO cho Core Systems:**
   - Các class `RunState`, `ConditionEvaluator`, `EndingResolver` không kế thừa `MonoBehaviour` để có thể chạy EditMode Unit Tests tức thì.
5. **Bảo toàn File Metadata của Unity:**
   - Không bao giờ tự ý xoá hoặc tạo lại file `.meta` thủ công nếu không có lý do chính đáng để tránh làm hỏng GUID tham chiếu trong Scenes/Prefabs.

---

## 4. XỬ LÝ KHI GẶP ĐIỂM CHƯA RÕ (NO SCOPE CREEP)

- Nếu một Agent phát hiện điểm mơ hồ hoặc thiếu dữ kiện trong thiết kế game:
  - **KHÔNG ĐƯỢC TỰ Ý BỊA THÊM TÍNH NĂNG MỚI (NO SCOPE CREEP).**
  - Ghi chú lại thành câu hỏi trong danh sách *"Cần làm rõ"* và báo cho người điều phối/nhóm thảo luận thống nhất trước khi code.
- Mỗi module chỉ được coi là hoàn thành (Definition of Done) khi thỏa mãn 100% **Acceptance Criteria** được mô tả trong file `spec_n_plan_the_deadline_rush.md`.

---
*Tài liệu này có hiệu lực từ ngày 10/11/2026 và áp dụng cho toàn bộ quá trình phát triển dự án.*

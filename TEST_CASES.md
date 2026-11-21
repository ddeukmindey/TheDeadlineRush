# BỘ KỊCH BẢN KIỂM THỬ TỔNG HỢP (COMPREHENSIVE TEST CASES)
## DỰ ÁN GAME 2D: THE DEADLINE RUSH

> **Tài liệu tham chiếu:** `spec_n_plan_the_deadline_rush.md`  
> **Phiên bản:** 1.0 — Áp dụng cho nghiệm thu toàn diện (Agent A, B, C, D)  
> **Cấu trúc mã hóa Test Case:** `TC-[MODULE]-[SỐ_THỨ_TỰ]`  
> **Phân loại kiểm thử:** `Unit (Automated)` · `Integration` · `Manual E2E (Playtest)`

---

## MỤC LỤC
1. [Cụm 1: Chỉ số Cơ bản & Năng lượng (Stats & Energy)](#1-chỉ-số-cơ-bản--năng-lượng)
2. [Cụm 2: Hệ thống Thời gian & Luật cắt 23:50 (TimeSystem)](#2-hệ-thống-thời-gian--luật-cắt-2350)
3. [Cụm 3: Pipeline 10 Bước Thực thi Hành động (ActionSystem)](#3-pipeline-10-bước-thực-thi-hành-động)
4. [Cụm 4: Bộ Điều phối Sự kiện Câu chuyện (EventDirector & Conditions)](#4-bộ-điều-phối-sự-kiện-câu-chuyện)
5. [Cụm 5: Manh mối & Trình lưu trữ ATTEMPT (Clues & MemoryData)](#5-manh-mối--trình-lưu-trữ-attempt)
6. [Cụm 6: Bàn phím Mật mã Cửa cuối Hành lang (PasscodeKeypad)](#6-bàn-phím-mật-mã-cửa-cuối-hành-lang)
7. [Cụm 7: Giao diện HUD, Cảnh báo & Modal Lock (UI Layer)](#7-giao-diện-hud-cảnh-báo--modal-lock)
8. [Cụm 8: Màn hình Máy tính KTX & Hệ thống Menu (Computer & Menus)](#8-màn-hình-máy-tính-ktx--hệ-thống-menu)
9. [Cụm 9: Phân giải 5 Kết cục Game (EndingResolver)](#9-phân-giải-5-kết-cục-game)
10. [Cụm 10: Thế giới, Điều khiển Nhân vật & Không khí (World & Atmosphere)](#10-thế-giới-điều-khiển-nhân-vật--không-khí)

---

## 1. CHỈ SỐ CƠ BẢN & NĂNG LƯỢNG

| Test Case ID | Tên kịch bản | Tiền điều kiện | Các bước thực hiện | Kết quả kỳ vọng | Loại Test | Mức độ |
|---|---|---|---|---|:---:|:---:|
| `TC-STAT-01` | Khởi tạo RunState mặc định | Bắt đầu Run mới | Gọi `RunState.Reset()` hoặc New Game | • `minute = 0` (18:00)<br>• `energy = 100`<br>• `stress = 10`<br>• `progress = 0%`<br>• `currentArea = Dorm`<br>• Tất cả danh sách theo dõi đều rỗng | Unit | **Critical** |
| `TC-STAT-02` | Clamp Energy chặn dưới và trên | `energy = 10` | 1. Gọi `ApplyDelta(-50, 0, 0)`<br>2. Gọi `ApplyDelta(+200, 0, 0)` | • Sau bước 1: `energy = 0`<br>• Sau bước 2: `energy = 100` (không vượt quá 100) | Unit | **High** |
| `TC-STAT-03` | Clamp Stress trong [0..100] | `stress = 5` | 1. Gọi `ApplyDelta(0, -20, 0)`<br>2. Gọi `ApplyDelta(0, +150, 0)` | • Sau bước 1: `stress = 0`<br>• Sau bước 2: `stress = 100` (chạm 100 không crash, giữ ở 100) | Unit | **High** |
| `TC-STAT-04` | Clamp Progress trong [0..100] | `progress = 95` | 1. Gọi `ApplyDelta(0, 0, +20)`<br>2. Gọi `ApplyDelta(0, 0, -200)` | • Sau bước 1: `progress = 100` (%)<br>• Sau bước 2: `progress = 0` (%) | Unit | **High** |
| `TC-STAT-05` | Energy = 0 kích hoạt Game Over tức thì | `energy = 10` | Thực hiện hành động tốn `15` Energy | • `energy = 0`<br>• Trả về `alive = false`<br>• Phát sự kiện `EventBus.OnGameEnded(EndingType.GameOver)`<br>• Dừng toàn bộ pipeline, không xét các điều kiện Ending khác | Unit | **Critical** |
| `TC-STAT-06` | Game Over ưu tiên tại 23:59 dù đạt 100% Progress | `minute = 359`<br>`progress = 100`<br>`energy = 5` | Thực hiện hành động trừ `10` Energy | Kết quả trả về là **GameOver**, tuyệt đối KHÔNG trả về Bad Ending | Unit | **Critical** |
| `TC-STAT-07` | Nhận diện trạng thái kiệt sức | `energy = 25` | Trừ tiếp `1` Energy xuống `24` | `StatsSystem.IsExhausted()` trả về `true` (ngưỡng `<= 24`) | Unit | **Medium** |

---

## 2. HỆ THỐNG THỜI GIAN & LUẬT CẮT 23:50

| Test Case ID | Tên kịch bản | Tiền điều kiện | Các bước thực hiện | Kết quả kỳ vọng | Loại Test | Mức độ |
|---|---|---|---|---|:---:|:---:|
| `TC-TIME-01` | Phân đoạn 4 WorldPhase | `minute` biến thiên | Gọi `GetPhase(minute)` với: `0, 119, 120, 239, 240, 299, 300, 359` | • `0..119`: Phase 1 (18:00–19:59)<br>• `120..239`: Phase 2 (20:00–21:59)<br>• `240..299`: Phase 3 (22:00–22:59)<br>• `300..359`: Phase 4 (23:00–23:59) | Unit | **High** |
| `TC-TIME-02` | Định dạng chuỗi đồng hồ | `minute = 0, 120, 240, 350, 359` | Gọi `MinuteToDisplay(minute)` | Trả về chuỗi chính xác: `"18:00"`, `"20:00"`, `"22:00"`, `"23:50"`, `"23:59"` | Unit | **Medium** |
| `TC-TIME-03` | Cộng Stress thụ động tại mốc 22:00 | `minute = 230`<br>`stress = 10` | Tiến thời gian `15` phút (đến phút `245`) | • `minute = 245`<br>• `stress = 15` (cộng `+5` Stress)<br>• Thêm mốc `240` vào `thresholdsApplied` | Unit | **High** |
| `TC-TIME-04` | Không cộng Stress trùng lặp tại cùng 1 mốc | Đã vượt mốc `240` | Tiến tiếp thời gian `10` phút (đến phút `255`) | `stress` giữ nguyên, không cộng thêm Stress mốc `240` lần thứ 2 | Unit | **High** |
| `TC-TIME-05` | Nhảy cóc qua 2 mốc giờ cùng lúc | `minute = 230`<br>`stress = 10` | Tiến thời gian `75` phút (nhảy qua mốc `240` và `300`) | • `minute = 305`<br>• `stress` cộng dồn đủ `+5 (22:00) + 10 (23:00) = +15` (tổng `25`)<br>• Cả `240` và `300` đều được ghi nhận | Unit | **High** |
| `TC-TIME-06` | Nhảy cóc qua cả 3 mốc giờ | `minute = 200`<br>`stress = 10` | Tiến thời gian `140` phút (nhảy qua `240, 300, 330`) | • `stress` cộng dồn đủ `+5 + 10 + 15 = +30` (tổng `40`) | Unit | **High** |
| `TC-TIME-07` | Luật cắt 23:50 khi ĐỦ điều kiện True Ending | Có đủ `3 Clue cốt lõi`<br>`secretAreaOpened = true`<br>`minute = 340` | Thực hiện hành động tốn `20` phút (đáng lẽ đến `360`) | • Thời gian bị chặn dừng **đúng ở phút 350 (23:50)**<br>• Người chơi không bị nhảy lố qua cửa sổ lựa chọn cuối | Unit | **Critical** |
| `TC-TIME-08` | Không cắt 23:50 khi THIẾU điều kiện True Ending | Chỉ có `2 Clue cốt lõi`<br>`minute = 340` | Tiến thời gian `20` phút | `minute` nhảy thẳng lên `359` (clamp tối đa), không bị cắt ở `350` | Unit | **Critical** |
| `TC-TIME-09` | Không cắt 23:50 khi đã ở `>= 350` | Đủ điều kiện True<br>`minute = 351` | Tiến thời gian `5` phút | `minute = 356` (tiến bình thường) | Unit | **Medium** |

---

## 3. PIPELINE 10 BƯỚC THỰC THI HÀNH ĐỘNG

| Test Case ID | Tên kịch bản | Tiền điều kiện | Các bước thực hiện | Kết quả kỳ vọng | Loại Test | Mức độ |
|---|---|---|---|---|:---:|:---:|
| `TC-ACT-01` | Kiểm tra điều kiện khu vực (`CanPerform`) | Đang ở `Library` | Yêu cầu thực hiện `act_code` (yêu cầu ở `Dorm`) | `CanPerform()` trả về `false`, từ chối thực hiện | Unit | **High** |
| `TC-ACT-02` | Popup xác nhận chi phí với hành động có tốn giờ | Ở phòng KTX | Gọi `RequestPerform(act_code)` (`minutes = 20`) | Phát `EventBus.OnActionConfirmRequested` mang preview: `20 phút, -15 Energy, +3 Stress, +15 Progress`. Chờ xác nhận mới thực thi | Integration | **High** |
| `TC-ACT-03` | Thực thi ngay với hành động 0 phút | Đứng trước đồ vật | Gọi `RequestPerform(act_inspect)` (`minutes = 0`) | Chạy thẳng `ExecutePerform()`, không hiện popup xác nhận | Integration | **Medium** |
| `TC-ACT-04` | Số liệu chuẩn hành động Code đồ án | `E:100, S:10, P:0, M:0` | Xác nhận thực hiện `act_code` | `M: 20`, `E: 85` (-15), `S: 13` (+3), `P: 15` (+15%) | Unit / Playtest | **High** |
| `TC-ACT-05` | Số liệu chuẩn hành động Ăn uống | `E:50, S:40, P:30, M:60` | Xác nhận thực hiện `act_eat` | `M: 75`, `E: 70` (+20), `S: 35` (-5), `P: 30` (0) | Unit / Playtest | **High** |
| `TC-ACT-06` | Số liệu chuẩn hành động Nghỉ ngơi | `E:30, S:60, P:50, M:100` | Xác nhận thực hiện `act_rest` | `M: 130`, `E: 60` (+30), `S: 50` (-10), `P: 50` (0) | Unit / Playtest | **High** |
| `TC-ACT-07` | Chạm 23:59 sau hành động kích hoạt kết thúc | `minute = 345` | Thực hiện hành động `20` phút | `minute` lên `359` ➔ Tự động gọi `EndingResolver.Resolve(state, FinalChoice.None)` và phát `OnGameEnded` | Integration | **Critical** |

---

## 4. BỘ ĐIỀU PHỐI SỰ KIỆN CÂU CHUYỆN

| Test Case ID | Tên kịch bản | Tiền điều kiện | Các bước thực hiện | Kết quả kỳ vọng | Loại Test | Mức độ |
|---|---|---|---|---|:---:|:---:|
| `TC-EVT-01` | Lọc sự kiện có Priority cao nhất | Thỏa điều kiện cả `E_A` (priority 10) và `E_B` (priority 50) | Gọi `EventDirector.Evaluate(state)` | Chỉ duy nhất `E_B` được phát ra | Unit | **High** |
| `TC-EVT-02` | Sự kiện `once = true` không bao giờ lặp lại | Đã phát sự kiện `E01` (once) | Gọi tiếp `EventDirector.Evaluate(state)` ở lượt sau | `E01` không phát lại dù các chỉ số vẫn thỏa mãn | Unit | **High** |
| `TC-EVT-03` | Sự kiện Priority thấp được phát ở lượt kế tiếp | Có `E_High` (once, p=50) và `E_Low` (once, p=10) | 1. Evaluate lần 1<br>2. Evaluate lần 2 | • Lần 1: `E_High` phát<br>• Lần 2: `E_Low` phát sau khi `E_High` đã bị tiêu thụ | Unit | **High** |
| `TC-EVT-04` | Kích hoạt sự kiện E02 (Tin nhắn bí ẩn) | `progress >= 50` | Thực hiện hành động đưa Progress từ `45%` lên `55%` | Phát sự kiện `E02`: `Stress +7`, kích hoạt màn hình điện thoại rung chuông | Integration | **High** |
| `TC-EVT-05` | Kích hoạt sự kiện E07 (Project Completed) | `progress == 100` | Code đạt `100%` | Phát `E07`: Máy tính chuyển trạng thái hoàn thành + tạo `ATTEMPT #28` (game tiếp tục đến 23:59) | Integration | **Critical** |
| `TC-EVT-06` | Đánh giá Condition: InArea | Nhân vật ở `Dorm` | Kiểm tra điều kiện `InArea(Lab)` | Trả về `false` | Unit | **Medium** |
| `TC-EVT-07` | Đánh giá Condition: TimeRange | `minute = 240` | Kiểm tra điều kiện `TimeRange(min=240, max=0)` | Trả về `true` | Unit | **Medium** |

---

## 5. MANH MỐI & TRÌNH LƯU TRỮ ATTEMPT

| Test Case ID | Tên kịch bản | Tiền điều kiện | Các bước thực hiện | Kết quả kỳ vọng | Loại Test | Mức độ |
|---|---|---|---|---|:---:|:---:|
| `TC-CLUE-01` | Nhận diện Clue Cốt lõi vs Phụ | Rỗng | 1. Thêm `clue_core_passcode`<br>2. Thêm `clue_minor_strange_note` | • `CoreClueCount = 1`<br>• `MinorClueCount = 1` | Unit | **High** |
| `TC-CLUE-02` | Mở tệp ATTEMPT lần đầu trong Run | `stress = 10` | Gọi `OpenAttempt(state, memory, 1)` | • Mở thành công<br>• `stress = 20` (tăng `+10` Stress)<br>• Ghi vào cả `attemptsOpenedThisRun` và `memory.unlockedAttempts` | Unit | **High** |
| `TC-CLUE-03` | Mở lại tệp ATTEMPT lần 2 trong cùng Run | Đã mở `ATTEMPT_01` trong Run | Mở lại `ATTEMPT_01` | Mở đọc bình thường, **không cộng thêm Stress** | Unit | **Medium** |
| `TC-CLUE-04` | Điều kiện khóa tệp ATTEMPT_27 | `progress = 50`<br>`minute = 200` | Thử mở `ATTEMPT_27` | Bị từ chối mở (yêu cầu `progress >= 70` hoặc `minute >= 240`) | Unit | **High** |
| `TC-CLUE-05` | Mở khóa ATTEMPT_27 thành công | `progress = 70` hoặc `minute = 240` | Mở `ATTEMPT_27` | Mở thành công, ghi nhận vào MemoryData | Unit | **High** |
| `TC-CLUE-06` | Ký ức vĩnh viễn qua các Run (MemoryData) | Đã mở `ATTEMPT_05` ở Run 1 | Bấm New Game (bắt đầu Run 2) | • `RunState.attemptsOpenedThisRun` bị reset về rỗng<br>• `MemoryData.unlockedAttempts` vẫn còn lưu `ATTEMPT_05` | Unit / Playtest | **Critical** |

---

## 6. BÀN PHÍM MẬT MÃ CỬA CUỐI HÀNH LANG

| Test Case ID | Tên kịch bản | Tiền điều kiện | Các bước thực hiện | Kết quả kỳ vọng | Loại Test | Mức độ |
|---|---|---|---|---|:---:|:---:|
| `TC-PAD-01` | Nhập mã đúng "2728" | Đứng trước cửa khóa cuối hành lang | Nhập lần lượt các số `2 - 7 - 2 - 8` rồi bấm Enter | • Phát âm thanh mở khóa cơ khí<br>• Cập nhật `RunState.secretAreaOpened = true`<br>• Cửa mở sang `SecretArea`<br>• Đóng giao diện Keypad | Playtest | **Critical** |
| `TC-PAD-02` | Nhập mã sai | Màn hình Keypad đang mở | Nhập `1 - 2 - 3 - 4` rồi bấm Enter | • Hiển thị báo đỏ `"ERROR"` nhấp nháy<br>• Tự động xóa các ô số về trống `_ _ _ _`<br>• `secretAreaOpened` vẫn giữ nguyên là `false` | Playtest | **High** |
| `TC-PAD-03` | Nút xóa ký tự (Clear/Backspace) | Đã gõ `2 - 7` | Bấm nút Clear | Xóa ô số vừa nhập, cho phép nhập lại | Playtest | **Low** |

---

## 7. GIAO DIỆN HUD, CẢNH BÁO & MODAL LOCK

| Test Case ID | Tên kịch bản | Tiền điều kiện | Các bước thực hiện | Kết quả kỳ vọng | Loại Test | Mức độ |
|---|---|---|---|---|:---:|:---:|
| `TC-HUD-01` | Đồng hồ đổi màu cảnh báo theo 4 giai đoạn | Game đang chạy | Quan sát đồng hồ tại các mốc: `18:00, 22:00, 23:00, 23:30` | • `< 22:00`: Chữ trắng<br>• `>= 22:00`: Chữ vàng cam<br>• `>= 23:00`: Chữ cam đỏ<br>• `>= 23:30`: Chữ đỏ rực nhấp nháy | Playtest | **High** |
| `TC-HUD-02` | Cảnh báo kiệt sức trên HUD | `energy` giảm xuống `<= 24` | Trừ Energy từ `25` xuống `24` | • Icon pin HUD nhấp nháy liên tục<br>• Hiện dòng chữ đỏ *"Bạn đang kiệt sức"*<br>• Tốc độ di chuyển Player giảm còn `×0.6` | Playtest | **High** |
| `TC-HUD-03` | Cảnh báo Stress nguy cấp | `stress` tăng lên `>= 81` | Tăng Stress lên `85` | • Xuất hiện viền đỏ (Red Vignette) chập chờn bao quanh màn hình | Playtest | **High** |
| `TC-HUD-04` | Khóa di chuyển khi Modal mở | Player đang đi lại tự do | Mở bất kỳ Modal nào (Computer, Keypad, Dialog, Confirm) | • `EventBus.OnUIModalStateChanged(true)` phát ra<br>• PlayerController bị khóa di chuyển, không thể di chuyển nhân vật | Integration / Playtest | **Critical** |
| `TC-HUD-05` | Mở khóa di chuyển khi Modal đóng | Modal đang mở | Bấm nút Đóng / Hủy Modal | • `EventBus.OnUIModalStateChanged(false)` phát ra<br>• PlayerController khôi phục di chuyển bình thường | Integration / Playtest | **Critical** |
| `TC-HUD-06` | Chống né lựa chọn bắt buộc (Anti-Escape) | Nhận tin nhắn `E02` hoặc lựa chọn cuối `E10` (`isMandatory == true`) | Bấm phím `Esc` liên tục | • Phím `Esc` bị vô hiệu hóa hoàn toàn<br>• Không thể đóng popup, không thể mở Pause Menu<br>• Bắt buộc người chơi phải chọn 1 phương án | Playtest | **Critical** |

---

## 8. MÀN HÌNH MÁY TÍNH KTX & HỆ THỐNG MENU

| Test Case ID | Tên kịch bản | Tiền điều kiện | Các bước thực hiện | Kết quả kỳ vọng | Loại Test | Mức độ |
|---|---|---|---|---|:---:|:---:|
| `TC-COMP-01` | Giao diện Code trên máy tính | Mở máy tính KTX | Bấm nút *"Code đồ án"* | Hiện popup xác nhận `act_code` ➔ Xác nhận ➔ Thanh Progress trên màn hình máy tính tăng `+15%` | Playtest | **High** |
| `TC-COMP-02` | Màn hình máy tính khi đạt 100% Progress | `progress = 100` | Quan sát máy tính | Chuyển sang hiệu ứng màn hình lỗi đỏ, hiện dòng chữ *"PROJECT COMPLETED — SUBMISSION SUCCESSFUL — ATTEMPT #28"* | Playtest | **High** |
| `TC-COMP-03` | Cửa sổ lựa chọn cuối cùng (23:50–23:59) | `minute >= 350`<br>Đủ điều kiện True Ending | Mở máy tính KTX | Xuất hiện hộp thoại 2 lựa chọn: `[1] Nộp đồ án` vs `[2] Xóa bản sao dự án và ngắt kết nối`. Khóa phím `Esc` | Playtest | **Critical** |
| `TC-MENU-01` | Tuyệt đối không có nút Continue | Mở MainMenu | Quan sát danh sách nút | Chỉ có: `New Game`, `Settings`, `Exit`. **Không có nút Continue** | Playtest | **Critical** |
| `TC-MENU-02` | New Game reset sạch chỉ số | Đang ở Run cũ (`E:50, S:80, P:60`) | Thoát ra Menu rồi bấm `New Game` | Chỉ số về lại chính xác `100 / 10 / 0 / 18:00`. Sổ tay Clue rỗng | Playtest | **Critical** |
| `TC-MENU-03` | Lưu và tải Settings âm lượng / độ sáng | Đang ở Menu Settings | Kéo Music `0.5`, SFX `0.3`, thoát game và mở lại | Âm lượng tải lại đúng giá trị `0.5` và `0.3` từ file `memory_save.json` | Integration | **Medium** |

---

## 9. PHÂN GIẢI 5 KẾT CỤC GAME

| Test Case ID | Kịch bản Ending | Điều kiện kích hoạt | Lựa chọn cuối | Kết quả phân giải kỳ vọng | Mức độ |
|---|---|---|---|:---:|:---:|
| `TC-END-01` | **Game Over** | `energy <= 0` tại bất kỳ thời điểm nào | Bất kỳ | `EndingType.GameOver` (ưu tiên tuyệt đối số 0) | **Critical** |
| `TC-END-02` | **True Ending** | Đủ `3 Clue cốt lõi` + `secretAreaOpened = true` + `minute 350..359` | Chọn *"Xóa bản sao dự án và ngắt kết nối"* | `EndingType.True` (ưu tiên số 1) | **Critical** |
| `TC-END-03` | **Secret Ending** | Có `>= 3 Clue phụ` + Đã đọc `ATTEMPT_27` trong Run (không đạt True) | Không chọn Xóa | `EndingType.Secret` (ưu tiên số 2) | **Critical** |
| `TC-END-04` | **Bad Ending** | `progress == 100` (không đạt True, không đạt Secret) | Chọn Nộp đồ án hoặc hết giờ | `EndingType.Bad` (ưu tiên số 3) | **Critical** |
| `TC-END-05` | **Normal Ending** | Chạm `23:59` (`minute = 359`) khi `progress < 100` (hoặc chọn Nộp khi chưa đủ 100%) | Không đạt các điều kiện trên | `EndingType.Normal` (ưu tiên số 4) | **Critical** |
| `TC-END-06` | **Secret đè bẹp Bad** | `progress == 100` NHƯNG có `>= 3 Clue phụ` + đọc `ATTEMPT_27` | Hết giờ | Trả về `EndingType.Secret` (Secret ưu tiên cao hơn Bad) | **Critical** |
| `TC-END-07` | **True đè bẹp tất cả** | Đạt cùng lúc cả điều kiện True, Secret và Bad | Chọn *"Xóa bản sao dự án..."* | Trả về `EndingType.True` | **Critical** |
| `TC-END-08` | **GameOver đè bẹp True** | Đủ điều kiện True nhưng đúng lúc đó `energy == 0` | Bất kỳ | Trả về `EndingType.GameOver` | **Critical** |

---

## 10. THẾ GIỚI, ĐIỀU KHIỂN NHÂN VẬT & KHÔNG KHÍ

| Test Case ID | Tên kịch bản | Tiền điều kiện | Các bước thực hiện | Kết quả kỳ vọng | Loại Test | Mức độ |
|---|---|---|---|---|:---:|:---:|
| `TC-WLD-01` | Di chuyển nhân vật 4 hướng | Trong Scene gameplay | Nhấn các phím `W, A, S, D` hoặc phím mũi tên | Nhân vật di chuyển mượt mà theo 4 hướng top-down với vận tốc chuẩn | Playtest | **High** |
| `TC-WLD-02` | Giảm tốc độ di chuyển khi kiệt sức | `energy <= 24` | Di chuyển nhân vật | Vận tốc di chuyển giảm còn đúng `×0.6` so với bình thường | Playtest | **High** |
| `TC-WLD-03` | Hiện icon tương tác "E" trong tầm | Nhân vật đứng xa đồ vật | Đi lại gần bàn máy tính hoặc giường ngủ | • Khi vào vùng Trigger: Icon `"E — Tương tác"` bật sáng trên HUD<br>• Khi đi ra xa: Icon tự động ẩn đi | Playtest | **High** |
| `TC-WLD-04` | Chuyển vùng giữa 6 khu vực tốn 0 phút | Đang ở `Dorm` (`minute = 60`) | Đi qua cửa sang `Hallway` | • Nhân vật chuyển sang khu vực `Hallway`<br>• `RunState.currentArea` đổi thành `Hallway`<br>• `minute` giữ nguyên là `60` (di chuyển tốn 0 phút) | Integration / Playtest | **High** |
| `TC-WLD-05` | Sự kiện E03 (Bạn cùng nhóm biến mất) | Ban đầu NPC đứng ở Căn tin | Thời gian nhảy sang Phase 3 (`>= 22:00`) | NPC bạn cùng nhóm biến mất hoàn toàn khỏi vị trí | Playtest | **Medium** |
| `TC-ATM-01` | Ánh sáng 2D thay đổi theo 4 giai đoạn | Game tiến triển theo thời gian | Quan sát `Global Light 2D` qua các Phase | • Phase 1: Sáng ấm<br>• Phase 2: Dịu tối dần<br>• Phase 3: Rất tối, đèn chập chờn<br>• Phase 4: Đêm muộn, u ám | Playtest | **Medium** |
| `TC-ATM-02` | Hiệu ứng tâm lý khi Stress cao | `stress >= 81` | Đẩy Stress lên cao | Bật hiệu ứng Vignette tối viền, Film Grain hạt nhiễu, âm thanh tiếng tim đập dồn dập | Playtest | **Medium** |

---

## 11. BẢNG TỔNG HỢP TIẾN ĐỘ THỰC THI TEST

| Nhóm Kiểm Thử | Tổng số Case | Đã có Unit Test | Cần Playtest Thủ công | Trạng thái hiện tại |
|---|:---:|:---:|:---:|:---:|
| **1. Chỉ số & Năng lượng** | 7 | 7 | 1 | ✅ **PASS 100%** |
| **2. Thời gian & Cắt 23:50** | 9 | 9 | 1 | ✅ **PASS 100%** |
| **3. Pipeline Hành động** | 7 | 5 | 7 | ✅ Sẵn sàng |
| **4. EventDirector & Câu chuyện** | 7 | 7 | 2 | ✅ **PASS 100%** |
| **5. Clue & ATTEMPT** | 6 | 6 | 2 | ✅ **PASS 100%** |
| **6. Bàn phím Mật mã 2728** | 3 | 1 | 3 | ✅ Sẵn sàng (chờ Scene) |
| **7. Giao diện HUD & Cảnh báo** | 6 | 0 | 6 | ✅ Sẵn sàng (chờ Scene) |
| **8. Máy tính KTX & Menus** | 3 | 0 | 3 | ✅ Sẵn sàng (chờ Scene) |
| **9. Phân giải 5 Ending** | 8 | 8 | 4 | ✅ **PASS 100%** |
| **10. Thế giới & Không khí** | 7 | 0 | 7 | ⏳ Chờ Agent C & D dựng Scene |
| **TỔNG CỘNG** | **63** | **43 test tự động** (chạy thành 64 asserts) | **36 case nghiệm thu** | **Core logic 100% Xanh** |

---
*Tài liệu này được tạo làm căn cứ nghiệm thu chính thức cho đồ án môn học Nhóm 13.*

# The Deadline Rush

> **Đồ án môn *Thiết kế và Phát triển Game*** · **Nhóm 13** · Trường Đại học Thủy Lợi  
> **Giảng viên hướng dẫn:** ThS. Trương Xuân Nam  
> **Engine:** Unity `6000.6.8f1` (Unity 6 · Universal Render Pipeline 2D · New Input System)

**The Deadline Rush** là game 2D top-down kết hợp quản lý thời gian, khám phá và trinh thám tâm lý. Người chơi nhập vai sinh viên chạy deadline đồ án trong khung giờ `18:00 – 23:59` (`359` phút game), cân bằng giữa tiến độ hoàn thành, năng lượng (`Energy`), căng thẳng (`Stress`), đồng thời thu thập manh mối và tệp nhật ký `ATTEMPT` để giải mã vòng lặp thời gian với **5 kết cục** (`True`, `Secret`, `Bad`, `Normal`, `GameOver`).

---

## Thành viên & Phân công Module (`M1` – `M14`)

| Vai trò | Thành viên (MSSV) | Phạm vi Module | Thư mục sở hữu chính |
|---|---|---|---|
| **Agent A** | **Thân Đức Minh** (`2351060467`) | Core Engine, Systems, Integration (`M1–M3, M5, M6, M9, M14`) | `Assets/_Project/Scripts/Core/`<br>`Assets/_Project/Scripts/Systems/`<br>`Assets/_Project/Tests/EditMode/` |
| **Agent B** | **Lô Trà My** (`2351060469`) | Narrative Data, Dialogue, UI Layer (`M7, M10, M12`) | `Assets/_Project/Scripts/UI/`<br>`Assets/_Project/Data/`<br>`Assets/_Project/Prefabs/UI/` |
| **Agent C** | **Đặng Tuấn Nam** (`2351060470`) | World, Player, Level & Anomalies (`M4, M8`) | `Assets/_Project/Scripts/World/`<br>`Assets/_Project/Scenes/`<br>`Assets/_Project/Prefabs/World/` |
| **Agent D** | **Phạm Ngọc Minh Nam** (`2351060471`) | Atmosphere, Art/Audio & QA (`M11, M13, QA`) | `Assets/_Project/Scripts/Atmosphere/`<br>`Assets/_Project/Art/`<br>`Assets/_Project/Audio/` |

---

## Cấu trúc dự án

```text
TheDLRush/
├── .github/workflows/unity-ci.yml   # GitHub Actions CI/CD (Test, Build, Release)
├── Assets/
│   ├── Editor/                      # BuildScript.cs, AssetImportValidator.cs
│   ├── Scenes/                      # SampleScene.unity
│   ├── Settings/                    # Cấu hình URP 2D / PC / Mobile
│   ├── Tests/
│   │   ├── EditMode/                # GameDataValidationTests.cs
│   │   └── PlayMode/                # SmokeTests.cs
│   └── _Project/
│       ├── Art/                     # Atmosphere config, VolumeProfile, Placeholders
│       ├── Audio/                   # Ambience, Music, SFX (quản lý qua Git LFS)
│       ├── Data/                    # attempts.json, clues.json, endings.json, dialogues.csv
│       ├── Prefabs/                 # UI & World prefabs
│       ├── ScriptableObjects/       # Action & Event definitions
│       ├── Scripts/
│       │   ├── Core/                # Contracts.cs, EventBus, GameConfig, GameManager, RunState, SaveService
│       │   ├── Systems/             # TimeSystem, StatsSystem, ActionSystem, ClueSystem, EventDirector, EndingResolver
│       │   ├── UI/                  # HUD, DialogueRunner, PhoneUI, ComputerUI, PasscodeKeypadUI, JournalUI, MemoryUI, Menus
│       │   └── Atmosphere/          # AtmosphereController, AudioManager, AtmosphereConfig
│       └── Tests/EditMode/          # CoreSystemsTests.cs (64 unit tests)
├── Packages/manifest.json           # Package dependencies (URP 17.6.0, InputSystem 1.20.1, UGUI 2.6.0...)
├── ProjectSettings/                 # Cấu hình Unity 6000.6.8f1
├── .editorconfig                    # Quy chuẩn format & đặt tên C#
├── .gitattributes                   # Git LFS & UnityYAML merge rules
├── .gitignore                       # Chuẩn Unity (.meta được giữ lại)
├── version.json                     # Phiên bản build (major.minor.patch.buildNumber)
├── AGENT_RULES.md                   # Quy tắc Git workflow & ranh giới thư mục
├── spec_n_plan_the_deadline_rush.md # Đặc tả kỹ thuật tổng thể
└── TEST_CASES.md                    # Bộ kịch bản kiểm thử nghiệm thu
```

---

## Kiến trúc cốt lõi

- **Một nguồn sự thật duy nhất (`RunState`)**: Logic lõi tách thành POCO C# thuần không kế thừa `MonoBehaviour`, giúp chạy EditMode Unit Tests tức thì. Mọi thay đổi chỉ số đi qua `ActionSystem` / `StatsSystem` / `TimeSystem`.
- **Giao tiếp qua `EventBus`**: `Core`/`Systems` phát sự kiện; `UI`, `World` và `Atmosphere` chỉ lắng nghe (`Subscribe`), tránh phụ thuộc chéo trực tiếp.
- **Data-Driven**: Toàn bộ hằng số đặt tại `GameConfig.cs`; dữ liệu thoại, manh mối, nhật ký `ATTEMPT` và mô tả kết cục đọc từ `Assets/_Project/Data/`.

---

## Hướng dẫn cài đặt môi trường phát triển

### 1. Yêu cầu
- **Unity `6000.6.8f1`** (khớp với `ProjectSettings/ProjectVersion.txt`), kèm Build Support tương ứng (`Windows`, `WebGL`, `Android` nếu build).
- **Git** và **Git LFS**.

### 2. Clone & cấu hình Git trên máy cá nhân
```bash
git clone https://github.com/ddeukmindey/TheDeadlineRush.git
cd TheDeadlineRush
git lfs install
git lfs pull
```

Cấu hình `UnityYAMLMerge` để hỗ trợ merge tự động các file `.unity`, `.prefab`, `.asset`:
```bash
git config merge.unityyamlmerge.name "Unity SmartMerge"
git config merge.unityyamlmerge.driver "\"D:/Unity/Editor/6000.6.8f1/Editor/Data/Tools/UnityYAMLMerge.exe\" merge -p %O %B %A %A"
```
*(Thay đường dẫn `D:/Unity/...` bằng đường dẫn cài Unity `6000.6.8f1` trên máy của bạn).*

---

## Kiểm thử, Quy chuẩn Asset & Build

### Chạy Test
- **Trong Unity Editor:** Mở **Window → General → Test Runner**, chạy cả tab **EditMode** (`70` test cases gồm `CoreSystemsTests` + `GameDataValidationTests`) và **PlayMode** (`SmokeTests`).
- **Qua CLI (`-batchmode`):**
  ```powershell
  Unity.exe -batchmode -nographics -projectPath "." -runTests -testPlatform EditMode -testResults "test_results.xml"
  Unity.exe -batchmode -nographics -projectPath "." -runTests -testPlatform PlayMode -testResults "Logs/playmode_results.xml"
  ```

### Quy ước đặt tên & Import Asset
`Assets/Editor/AssetImportValidator.cs` tự động kiểm tra tiền tố asset khi import và áp preset nén/tối ưu cho lần import đầu:
- `SPR_` — Sprite (tắt mipmap, bật nén texture, max size `2048`)
- `TEX_` — Texture
- `SFX_` — Hiệu ứng âm thanh (`DecompressOnLoad`, `ADPCM`)
- `BGM_` — Nhạc nền (`Streaming`, `Vorbis`)
- `PRE_` — Prefab

### Build Local & Quản lý Version
- **Trong Unity Editor:** Menu **Build → Windows / WebGL / Android**.
- **Qua CLI:**
  ```powershell
  Unity.exe -batchmode -nographics -projectPath "." -executeMethod DeadlineRush.Editor.BuildScript.BuildFromCLI -buildTarget Win64
  ```
  *(Hỗ trợ `-target Windows|WebGL|Android` hoặc `-outputPath <đường-dẫn>` — mặc định xuất ra `Builds/<Target>/`)*.
- Mỗi lần build, `BuildScript` tự động tăng `buildNumber` trong `version.json` và cập nhật `PlayerSettings.bundleVersion`.

---

## CI/CD (GitHub Actions)

Workflow tại `.github/workflows/unity-ci.yml` sử dụng `game-ci/unity-test-runner@v4` và `game-ci/unity-builder@v4` cho Unity `6000.6.8f1`:
- **Pull Request:** Chỉ chạy EditMode + PlayMode Tests và upload test artifacts.
- **Push lên `main` hoặc tag `v*`:** Chạy Test → Build → nén `.zip` kèm version & commit hash → Upload Artifact (riêng tag `v*` tạo thêm GitHub Release).
- **`workflow_dispatch`:** Cho phép chọn thủ công target `Windows` / `WebGL` / `Android`.

**GitHub Repository Secrets cần cấu hình** (**Settings → Secrets and variables → Actions**):
- `UNITY_LICENSE`: Nội dung XML file license `.ulf`.
- `UNITY_EMAIL`: Email tài khoản Unity.
- `UNITY_PASSWORD`: Mật khẩu tài khoản Unity.

---

## Tài liệu liên quan

- [Quy tắc làm việc & Git Workflow (`AGENT_RULES.md`)](AGENT_RULES.md)
- [Đặc tả kỹ thuật & Kế hoạch Module (`spec_n_plan_the_deadline_rush.md`)](spec_n_plan_the_deadline_rush.md)
- [Bộ kịch bản kiểm thử tổng hợp (`TEST_CASES.md`)](TEST_CASES.md)

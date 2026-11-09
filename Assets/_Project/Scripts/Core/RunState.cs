// ============================================================================
// RunState.cs — Trạng thái của một Run (lượt chơi) hiện tại
// POCO class (Plain Old C# Object) — KHÔNG kế thừa MonoBehaviour.
// Cho phép chạy EditMode Unit Tests tức thì không cần Scene.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace DeadlineRush.Core
{
    /// <summary>
    /// Chứa toàn bộ trạng thái trong một Run. Reset hoàn toàn khi bắt đầu Run mới.
    /// Không bao giờ được lưu ra đĩa (chỉ MemoryData mới được lưu).
    /// </summary>
    public class RunState
    {
        // ─── Chỉ số chính ─────────────────────────────────────────────────
        /// <summary>Phút game: 0 = 18:00, 359 = 23:59. Tổng 359 phút.</summary>
        public int minute;

        /// <summary>Tiến độ đồ án: 0..100 (%)</summary>
        public int progress;

        /// <summary>Năng lượng: 0..100. Về 0 → Game Over ngay lập tức.</summary>
        public int energy;

        /// <summary>Căng thẳng: 0..100. Chạm 100 không Game Over, giữ ở 100.</summary>
        public int stress;

        // ─── Vị trí & cờ trạng thái ──────────────────────────────────────
        /// <summary>Khu vực hiện tại của người chơi.</summary>
        public AreaId currentArea;

        /// <summary>Đã nhập đúng mật mã 2728 và mở cửa SecretArea chưa.</summary>
        public bool secretAreaOpened;

        // ─── Tập hợp theo dõi trong Run ──────────────────────────────────
        /// <summary>Danh sách ID clue đã nhặt trong Run hiện tại.</summary>
        public HashSet<string> cluesThisRun;

        /// <summary>Các tệp ATTEMPT đã mở đọc trong Run hiện tại.</summary>
        public HashSet<int> attemptsOpenedThisRun;

        /// <summary>Cờ lựa chọn và trạng thái trong Run hiện tại.</summary>
        public HashSet<string> flags;

        /// <summary>Các sự kiện once=true đã phát trong Run hiện tại.</summary>
        public HashSet<string> firedEvents;

        /// <summary>Các ngưỡng giờ (240, 300, 330) đã cộng Stress thụ động.</summary>
        public HashSet<int> thresholdsApplied;

        // ─── Danh sách Clue theo loại (để tra cứu nhanh) ────────────────
        private static readonly HashSet<string> CoreClueIds = new HashSet<string>
        {
            "clue_core_passcode",
            "clue_core_attempt_data",
            "clue_core_loop_record"
        };

        /// <summary>Số Clue cốt lõi đã thu thập trong Run hiện tại.</summary>
        public int CoreClueCount => cluesThisRun.Count(c => CoreClueIds.Contains(c));

        /// <summary>Số Clue phụ đã thu thập trong Run hiện tại.</summary>
        public int MinorClueCount => cluesThisRun.Count(c => !CoreClueIds.Contains(c));

        // ─── Constructor & Reset ─────────────────────────────────────────
        public RunState()
        {
            cluesThisRun = new HashSet<string>();
            attemptsOpenedThisRun = new HashSet<int>();
            flags = new HashSet<string>();
            firedEvents = new HashSet<string>();
            thresholdsApplied = new HashSet<int>();
            Reset();
        }

        /// <summary>
        /// Đưa toàn bộ chỉ số về mặc định đầu Run.
        /// minute=0 (18:00), energy=100, stress=10, progress=0%.
        /// </summary>
        public void Reset()
        {
            minute = 0;
            progress = 0;
            energy = 100;
            stress = 10;
            currentArea = AreaId.Dorm;
            secretAreaOpened = false;

            cluesThisRun.Clear();
            attemptsOpenedThisRun.Clear();
            flags.Clear();
            firedEvents.Clear();
            thresholdsApplied.Clear();
        }
    }
}

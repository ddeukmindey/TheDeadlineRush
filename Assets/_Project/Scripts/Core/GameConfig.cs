// ============================================================================
// GameConfig.cs — Hằng số cấu hình game, đọc tập trung tại đây
// Tuyệt đối không rải con số trong code. Thay đổi balance ở đây.
// ============================================================================

namespace DeadlineRush.Core
{
    /// <summary>
    /// Chứa toàn bộ hằng số cấu hình game. Mọi hệ thống tham chiếu từ đây.
    /// </summary>
    public static class GameConfig
    {
        // ─── Thời gian ───────────────────────────────────────────────────
        /// <summary>Phút tối đa trong game: 359 = 23:59.</summary>
        public const int MaxMinute = 359;

        /// <summary>Phút bắt đầu game: 0 = 18:00.</summary>
        public const int StartMinute = 0;

        /// <summary>Mốc phút cắt cửa sổ True Ending: 350 = 23:50.</summary>
        public const int TrueEndingWindowMinute = 350;

        // ─── WorldPhase Boundaries (phút bắt đầu) ───────────────────────
        public const int Phase2Start = 120;  // 20:00
        public const int Phase3Start = 240;  // 22:00
        public const int Phase4Start = 300;  // 23:00

        // ─── Chỉ số ban đầu ─────────────────────────────────────────────
        public const int StartEnergy = 100;
        public const int StartStress = 10;
        public const int StartProgress = 0;

        public const int MinStat = 0;
        public const int MaxStat = 100;

        // ─── Ngưỡng trạng thái đặc biệt ─────────────────────────────────
        /// <summary>Energy <= giá trị này → giảm tốc x0.6 và nhấp nháy HUD.</summary>
        public const int LowEnergyThreshold = 24;

        /// <summary>Hệ số nhân tốc độ khi kiệt sức.</summary>
        public const float LowEnergySpeedMultiplier = 0.6f;

        // ─── Stress Zones ────────────────────────────────────────────────
        public const int StressNormalMax = 30;
        public const int StressMildMax = 60;
        public const int StressAnxiousMax = 80;
        public const int StressCritical = 81;        // >= 81 → cảnh báo đỏ, mở khóa sự kiện kinh dị
        public const int StressMax = 100;

        // ─── Stress thụ động theo ngưỡng thời gian ──────────────────────
        /// <summary>Ngưỡng phút → lượng Stress cộng thêm. Mỗi ngưỡng chỉ áp dụng 1 lần.</summary>
        public static readonly (int threshold, int stressPenalty)[] TimeStressThresholds = new[]
        {
            (240, 5),   // >= 22:00 → +5  Stress
            (300, 10),  // >= 23:00 → +10 Stress
            (330, 15),  // >= 23:30 → +15 Stress
        };

        // ─── Clue cốt lõi ───────────────────────────────────────────────
        public const int RequiredCoreClueCount = 3;
        public const int RequiredMinorClueForSecret = 3;

        // ─── ATTEMPT ─────────────────────────────────────────────────────
        public const int TotalAttempts = 28;
        public const int Attempt27 = 27;
        public const int Attempt28 = 28;

        /// <summary>Stress cộng khi mở ATTEMPT lần đầu trong Run.</summary>
        public const int AttemptFirstOpenStress = 10;

        /// <summary>Điều kiện mở ATTEMPT_27: progress >= 70 hoặc minute >= 240.</summary>
        public const int Attempt27ProgressMin = 70;
        public const int Attempt27MinuteMin = 240;

        // ─── Mật mã SecretArea ───────────────────────────────────────────
        public const string SecretAreaPasscode = "2728";
    }
}

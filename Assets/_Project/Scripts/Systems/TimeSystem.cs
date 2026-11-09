// ============================================================================
// TimeSystem.cs — Quản lý đồng hồ game và các ngưỡng thời gian
// M2: Advance time, WorldPhase, Stress thụ động, Luật cắt 23:50
// ============================================================================

using System;

namespace DeadlineRush.Systems
{
    using DeadlineRush.Core;

    /// <summary>
    /// Hệ thống quản lý thời gian game. POCO tĩnh để chạy EditMode Unit Tests.
    /// </summary>
    public static class TimeSystem
    {
        /// <summary>
        /// Xác định WorldPhase từ phút game hiện tại.
        /// 0..119 → Phase1 (18:00–19:59)
        /// 120..239 → Phase2 (20:00–21:59)
        /// 240..299 → Phase3 (22:00–22:59)
        /// 300..359 → Phase4 (23:00–23:59)
        /// </summary>
        public static WorldPhase GetPhase(int minute) => GameConfig.GetPhase(minute);

        /// <summary>
        /// Chuyển đổi phút game (0..359) sang chuỗi giờ:phút hiển thị (18:00..23:59).
        /// </summary>
        public static string MinuteToDisplay(int minute)
        {
            int totalMinutes = 18 * 60 + minute; // 18:00 + offset
            int hours = totalMinutes / 60;
            int mins = totalMinutes % 60;
            return $"{hours:D2}:{mins:D2}";
        }

        /// <summary>
        /// Tiến thời gian game. Thực hiện:
        /// 1. Luật cắt 23:50 (nếu đủ điều kiện True Ending)
        /// 2. Clamp minute trong [0..359]
        /// 3. Kiểm tra 3 ngưỡng Stress thụ động (240, 300, 330)
        /// 4. Phát sự kiện OnTimeChanged
        /// 
        /// Trả về tổng Stress thụ động đã cộng (để ActionSystem biết cần ApplyDelta).
        /// </summary>
        public static int Advance(RunState s, int minutes)
        {
            int oldMinute = s.minute;

            // ── Bước 1: Luật cắt 23:50 ──────────────────────────────────
            // Khi đủ 3 Clue cốt lõi + đã mở SecretArea, nếu hành động sẽ
            // vượt qua phút 350, cắt dừng đúng ở 350 để người chơi có
            // cửa sổ lựa chọn cuối (23:50–23:59).
            bool eligibleForTrueEnding =
                s.CoreClueCount >= GameConfig.RequiredCoreClueCount
                && s.secretAreaOpened;

            int newMinute = s.minute + minutes;

            if (eligibleForTrueEnding
                && s.minute < GameConfig.TrueEndingWindowMinute
                && newMinute > GameConfig.TrueEndingWindowMinute)
            {
                newMinute = GameConfig.TrueEndingWindowMinute;
            }

            // ── Bước 2: Clamp ────────────────────────────────────────────
            s.minute = Math.Clamp(newMinute, 0, GameConfig.MaxMinute);

            // ── Bước 3: Stress thụ động theo ngưỡng thời gian ───────────
            int totalPassiveStress = 0;
            foreach (var (threshold, penalty) in GameConfig.TimeStressThresholds)
            {
                if (s.minute >= threshold && !s.thresholdsApplied.Contains(threshold))
                {
                    s.thresholdsApplied.Add(threshold);
                    totalPassiveStress += penalty;
                }
            }

            // Áp dụng Stress thụ động trực tiếp (clamp trong [0..100])
            if (totalPassiveStress > 0)
            {
                s.stress = Math.Clamp(s.stress + totalPassiveStress, GameConfig.MinStat, GameConfig.MaxStat);
            }

            // ── Bước 4: Phát sự kiện ────────────────────────────────────
            EventBus.RaiseTimeChanged(s.minute, GetPhase(s.minute));

            return totalPassiveStress;
        }
    }
}

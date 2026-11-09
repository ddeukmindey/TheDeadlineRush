// ============================================================================
// StatsSystem.cs — Quản lý chỉ số Energy, Stress, Progress
// M2: ApplyDelta, Clamp [0..100], Energy=0 → Game Over
// ============================================================================

using System;

namespace DeadlineRush.Systems
{
    using DeadlineRush.Core;

    /// <summary>
    /// Hệ thống quản lý chỉ số Energy / Stress / Progress.
    /// POCO tĩnh để chạy EditMode Unit Tests.
    /// </summary>
    public static class StatsSystem
    {
        /// <summary>
        /// Áp dụng thay đổi chỉ số cho RunState.
        /// - Clamp tất cả trong [0..100]
        /// - Phát EventBus.RaiseStatsChanged
        /// - Nếu Energy == 0 → phát EventBus.RaiseGameEnded(GameOver)
        /// 
        /// Trả về true nếu Energy > 0 (game tiếp tục), false nếu Game Over.
        /// </summary>
        public static bool ApplyDelta(RunState s, int dEnergy, int dStress, int dProgress)
        {
            s.energy = Math.Clamp(s.energy + dEnergy, GameConfig.MinStat, GameConfig.MaxStat);
            s.stress = Math.Clamp(s.stress + dStress, GameConfig.MinStat, GameConfig.MaxStat);
            s.progress = Math.Clamp(s.progress + dProgress, GameConfig.MinStat, GameConfig.MaxStat);

            // Phát sự kiện thay đổi chỉ số
            EventBus.RaiseStatsChanged(s.energy, s.stress, s.progress);

            // Kiểm tra Game Over: Energy = 0 → dừng ngay lập tức
            if (s.energy <= 0)
            {
                EventBus.RaiseGameEnded(EndingType.GameOver);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Kiểm tra xem nhân vật có đang kiệt sức không (tốc độ giảm x0.6).
        /// </summary>
        public static bool IsExhausted(RunState s)
        {
            return s.energy <= GameConfig.LowEnergyThreshold;
        }

        /// <summary>
        /// Trả về mức Stress zone hiện tại dưới dạng text.
        /// 0–30: Normal | 31–60: Mild | 61–80: Anxious | 81–100: Critical
        /// </summary>
        public static string GetStressZone(int stress)
        {
            if (stress <= GameConfig.StressNormalMax) return "Normal";
            if (stress <= GameConfig.StressMildMax) return "Mild";
            if (stress <= GameConfig.StressAnxiousMax) return "Anxious";
            return "Critical";
        }
    }
}

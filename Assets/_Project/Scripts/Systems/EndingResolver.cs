// ============================================================================
// EndingResolver.cs — Phân giải kết cục game theo 5 mức ưu tiên
// M9: GameOver > True > Secret > Bad > Normal
// ============================================================================

namespace DeadlineRush.Systems
{
    using DeadlineRush.Core;

    /// <summary>
    /// Phân giải kết cục game. Hàm thuần (pure function), không phụ thuộc MonoBehaviour.
    /// Thứ tự ưu tiên: GameOver (0) > True (1) > Secret (2) > Bad (3) > Normal (4)
    /// </summary>
    public static class EndingResolver
    {
        /// <summary>
        /// Phân giải kết cục dựa trên RunState và lựa chọn cuối của người chơi.
        /// 
        /// Ưu tiên 0: energy <= 0 → GameOver
        /// Ưu tiên 1: 3 Clue cốt lõi + secretAreaOpened + DeleteAndDisconnect trong 23:50–23:59 → True
        /// Ưu tiên 2: >= 3 Clue phụ + đã đọc ATTEMPT_27 trong Run (và không đạt True) → Secret
        /// Ưu tiên 3: progress == 100 (và không đạt True / Secret) → Bad
        /// Ưu tiên 4: Các trường hợp còn lại → Normal
        /// </summary>
        public static EndingType Resolve(RunState s, FinalChoice choice)
        {
            // ── Ưu tiên 0: Game Over ────────────────────────────────────
            if (s.energy <= 0)
            {
                return EndingType.GameOver;
            }

            // ── Ưu tiên 1: True Ending ──────────────────────────────────
            // Đủ 3 Clue cốt lõi + đã mở SecretArea + chọn "Xóa bản sao"
            // trong cửa sổ 23:50–23:59
            if (s.CoreClueCount >= GameConfig.RequiredCoreClueCount
                && s.secretAreaOpened
                && choice == FinalChoice.DeleteAndDisconnect
                && s.minute >= GameConfig.TrueEndingWindowMinute
                && s.minute <= GameConfig.MaxMinute)
            {
                return EndingType.True;
            }

            // ── Ưu tiên 2: Secret Ending ────────────────────────────────
            // >= 3 Clue phụ + đã đọc ATTEMPT_27 trong Run hiện tại
            if (s.MinorClueCount >= GameConfig.RequiredMinorClueForSecret
                && s.attemptsOpenedThisRun.Contains(GameConfig.Attempt27))
            {
                return EndingType.Secret;
            }

            // ── Ưu tiên 3: Bad Ending ───────────────────────────────────
            // progress đạt 100% (hoàn thành đồ án)
            if (s.progress >= GameConfig.MaxStat)
            {
                return EndingType.Bad;
            }

            // ── Ưu tiên 4: Normal Ending ────────────────────────────────
            // Mọi trường hợp còn lại khi chạm 23:59 hoặc chọn "Nộp đồ án"
            return EndingType.Normal;
        }
    }
}

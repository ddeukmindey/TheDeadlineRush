// ============================================================================
// ClueSystem.cs — Quản lý Clue (manh mối) và tệp ATTEMPT
// M6: 3 Clue cốt lõi, 6 Clue phụ, 28 tệp ATTEMPT
// ============================================================================

using System;
using UnityEngine;

namespace DeadlineRush.Systems
{
    using DeadlineRush.Core;

    /// <summary>
    /// Hệ thống quản lý manh mối (Clue) và tệp ATTEMPT.
    /// POCO tĩnh để chạy EditMode Unit Tests.
    /// </summary>
    public static class ClueSystem
    {
        // ─── Clue IDs cốt lõi ───────────────────────────────────────────
        public const string CluePasscode = "clue_core_passcode";           // Mật mã 2728
        public const string ClueAttemptData = "clue_core_attempt_data";    // Dữ liệu ATTEMPT
        public const string ClueLoopRecord = "clue_core_loop_record";      // Bản ghi vòng lặp

        // ─── Clue IDs phụ (6 clue) ──────────────────────────────────────
        public const string ClueMinor01 = "clue_minor_strange_note";
        public const string ClueMinor02 = "clue_minor_old_photo";
        public const string ClueMinor03 = "clue_minor_error_log";
        public const string ClueMinor04 = "clue_minor_npc_hint";
        public const string ClueMinor05 = "clue_minor_scratched_wall";
        public const string ClueMinor06 = "clue_minor_hidden_message";

        /// <summary>
        /// Kiểm tra một Clue ID có phải cốt lõi không.
        /// </summary>
        public static bool IsCoreClue(string clueId)
        {
            return clueId == CluePasscode
                || clueId == ClueAttemptData
                || clueId == ClueLoopRecord;
        }

        /// <summary>
        /// Thêm Clue vào Run hiện tại. Nếu đã có thì bỏ qua.
        /// Phát EventBus.RaiseClueAdded.
        /// </summary>
        public static void AddClue(RunState s, string clueId)
        {
            if (string.IsNullOrEmpty(clueId)) return;
            if (s.cluesThisRun.Contains(clueId)) return;

            s.cluesThisRun.Add(clueId);
            bool isCore = IsCoreClue(clueId);
            EventBus.RaiseClueAdded(clueId, isCore);

            Debug.Log($"[ClueSystem] Added clue: {clueId} (Core: {isCore})");
        }

        /// <summary>
        /// Kiểm tra xem có thể mở ATTEMPT_27 không.
        /// Điều kiện: progress >= 70 hoặc minute >= 240.
        /// </summary>
        public static bool CanOpenAttempt27(RunState s)
        {
            return s.progress >= GameConfig.Attempt27ProgressMin
                || s.minute >= GameConfig.Attempt27MinuteMin;
        }

        /// <summary>
        /// Mở một tệp ATTEMPT.
        /// - Kiểm tra điều kiện đặc biệt cho ATTEMPT_27.
        /// - Nếu mở lần đầu trong Run: cộng +10 Stress.
        /// - Thêm vào MemoryData.unlockedAttempts (lưu vĩnh viễn).
        /// - Phát EventBus.RaiseAttemptOpened.
        /// 
        /// Trả về true nếu mở thành công.
        /// </summary>
        public static bool OpenAttempt(RunState s, MemoryData mem, int attemptNum)
        {
            // Validate range
            if (attemptNum < 1 || attemptNum > GameConfig.TotalAttempts)
            {
                Debug.LogWarning($"[ClueSystem] Invalid attempt number: {attemptNum}");
                return false;
            }

            // Điều kiện đặc biệt cho ATTEMPT_27
            if (attemptNum == GameConfig.Attempt27 && !CanOpenAttempt27(s))
            {
                Debug.Log("[ClueSystem] Cannot open ATTEMPT_27: conditions not met " +
                          $"(progress={s.progress}, minute={s.minute})");
                return false;
            }

            // Kiểm tra lần đầu mở trong Run hiện tại
            bool firstTimeInRun = !s.attemptsOpenedThisRun.Contains(attemptNum);

            // Đánh dấu đã mở trong Run
            s.attemptsOpenedThisRun.Add(attemptNum);

            // Cộng Stress nếu mở lần đầu trong Run
            if (firstTimeInRun)
            {
                s.stress = Math.Clamp(
                    s.stress + GameConfig.AttemptFirstOpenStress,
                    GameConfig.MinStat, GameConfig.MaxStat);
                EventBus.RaiseStatsChanged(s.energy, s.stress, s.progress);
                Debug.Log($"[ClueSystem] First-time ATTEMPT_{attemptNum:D2} in this Run: +{GameConfig.AttemptFirstOpenStress} Stress");
            }

            // Lưu vào MemoryData (persistent across runs)
            mem.AddUnlockedAttempt(attemptNum);

            // Phát sự kiện
            EventBus.RaiseAttemptOpened(attemptNum);

            Debug.Log($"[ClueSystem] Opened ATTEMPT_{attemptNum:D2}");
            return true;
        }
    }
}

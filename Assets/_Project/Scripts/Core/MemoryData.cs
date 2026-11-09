// ============================================================================
// MemoryData.cs — Dữ liệu lưu trữ vĩnh viễn giữa các Run
// Serializable để JsonUtility có thể ghi/đọc ra file JSON.
// ============================================================================

using System;
using System.Collections.Generic;

namespace DeadlineRush.Core
{
    /// <summary>
    /// Dữ liệu lưu trữ vĩnh viễn giữa các Run.
    /// Khi bấm New Game, RunState reset 100%, nhưng MemoryData giữ nguyên.
    /// </summary>
    [Serializable]
    public class MemoryData
    {
        // ─── Tệp ATTEMPT đã mở qua mọi Run ─────────────────────────────
        /// <summary>
        /// Danh sách tệp ATTEMPT_01..28 đã từng mở qua tất cả các lần chơi.
        /// Dùng List vì JsonUtility không hỗ trợ serialize HashSet.
        /// </summary>
        public List<int> unlockedAttempts = new List<int>();

        // ─── Settings ────────────────────────────────────────────────────
        public float musicVolume = 0.8f;
        public float sfxVolume = 0.8f;
        public float brightness = 1.0f;

        // ─── Helper Methods ──────────────────────────────────────────────

        /// <summary>
        /// Kiểm tra xem một tệp ATTEMPT đã từng được mở chưa (qua mọi Run).
        /// </summary>
        public bool HasUnlockedAttempt(int attemptNum)
        {
            return unlockedAttempts.Contains(attemptNum);
        }

        /// <summary>
        /// Thêm một tệp ATTEMPT vào danh sách đã mở (nếu chưa có).
        /// </summary>
        public void AddUnlockedAttempt(int attemptNum)
        {
            if (!unlockedAttempts.Contains(attemptNum))
            {
                unlockedAttempts.Add(attemptNum);
            }
        }
    }
}

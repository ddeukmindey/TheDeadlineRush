// ============================================================================
// Contracts.cs — Hợp đồng Code dùng chung (Bước 0)
// Tất cả Agent B, C, D chỉ sử dụng các Enum, Struct và Event trong file này.
// Agent A sở hữu và duy trì file này. Các Agent khác KHÔNG được tự ý sửa.
// ============================================================================

using System;
using System.Collections.Generic;

namespace DeadlineRush.Core
{
    /// <summary>
    /// Giai đoạn thế giới theo khung giờ trong game.
    /// Phase1: 18:00–19:59 | Phase2: 20:00–21:59 | Phase3: 22:00–22:59 | Phase4: 23:00–23:59
    /// </summary>
    public enum WorldPhase
    {
        Phase1_18_20 = 1,
        Phase2_20_22 = 2,
        Phase3_22_23 = 3,
        Phase4_23_24 = 4
    }

    /// <summary>
    /// Các khu vực trong game. Di chuyển giữa khu vực tốn 0 phút.
    /// </summary>
    public enum AreaId
    {
        Dorm,
        Hallway,
        Library,
        Lab,
        Canteen,
        SecretArea
    }

    /// <summary>
    /// Các loại kết thúc game, sắp theo thứ tự ưu tiên giảm dần.
    /// GameOver > True > Secret > Bad > Normal
    /// </summary>
    public enum EndingType
    {
        None,
        GameOver,
        Normal,
        Bad,
        Secret,
        True
    }

    /// <summary>
    /// Lựa chọn cuối cùng của người chơi tại cửa sổ 23:50–23:59.
    /// </summary>
    public enum FinalChoice
    {
        None,
        SubmitProject,
        DeleteAndDisconnect
    }

    /// <summary>
    /// Trạng thái của GameManager State Machine.
    /// </summary>
    public enum GameState
    {
        MainMenu,
        Playing,
        Ending,
        GameOver
    }

    /// <summary>
    /// Các loại điều kiện để đánh giá sự kiện và hành động.
    /// </summary>
    public enum ConditionType
    {
        TimeRange,
        ProgressMin,
        StressMin,
        HasClue,
        CoreClueCountMin,
        MinorClueCountMin,
        InArea,
        HasFlag,
        AttemptOpened
    }

    /// <summary>
    /// Một điều kiện đơn lẻ, có thể serialize trong ScriptableObject hoặc JSON.
    /// </summary>
    [Serializable]
    public struct Condition
    {
        public ConditionType type;
        public int intValue;
        public int intValueMax;
        public string stringValue;
        public AreaId areaValue;
    }

    /// <summary>
    /// Dữ liệu xem trước chi phí hành động, hiển thị trong popup xác nhận.
    /// </summary>
    [Serializable]
    public class ActionCostPreview
    {
        public string actionId;
        public string displayName;
        public int minutes;
        public int dEnergy;
        public int dStress;
        public int dProgress;
    }

    /// <summary>
    /// EventBus trung tâm: Agent A phát sự kiện (Raise), Agent B/C/D đăng ký lắng nghe (Subscribe).
    /// Tất cả event là static để đảm bảo tính toàn cục và đơn giản.
    /// </summary>
    public static class EventBus
    {
        // ─── Sự kiện thời gian & chỉ số ───────────────────────────────────
        public static event Action<int, WorldPhase> OnTimeChanged;                // (minute, currentPhase)
        public static event Action<int, int, int> OnStatsChanged;                 // (energy, stress, progress)

        // ─── Sự kiện khu vực ──────────────────────────────────────────────
        public static event Action<AreaId> OnAreaChanged;                         // (newArea)

        // ─── Sự kiện manh mối & tệp ATTEMPT ──────────────────────────────
        public static event Action<string, bool> OnClueAdded;                     // (clueId, isCore)
        public static event Action<int> OnAttemptOpened;                          // (attemptNumber 1..28)

        // ─── Sự kiện câu chuyện ───────────────────────────────────────────
        public static event Action<string> OnGameEventTriggered;                  // (eventId: E01..E11)
        public static event Action<ActionCostPreview, Action> OnActionConfirmRequested; // (preview, onConfirmCallback)
        public static event Action<string, bool> OnDialogueRequested;             // (dialogueGroupId, isMandatory)

        // ─── Sự kiện UI & kết thúc ───────────────────────────────────────
        public static event Action<bool> OnUIModalStateChanged;                   // (isAnyModalOpen → khóa Player move)
        public static event Action<EndingType> OnGameEnded;                       // (endingType)

        // ─── Phương thức phát sự kiện ─────────────────────────────────────
        public static void RaiseTimeChanged(int min, WorldPhase phase)
            => OnTimeChanged?.Invoke(min, phase);

        public static void RaiseStatsChanged(int e, int s, int p)
            => OnStatsChanged?.Invoke(e, s, p);

        public static void RaiseAreaChanged(AreaId area)
            => OnAreaChanged?.Invoke(area);

        public static void RaiseClueAdded(string id, bool isCore)
            => OnClueAdded?.Invoke(id, isCore);

        public static void RaiseAttemptOpened(int num)
            => OnAttemptOpened?.Invoke(num);

        public static void RaiseGameEventTriggered(string evtId)
            => OnGameEventTriggered?.Invoke(evtId);

        public static void RaiseActionConfirmRequested(ActionCostPreview p, Action confirm)
            => OnActionConfirmRequested?.Invoke(p, confirm);

        public static void RaiseDialogueRequested(string group, bool mandatory = false)
            => OnDialogueRequested?.Invoke(group, mandatory);

        public static void RaiseUIModalStateChanged(bool isOpen)
            => OnUIModalStateChanged?.Invoke(isOpen);

        public static void RaiseGameEnded(EndingType ending)
            => OnGameEnded?.Invoke(ending);

        /// <summary>
        /// Hủy toàn bộ listener. Chỉ dùng cho Unit Test teardown hoặc khi chuyển Scene.
        /// </summary>
        public static void ClearAllListeners()
        {
            OnTimeChanged = null;
            OnStatsChanged = null;
            OnAreaChanged = null;
            OnClueAdded = null;
            OnAttemptOpened = null;
            OnGameEventTriggered = null;
            OnActionConfirmRequested = null;
            OnDialogueRequested = null;
            OnUIModalStateChanged = null;
            OnGameEnded = null;
        }
    }
}

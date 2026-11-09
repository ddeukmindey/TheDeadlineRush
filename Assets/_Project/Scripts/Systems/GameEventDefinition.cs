// ============================================================================
// GameEventDefinition.cs — ScriptableObject mô tả một sự kiện game (E01–E11)
// M5: Data-driven, định nghĩa điều kiện kích hoạt, priority, once flag
// ============================================================================

using UnityEngine;

namespace DeadlineRush.Systems
{
    using DeadlineRush.Core;

    /// <summary>
    /// ScriptableObject mô tả một sự kiện câu chuyện (E01–E11).
    /// Chứa điều kiện kích hoạt, hiệu ứng state, priority, cờ once.
    /// </summary>
    [CreateAssetMenu(fileName = "NewGameEvent", menuName = "DeadlineRush/Game Event Definition")]
    public class GameEventDefinition : ScriptableObject
    {
        [Header("─── Thông tin cơ bản ───")]
        public string eventId;
        public string eventName;

        [TextArea(2, 4)]
        public string description;

        [Header("─── Điều kiện kích hoạt ───")]
        [Tooltip("Tất cả điều kiện phải thỏa mãn (AND logic)")]
        public Condition[] conditions;

        [Header("─── Hiệu ứng ───")]
        [Tooltip("Stress cộng thêm khi sự kiện phát ra")]
        public int stressDelta;

        [Tooltip("ID Clue trao cho người chơi (để trống nếu không có)")]
        public string clueRewardId;

        [Tooltip("Clue trao là cốt lõi?")]
        public bool clueIsCore;

        [Tooltip("Flag thêm vào RunState khi sự kiện phát ra")]
        public string flagToSet;

        [Tooltip("ID nhóm thoại kích hoạt (để trống nếu không có)")]
        public string dialogueGroupId;

        [Tooltip("Thoại có bắt buộc chọn không (khóa Esc)")]
        public bool dialogueIsMandatory;

        [Header("─── Ưu tiên & Lặp lại ───")]
        [Tooltip("Ưu tiên: số cao hơn phát trước khi nhiều sự kiện thỏa điều kiện cùng lúc")]
        public int priority;

        [Tooltip("Nếu true, sự kiện chỉ phát 1 lần duy nhất trong toàn bộ Run")]
        public bool once = true;
    }
}

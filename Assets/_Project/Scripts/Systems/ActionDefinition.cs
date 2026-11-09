// ============================================================================
// ActionDefinition.cs — ScriptableObject mô tả một hành động trong game
// M3: Data-driven, không hardcode con số trong script
// ============================================================================

using UnityEngine;

namespace DeadlineRush.Systems
{
    using DeadlineRush.Core;

    /// <summary>
    /// ScriptableObject mô tả một hành động game (Code, Eat, Rest, Research, Explore, Inspect, Dialogue).
    /// Toàn bộ thông số balance chỉ chỉnh ở asset file, không sửa code.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAction", menuName = "DeadlineRush/Action Definition")]
    public class ActionDefinition : ScriptableObject
    {
        [Header("─── Thông tin cơ bản ───")]
        public string actionId;
        public string displayName;

        [Header("─── Chi phí / Hiệu ứng ───")]
        [Tooltip("Số phút tốn khi thực hiện (0 = tức thì)")]
        public int minutes;

        [Tooltip("Thay đổi Energy (âm = mất, dương = hồi)")]
        public int dEnergy;

        [Tooltip("Thay đổi Stress (dương = tăng căng thẳng, âm = giảm)")]
        public int dStress;

        [Tooltip("Thay đổi Progress (dương = tăng tiến độ)")]
        public int dProgress;

        [Header("─── Điều kiện ───")]
        [Tooltip("Khu vực yêu cầu để thực hiện (nếu cần)")]
        public AreaId requiredArea;

        [Tooltip("Có yêu cầu đúng khu vực không")]
        public bool requiresArea = true;

        [Tooltip("Các điều kiện bổ sung")]
        public Condition[] conditions;

        /// <summary>
        /// Tạo ActionCostPreview để hiển thị UI xác nhận.
        /// </summary>
        public ActionCostPreview ToPreview()
        {
            return new ActionCostPreview
            {
                actionId = actionId,
                displayName = displayName,
                minutes = minutes,
                dEnergy = dEnergy,
                dStress = dStress,
                dProgress = dProgress
            };
        }
    }
}

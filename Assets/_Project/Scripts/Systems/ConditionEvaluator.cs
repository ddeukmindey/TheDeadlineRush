// ============================================================================
// ConditionEvaluator.cs — Đánh giá điều kiện cho sự kiện và hành động
// M5: Evaluate từng ConditionType dựa trên RunState
// ============================================================================

namespace DeadlineRush.Systems
{
    using DeadlineRush.Core;

    /// <summary>
    /// Đánh giá từng loại Condition dựa trên trạng thái RunState hiện tại.
    /// POCO tĩnh để chạy EditMode Unit Tests.
    /// </summary>
    public static class ConditionEvaluator
    {
        /// <summary>
        /// Đánh giá một Condition đơn lẻ. Trả về true nếu thỏa mãn.
        /// </summary>
        public static bool Evaluate(Condition c, RunState s)
        {
            switch (c.type)
            {
                case ConditionType.TimeRange:
                    // intValue = minMinute, intValueMax = maxMinute
                    // Nếu intValueMax == 0, chỉ kiểm tra >= intValue
                    if (c.intValueMax > 0)
                        return s.minute >= c.intValue && s.minute <= c.intValueMax;
                    return s.minute >= c.intValue;

                case ConditionType.ProgressMin:
                    return s.progress >= c.intValue;

                case ConditionType.StressMin:
                    return s.stress >= c.intValue;

                case ConditionType.HasClue:
                    return s.cluesThisRun.Contains(c.stringValue);

                case ConditionType.CoreClueCountMin:
                    return s.CoreClueCount >= c.intValue;

                case ConditionType.MinorClueCountMin:
                    return s.MinorClueCount >= c.intValue;

                case ConditionType.InArea:
                    return s.currentArea == c.areaValue;

                case ConditionType.HasFlag:
                    return s.flags.Contains(c.stringValue);

                case ConditionType.AttemptOpened:
                    return s.attemptsOpenedThisRun.Contains(c.intValue);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Đánh giá một mảng Conditions. Trả về true nếu TẤT CẢ đều thỏa mãn (AND logic).
        /// Mảng rỗng hoặc null → trả về true.
        /// </summary>
        public static bool EvaluateAll(Condition[] conditions, RunState s)
        {
            if (conditions == null || conditions.Length == 0)
                return true;

            foreach (var c in conditions)
            {
                if (!Evaluate(c, s))
                    return false;
            }
            return true;
        }
    }
}

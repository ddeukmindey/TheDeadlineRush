// ============================================================================
// EventDirector.cs — Bộ điều phối sự kiện: lọc, sắp priority, phát tối đa 1
// M5: Duyệt E01–E11, chỉ phát 1 sự kiện priority cao nhất mỗi lần Evaluate
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadlineRush.Systems
{
    using DeadlineRush.Core;

    /// <summary>
    /// Bộ điều phối sự kiện game. Mỗi lần Evaluate, chỉ phát tối đa 1 sự kiện
    /// có priority cao nhất trong số các sự kiện đủ điều kiện.
    /// </summary>
    public class EventDirector : MonoBehaviour
    {
        [Tooltip("Danh sách tất cả GameEventDefinition (E01–E11)")]
        public List<GameEventDefinition> allEvents = new List<GameEventDefinition>();

        /// <summary>
        /// Đánh giá toàn bộ sự kiện dựa trên RunState hiện tại.
        /// Chỉ phát 1 sự kiện có priority cao nhất.
        /// Trả về eventId đã phát, hoặc null nếu không có.
        /// </summary>
        public string Evaluate(RunState s)
        {
            return EvaluatePure(allEvents, s);
        }

        /// <summary>
        /// Phiên bản tĩnh thuần túy cho Unit Tests.
        /// </summary>
        public static string EvaluatePure(List<GameEventDefinition> events, RunState s)
        {
            if (events == null || events.Count == 0) return null;

            GameEventDefinition bestEvent = null;
            int bestPriority = int.MinValue;

            foreach (var evt in events)
            {
                if (evt == null) continue;

                // Bỏ qua sự kiện once đã phát
                if (evt.once && s.firedEvents.Contains(evt.eventId))
                    continue;

                // Kiểm tra tất cả điều kiện
                if (!ConditionEvaluator.EvaluateAll(evt.conditions, s))
                    continue;

                // Chọn sự kiện có priority cao nhất
                if (evt.priority > bestPriority)
                {
                    bestPriority = evt.priority;
                    bestEvent = evt;
                }
            }

            if (bestEvent == null) return null;

            // ── Áp dụng hiệu ứng sự kiện ────────────────────────────────

            // Đánh dấu đã phát (cho sự kiện once)
            if (bestEvent.once)
            {
                s.firedEvents.Add(bestEvent.eventId);
            }

            // Cộng Stress
            if (bestEvent.stressDelta != 0)
            {
                s.stress = Math.Clamp(
                    s.stress + bestEvent.stressDelta,
                    GameConfig.MinStat, GameConfig.MaxStat);
                EventBus.RaiseStatsChanged(s.energy, s.stress, s.progress);
            }

            // Trao Clue
            if (!string.IsNullOrEmpty(bestEvent.clueRewardId))
            {
                s.cluesThisRun.Add(bestEvent.clueRewardId);
                EventBus.RaiseClueAdded(bestEvent.clueRewardId, bestEvent.clueIsCore);
            }

            // Set Flag
            if (!string.IsNullOrEmpty(bestEvent.flagToSet))
            {
                s.flags.Add(bestEvent.flagToSet);
            }

            // Phát dialogue
            if (!string.IsNullOrEmpty(bestEvent.dialogueGroupId))
            {
                EventBus.RaiseDialogueRequested(bestEvent.dialogueGroupId, bestEvent.dialogueIsMandatory);
            }

            // Phát sự kiện chung
            EventBus.RaiseGameEventTriggered(bestEvent.eventId);

            return bestEvent.eventId;
        }
    }
}

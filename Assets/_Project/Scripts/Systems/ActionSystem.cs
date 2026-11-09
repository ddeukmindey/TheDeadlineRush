// ============================================================================
// ActionSystem.cs — Hệ thống thực thi hành động theo pipeline 10 bước
// M3: Kiểm tra → Xác nhận → TimeAdvance → ApplyDelta → Threshold →
//     GameOver → EventDirector → EndingCheck → HUD Update
// ============================================================================

using System;
using UnityEngine;

namespace DeadlineRush.Systems
{
    using DeadlineRush.Core;

    /// <summary>
    /// Hệ thống thực thi hành động game. Tuân thủ pipeline 10 bước trong tài liệu đặc tả.
    /// </summary>
    public class ActionSystem : MonoBehaviour
    {
        public static ActionSystem Instance { get; private set; }

        private RunState _state;
        private MemoryData _memory;
        private EventDirector _eventDirector;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Khởi tạo hệ thống với các tham chiếu cần thiết.
        /// </summary>
        public void Initialize(RunState state, MemoryData memory, EventDirector eventDirector)
        {
            _state = state;
            _memory = memory;
            _eventDirector = eventDirector;
        }

        // ─── Bước 1: Kiểm tra điều kiện CanPerform ──────────────────────

        /// <summary>
        /// Kiểm tra xem hành động có thể thực hiện hay không.
        /// Đúng khu vực, đúng đối tượng, đủ điều kiện tiên quyết.
        /// </summary>
        public bool CanPerform(ActionDefinition def)
        {
            if (_state == null) return false;

            // Kiểm tra khu vực
            if (def.requiresArea && _state.currentArea != def.requiredArea)
                return false;

            // Kiểm tra các điều kiện bổ sung
            if (def.conditions != null)
            {
                foreach (var cond in def.conditions)
                {
                    if (!ConditionEvaluator.Evaluate(cond, _state))
                        return false;
                }
            }

            return true;
        }

        // ─── Bước 2-10: Yêu cầu thực hiện hành động ────────────────────

        /// <summary>
        /// Yêu cầu thực hiện hành động.
        /// Nếu minutes > 0: hiển thị popup xác nhận trước.
        /// Nếu minutes == 0: chạy thẳng (tương tác đơn giản).
        /// </summary>
        public void RequestPerform(ActionDefinition def)
        {
            // Bước 1: Kiểm tra CanPerform
            if (!CanPerform(def))
            {
                Debug.Log($"[ActionSystem] Cannot perform '{def.displayName}': conditions not met.");
                return;
            }

            if (def.minutes > 0)
            {
                // Bước 2: Hiển thị UI xác nhận chi phí
                ActionCostPreview preview = def.ToPreview();
                EventBus.RaiseActionConfirmRequested(preview, () => ExecutePerform(def));
            }
            else
            {
                // Minutes == 0 → chạy ngay, không cần xác nhận
                ExecutePerform(def);
            }
        }

        /// <summary>
        /// Pipeline thực thi hành động hoàn chỉnh (10 bước).
        /// Phiên bản tĩnh thuần túy dùng cho Unit Tests.
        /// </summary>
        public static void ExecutePerformPure(
            ActionDefinition def,
            RunState state,
            MemoryData memory,
            EventDirector eventDirector)
        {
            // Bước 3: Tính số phút thực tế (Luật cắt 23:50 nằm trong TimeSystem)
            int actualMinutes = def.minutes;

            // Bước 4: TimeSystem.Advance → cập nhật đồng hồ & Stress thụ động
            if (actualMinutes > 0)
            {
                TimeSystem.Advance(state, actualMinutes);
            }

            // Bước 5: StatsSystem.ApplyDelta → Clamp [0..100]
            bool alive = StatsSystem.ApplyDelta(state, def.dEnergy, def.dStress, def.dProgress);

            // Bước 6: Ngưỡng thời gian đã xử lý trong TimeSystem.Advance ở bước 4

            // Bước 7: Nếu Energy == 0 → GAME OVER (ApplyDelta đã phát sự kiện)
            if (!alive) return;

            // Bước 8: EventDirector.Evaluate → Lọc và phát sự kiện có priority cao nhất
            if (eventDirector != null)
            {
                eventDirector.Evaluate(state);
            }

            // Bước 9: Nếu minute == 359 → Gọi EndingResolver
            if (state.minute >= GameConfig.MaxMinute)
            {
                EndingType ending = EndingResolver.Resolve(state, FinalChoice.None);
                if (ending != EndingType.None)
                {
                    EventBus.RaiseGameEnded(ending);
                }
            }

            // Bước 10: Phát sự kiện HUD & Atmosphere (đã phát trong các bước 4, 5)
        }

        // ─── Private: Wrapper cho MonoBehaviour context ──────────────────

        private void ExecutePerform(ActionDefinition def)
        {
            ExecutePerformPure(def, _state, _memory, _eventDirector);
        }
    }
}

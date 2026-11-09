// ============================================================================
// CoreSystemsTests.cs — EditMode Unit Tests cho toàn bộ Core Systems (Agent A)
// Bao phủ Checklist 5A: Clamp, Ngưỡng thời gian, GameOver, EndingResolver,
//   Luật cắt 23:50, EventDirector priority & once
// ============================================================================

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using DeadlineRush.Core;
using DeadlineRush.Systems;

namespace DeadlineRush.Tests
{
    [TestFixture]
    public class CoreSystemsTests
    {
        private RunState _state;
        private MemoryData _memory;

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAllListeners();
            _state = new RunState();
            _memory = new MemoryData();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ClearAllListeners();
        }

        // =====================================================================
        // 1. RunState — Khởi tạo và Reset
        // =====================================================================

        [Test]
        public void RunState_DefaultValues_AreCorrect()
        {
            Assert.AreEqual(0, _state.minute, "minute should start at 0 (18:00)");
            Assert.AreEqual(100, _state.energy, "energy should start at 100");
            Assert.AreEqual(10, _state.stress, "stress should start at 10");
            Assert.AreEqual(0, _state.progress, "progress should start at 0");
            Assert.AreEqual(AreaId.Dorm, _state.currentArea, "should start in Dorm");
            Assert.IsFalse(_state.secretAreaOpened, "secretArea should be closed");
            Assert.AreEqual(0, _state.cluesThisRun.Count, "clues should be empty");
            Assert.AreEqual(0, _state.attemptsOpenedThisRun.Count, "attempts should be empty");
            Assert.AreEqual(0, _state.flags.Count, "flags should be empty");
            Assert.AreEqual(0, _state.firedEvents.Count, "firedEvents should be empty");
            Assert.AreEqual(0, _state.thresholdsApplied.Count, "thresholds should be empty");
        }

        [Test]
        public void RunState_Reset_ClearsEverything()
        {
            _state.minute = 200;
            _state.energy = 50;
            _state.stress = 80;
            _state.progress = 75;
            _state.currentArea = AreaId.Library;
            _state.secretAreaOpened = true;
            _state.cluesThisRun.Add("clue_core_passcode");
            _state.attemptsOpenedThisRun.Add(5);
            _state.flags.Add("some_flag");
            _state.firedEvents.Add("E01");
            _state.thresholdsApplied.Add(240);

            _state.Reset();

            Assert.AreEqual(0, _state.minute);
            Assert.AreEqual(100, _state.energy);
            Assert.AreEqual(10, _state.stress);
            Assert.AreEqual(0, _state.progress);
            Assert.AreEqual(AreaId.Dorm, _state.currentArea);
            Assert.IsFalse(_state.secretAreaOpened);
            Assert.AreEqual(0, _state.cluesThisRun.Count);
            Assert.AreEqual(0, _state.attemptsOpenedThisRun.Count);
            Assert.AreEqual(0, _state.flags.Count);
            Assert.AreEqual(0, _state.firedEvents.Count);
            Assert.AreEqual(0, _state.thresholdsApplied.Count);
        }

        [Test]
        public void RunState_CoreClueCount_CountsCorrectly()
        {
            Assert.AreEqual(0, _state.CoreClueCount);

            _state.cluesThisRun.Add("clue_core_passcode");
            Assert.AreEqual(1, _state.CoreClueCount);

            _state.cluesThisRun.Add("clue_minor_strange_note");
            Assert.AreEqual(1, _state.CoreClueCount, "Minor clue should not count as core");

            _state.cluesThisRun.Add("clue_core_attempt_data");
            _state.cluesThisRun.Add("clue_core_loop_record");
            Assert.AreEqual(3, _state.CoreClueCount);
        }

        [Test]
        public void RunState_MinorClueCount_CountsCorrectly()
        {
            _state.cluesThisRun.Add("clue_minor_strange_note");
            _state.cluesThisRun.Add("clue_minor_old_photo");
            _state.cluesThisRun.Add("clue_core_passcode");

            Assert.AreEqual(2, _state.MinorClueCount);
            Assert.AreEqual(1, _state.CoreClueCount);
        }

        // =====================================================================
        // 2. StatsSystem — Clamp [0..100]
        // =====================================================================

        [Test]
        public void StatsSystem_ApplyDelta_ClampsEnergyTo0()
        {
            _state.energy = 10;
            StatsSystem.ApplyDelta(_state, -50, 0, 0);
            Assert.AreEqual(0, _state.energy, "Energy should clamp to 0");
        }

        [Test]
        public void StatsSystem_ApplyDelta_ClampsEnergyTo100()
        {
            _state.energy = 90;
            StatsSystem.ApplyDelta(_state, +50, 0, 0);
            Assert.AreEqual(100, _state.energy, "Energy should clamp to 100");
        }

        [Test]
        public void StatsSystem_ApplyDelta_ClampsStressTo0()
        {
            _state.stress = 5;
            StatsSystem.ApplyDelta(_state, 0, -20, 0);
            Assert.AreEqual(0, _state.stress, "Stress should clamp to 0");
        }

        [Test]
        public void StatsSystem_ApplyDelta_ClampsStressTo100()
        {
            _state.stress = 95;
            StatsSystem.ApplyDelta(_state, 0, +20, 0);
            Assert.AreEqual(100, _state.stress, "Stress should clamp to 100");
        }

        [Test]
        public void StatsSystem_ApplyDelta_ClampsProgressTo0()
        {
            _state.progress = 5;
            StatsSystem.ApplyDelta(_state, 0, 0, -20);
            Assert.AreEqual(0, _state.progress, "Progress should clamp to 0");
        }

        [Test]
        public void StatsSystem_ApplyDelta_ClampsProgressTo100()
        {
            _state.progress = 95;
            StatsSystem.ApplyDelta(_state, 0, 0, +20);
            Assert.AreEqual(100, _state.progress, "Progress should clamp to 100");
        }

        [Test]
        public void StatsSystem_ApplyDelta_AllThreeClampSimultaneously()
        {
            _state.energy = 5;
            _state.stress = 95;
            _state.progress = 98;

            StatsSystem.ApplyDelta(_state, -100, +100, +100);

            Assert.AreEqual(0, _state.energy);
            Assert.AreEqual(100, _state.stress);
            Assert.AreEqual(100, _state.progress);
        }

        // =====================================================================
        // 3. StatsSystem — Energy == 0 → Game Over
        // =====================================================================

        [Test]
        public void StatsSystem_EnergyZero_TriggersGameOver()
        {
            EndingType? receivedEnding = null;
            EventBus.OnGameEnded += (ending) => receivedEnding = ending;

            _state.energy = 10;
            bool alive = StatsSystem.ApplyDelta(_state, -10, 0, 0);

            Assert.IsFalse(alive, "ApplyDelta should return false when energy reaches 0");
            Assert.AreEqual(EndingType.GameOver, receivedEnding, "Should fire GameOver event");
        }

        [Test]
        public void StatsSystem_EnergyAboveZero_DoesNotTriggerGameOver()
        {
            EndingType? receivedEnding = null;
            EventBus.OnGameEnded += (ending) => receivedEnding = ending;

            _state.energy = 20;
            bool alive = StatsSystem.ApplyDelta(_state, -10, 0, 0);

            Assert.IsTrue(alive, "ApplyDelta should return true when energy > 0");
            Assert.IsNull(receivedEnding, "Should NOT fire GameOver event");
        }

        [Test]
        public void StatsSystem_EnergyZeroAt2359_GameOverBeforeEnding()
        {
            // Kiểm tra: Energy = 0 kích hoạt Game Over ngay lập tức,
            // ưu tiên trước cả EndingResolver dù xảy ra đúng lúc 23:59
            _state.minute = 359;
            _state.energy = 5;
            _state.progress = 100; // Đáng lẽ sẽ là Bad Ending

            EndingType? receivedEnding = null;
            EventBus.OnGameEnded += (ending) => receivedEnding = ending;

            StatsSystem.ApplyDelta(_state, -5, 0, 0);

            Assert.AreEqual(EndingType.GameOver, receivedEnding,
                "GameOver must take priority even at 23:59 with progress 100");
        }

        // =====================================================================
        // 4. TimeSystem — WorldPhase
        // =====================================================================

        [Test]
        public void TimeSystem_GetPhase_ReturnsCorrectPhases()
        {
            Assert.AreEqual(WorldPhase.Phase1_18_20, TimeSystem.GetPhase(0));
            Assert.AreEqual(WorldPhase.Phase1_18_20, TimeSystem.GetPhase(119));
            Assert.AreEqual(WorldPhase.Phase2_20_22, TimeSystem.GetPhase(120));
            Assert.AreEqual(WorldPhase.Phase2_20_22, TimeSystem.GetPhase(239));
            Assert.AreEqual(WorldPhase.Phase3_22_23, TimeSystem.GetPhase(240));
            Assert.AreEqual(WorldPhase.Phase3_22_23, TimeSystem.GetPhase(299));
            Assert.AreEqual(WorldPhase.Phase4_23_24, TimeSystem.GetPhase(300));
            Assert.AreEqual(WorldPhase.Phase4_23_24, TimeSystem.GetPhase(359));
        }

        [Test]
        public void TimeSystem_MinuteToDisplay_FormatsCorrectly()
        {
            Assert.AreEqual("18:00", TimeSystem.MinuteToDisplay(0));
            Assert.AreEqual("20:00", TimeSystem.MinuteToDisplay(120));
            Assert.AreEqual("22:00", TimeSystem.MinuteToDisplay(240));
            Assert.AreEqual("23:00", TimeSystem.MinuteToDisplay(300));
            Assert.AreEqual("23:30", TimeSystem.MinuteToDisplay(330));
            Assert.AreEqual("23:50", TimeSystem.MinuteToDisplay(350));
            Assert.AreEqual("23:59", TimeSystem.MinuteToDisplay(359));
        }

        // =====================================================================
        // 5. TimeSystem — Ngưỡng Stress thụ động (Checklist 5A #2)
        // =====================================================================

        [Test]
        public void TimeSystem_SingleThreshold_AddsStressOnce()
        {
            _state.minute = 230;
            _state.stress = 10;

            TimeSystem.Advance(_state, 15); // 230 + 15 = 245, vượt 240

            Assert.AreEqual(15, _state.stress, "Should add +5 stress for passing 22:00 threshold");
            Assert.IsTrue(_state.thresholdsApplied.Contains(240));
        }

        [Test]
        public void TimeSystem_SameThreshold_DoesNotAddStressTwice()
        {
            _state.minute = 230;
            _state.stress = 10;

            TimeSystem.Advance(_state, 15); // vượt 240 → +5
            Assert.AreEqual(15, _state.stress);

            TimeSystem.Advance(_state, 10); // vẫn >= 240 nhưng đã đánh dấu
            Assert.AreEqual(15, _state.stress, "Should NOT add stress again for same threshold");
        }

        [Test]
        public void TimeSystem_JumpTwoThresholds_AddsBothStress()
        {
            // Checklist 5A #2: nhảy qua 2 ngưỡng cùng lúc cộng đủ cả hai
            _state.minute = 230;
            _state.stress = 10;

            TimeSystem.Advance(_state, 75); // 230 + 75 = 305, vượt cả 240 (+5) và 300 (+10)

            Assert.AreEqual(25, _state.stress, "Should add +5 (22:00) and +10 (23:00) = +15 total");
            Assert.IsTrue(_state.thresholdsApplied.Contains(240));
            Assert.IsTrue(_state.thresholdsApplied.Contains(300));
        }

        [Test]
        public void TimeSystem_JumpAllThreeThresholds_AddsAllStress()
        {
            _state.minute = 200;
            _state.stress = 10;

            TimeSystem.Advance(_state, 140); // 200 + 140 = 340, vượt 240 (+5), 300 (+10), 330 (+15)

            Assert.AreEqual(40, _state.stress, "Should add +5 +10 +15 = +30 total");
            Assert.IsTrue(_state.thresholdsApplied.Contains(240));
            Assert.IsTrue(_state.thresholdsApplied.Contains(300));
            Assert.IsTrue(_state.thresholdsApplied.Contains(330));
        }

        [Test]
        public void TimeSystem_Advance_ClampsMinuteTo359()
        {
            _state.minute = 350;
            TimeSystem.Advance(_state, 20); // 350 + 20 = 370, should clamp to 359

            Assert.AreEqual(359, _state.minute, "minute should clamp to 359 (23:59)");
        }

        // =====================================================================
        // 6. TimeSystem — Luật cắt 23:50 (Checklist 5A #5)
        // =====================================================================

        [Test]
        public void TimeSystem_TrueEndingCut_StopsAt350WhenEligible()
        {
            // Đủ 3 Clue cốt lõi + secretArea → cắt ở 350
            _state.minute = 340;
            _state.cluesThisRun.Add("clue_core_passcode");
            _state.cluesThisRun.Add("clue_core_attempt_data");
            _state.cluesThisRun.Add("clue_core_loop_record");
            _state.secretAreaOpened = true;

            TimeSystem.Advance(_state, 20); // 340 + 20 = 360 nhưng phải cắt ở 350

            Assert.AreEqual(350, _state.minute, "Should cut to exactly 350 (23:50) for True Ending window");
        }

        [Test]
        public void TimeSystem_TrueEndingCut_DoesNotApplyWhenNotEligible()
        {
            // Chỉ có 2 Clue cốt lõi → không cắt
            _state.minute = 340;
            _state.cluesThisRun.Add("clue_core_passcode");
            _state.cluesThisRun.Add("clue_core_attempt_data");
            _state.secretAreaOpened = true;

            TimeSystem.Advance(_state, 20); // 340 + 20 = 360, clamp to 359

            Assert.AreEqual(359, _state.minute, "Should NOT cut when only 2 core clues");
        }

        [Test]
        public void TimeSystem_TrueEndingCut_DoesNotApplyWhenSecretAreaNotOpened()
        {
            // 3 Clue cốt lõi nhưng chưa mở SecretArea → không cắt
            _state.minute = 340;
            _state.cluesThisRun.Add("clue_core_passcode");
            _state.cluesThisRun.Add("clue_core_attempt_data");
            _state.cluesThisRun.Add("clue_core_loop_record");
            _state.secretAreaOpened = false;

            TimeSystem.Advance(_state, 20);

            Assert.AreEqual(359, _state.minute, "Should NOT cut when secretArea not opened");
        }

        [Test]
        public void TimeSystem_TrueEndingCut_DoesNotApplyWhenAlreadyPast350()
        {
            // Đã ở >= 350, không cắt lại
            _state.minute = 351;
            _state.cluesThisRun.Add("clue_core_passcode");
            _state.cluesThisRun.Add("clue_core_attempt_data");
            _state.cluesThisRun.Add("clue_core_loop_record");
            _state.secretAreaOpened = true;

            TimeSystem.Advance(_state, 5);

            Assert.AreEqual(356, _state.minute, "Should advance normally when already past 350");
        }

        [Test]
        public void TimeSystem_TrueEndingCut_DoesNotApplyWhenNotCrossing350()
        {
            // Hành động không vượt qua 350 → không cắt
            _state.minute = 340;
            _state.cluesThisRun.Add("clue_core_passcode");
            _state.cluesThisRun.Add("clue_core_attempt_data");
            _state.cluesThisRun.Add("clue_core_loop_record");
            _state.secretAreaOpened = true;

            TimeSystem.Advance(_state, 5); // 340 + 5 = 345, không vượt 350

            Assert.AreEqual(345, _state.minute, "Should advance normally when not crossing 350");
        }

        // =====================================================================
        // 7. EndingResolver — 5 Kết cục (Checklist 5A #4)
        // =====================================================================

        [Test]
        public void EndingResolver_GameOver_WhenEnergyZero()
        {
            _state.energy = 0;
            var result = EndingResolver.Resolve(_state, FinalChoice.None);
            Assert.AreEqual(EndingType.GameOver, result);
        }

        [Test]
        public void EndingResolver_TrueEnding_WhenAllConditionsMet()
        {
            _state.energy = 50;
            _state.minute = 355;
            _state.cluesThisRun.Add("clue_core_passcode");
            _state.cluesThisRun.Add("clue_core_attempt_data");
            _state.cluesThisRun.Add("clue_core_loop_record");
            _state.secretAreaOpened = true;

            var result = EndingResolver.Resolve(_state, FinalChoice.DeleteAndDisconnect);
            Assert.AreEqual(EndingType.True, result);
        }

        [Test]
        public void EndingResolver_TrueEnding_RequiresDeleteChoice()
        {
            _state.energy = 50;
            _state.minute = 355;
            _state.cluesThisRun.Add("clue_core_passcode");
            _state.cluesThisRun.Add("clue_core_attempt_data");
            _state.cluesThisRun.Add("clue_core_loop_record");
            _state.secretAreaOpened = true;

            // Chọn Submit thay vì Delete → không phải True Ending
            var result = EndingResolver.Resolve(_state, FinalChoice.SubmitProject);
            Assert.AreNotEqual(EndingType.True, result);
        }

        [Test]
        public void EndingResolver_TrueEnding_RequiresTimeWindow()
        {
            _state.energy = 50;
            _state.minute = 300; // Trước 23:50
            _state.cluesThisRun.Add("clue_core_passcode");
            _state.cluesThisRun.Add("clue_core_attempt_data");
            _state.cluesThisRun.Add("clue_core_loop_record");
            _state.secretAreaOpened = true;

            var result = EndingResolver.Resolve(_state, FinalChoice.DeleteAndDisconnect);
            Assert.AreNotEqual(EndingType.True, result, "True Ending requires minute >= 350");
        }

        [Test]
        public void EndingResolver_SecretEnding_WithMinorCluesAndAttempt27()
        {
            _state.energy = 50;
            _state.minute = 359;
            _state.cluesThisRun.Add("clue_minor_strange_note");
            _state.cluesThisRun.Add("clue_minor_old_photo");
            _state.cluesThisRun.Add("clue_minor_error_log");
            _state.attemptsOpenedThisRun.Add(27);

            var result = EndingResolver.Resolve(_state, FinalChoice.None);
            Assert.AreEqual(EndingType.Secret, result);
        }

        [Test]
        public void EndingResolver_SecretEnding_RequiresAttempt27()
        {
            _state.energy = 50;
            _state.minute = 359;
            _state.cluesThisRun.Add("clue_minor_strange_note");
            _state.cluesThisRun.Add("clue_minor_old_photo");
            _state.cluesThisRun.Add("clue_minor_error_log");
            // Không mở ATTEMPT_27

            var result = EndingResolver.Resolve(_state, FinalChoice.None);
            Assert.AreNotEqual(EndingType.Secret, result, "Secret requires ATTEMPT_27");
        }

        [Test]
        public void EndingResolver_BadEnding_WhenProgress100()
        {
            _state.energy = 50;
            _state.minute = 359;
            _state.progress = 100;

            var result = EndingResolver.Resolve(_state, FinalChoice.None);
            Assert.AreEqual(EndingType.Bad, result);
        }

        [Test]
        public void EndingResolver_NormalEnding_WhenNothingSpecial()
        {
            _state.energy = 50;
            _state.minute = 359;
            _state.progress = 80;

            var result = EndingResolver.Resolve(_state, FinalChoice.None);
            Assert.AreEqual(EndingType.Normal, result);
        }

        [Test]
        public void EndingResolver_SecretOverridesBad_WhenBothConditionsMet()
        {
            // Checklist 5A #4: progress == 100 nhưng đủ điều kiện Secret
            // → phải trả về Secret (ưu tiên cao hơn Bad)
            _state.energy = 50;
            _state.minute = 359;
            _state.progress = 100;
            _state.cluesThisRun.Add("clue_minor_strange_note");
            _state.cluesThisRun.Add("clue_minor_old_photo");
            _state.cluesThisRun.Add("clue_minor_error_log");
            _state.attemptsOpenedThisRun.Add(27);

            var result = EndingResolver.Resolve(_state, FinalChoice.None);
            Assert.AreEqual(EndingType.Secret, result,
                "Secret should override Bad when both conditions are met");
        }

        [Test]
        public void EndingResolver_TrueOverridesAll_WhenAllConditionsMet()
        {
            // Đủ điều kiện True + Secret + Bad cùng lúc → True thắng
            _state.energy = 50;
            _state.minute = 355;
            _state.progress = 100;
            _state.cluesThisRun.Add("clue_core_passcode");
            _state.cluesThisRun.Add("clue_core_attempt_data");
            _state.cluesThisRun.Add("clue_core_loop_record");
            _state.cluesThisRun.Add("clue_minor_strange_note");
            _state.cluesThisRun.Add("clue_minor_old_photo");
            _state.cluesThisRun.Add("clue_minor_error_log");
            _state.attemptsOpenedThisRun.Add(27);
            _state.secretAreaOpened = true;

            var result = EndingResolver.Resolve(_state, FinalChoice.DeleteAndDisconnect);
            Assert.AreEqual(EndingType.True, result,
                "True ending should override all other endings");
        }

        [Test]
        public void EndingResolver_GameOverOverridesTrue_WhenEnergyZero()
        {
            // GameOver ưu tiên hơn True Ending
            _state.energy = 0;
            _state.minute = 355;
            _state.cluesThisRun.Add("clue_core_passcode");
            _state.cluesThisRun.Add("clue_core_attempt_data");
            _state.cluesThisRun.Add("clue_core_loop_record");
            _state.secretAreaOpened = true;

            var result = EndingResolver.Resolve(_state, FinalChoice.DeleteAndDisconnect);
            Assert.AreEqual(EndingType.GameOver, result,
                "GameOver should override True ending when energy = 0");
        }

        // =====================================================================
        // 8. EventDirector — Priority & Once (Checklist 5A #6)
        // =====================================================================

        [Test]
        public void EventDirector_HighestPriority_Fires()
        {
            // Tạo 2 sự kiện đều thỏa điều kiện, priority khác nhau
            var evtLow = ScriptableObject.CreateInstance<GameEventDefinition>();
            evtLow.eventId = "E_LOW";
            evtLow.priority = 10;
            evtLow.once = true;
            evtLow.conditions = new Condition[0]; // Luôn thỏa

            var evtHigh = ScriptableObject.CreateInstance<GameEventDefinition>();
            evtHigh.eventId = "E_HIGH";
            evtHigh.priority = 50;
            evtHigh.once = true;
            evtHigh.conditions = new Condition[0];

            var events = new List<GameEventDefinition> { evtLow, evtHigh };

            string fired = EventDirector.EvaluatePure(events, _state);

            Assert.AreEqual("E_HIGH", fired,
                "Should fire highest priority event when multiple qualify");

            // Cleanup
            Object.DestroyImmediate(evtLow);
            Object.DestroyImmediate(evtHigh);
        }

        [Test]
        public void EventDirector_OnceEvent_DoesNotRepeat()
        {
            var evt = ScriptableObject.CreateInstance<GameEventDefinition>();
            evt.eventId = "E_ONCE";
            evt.priority = 30;
            evt.once = true;
            evt.conditions = new Condition[0];

            var events = new List<GameEventDefinition> { evt };

            // Lần 1: phát thành công
            string first = EventDirector.EvaluatePure(events, _state);
            Assert.AreEqual("E_ONCE", first);
            Assert.IsTrue(_state.firedEvents.Contains("E_ONCE"));

            // Lần 2: không phát lại
            string second = EventDirector.EvaluatePure(events, _state);
            Assert.IsNull(second, "Once event should NOT fire again");

            Object.DestroyImmediate(evt);
        }

        [Test]
        public void EventDirector_NonOnceEvent_CanRepeat()
        {
            var evt = ScriptableObject.CreateInstance<GameEventDefinition>();
            evt.eventId = "E_REPEAT";
            evt.priority = 20;
            evt.once = false; // Có thể lặp lại
            evt.conditions = new Condition[0];

            var events = new List<GameEventDefinition> { evt };

            string first = EventDirector.EvaluatePure(events, _state);
            Assert.AreEqual("E_REPEAT", first);

            string second = EventDirector.EvaluatePure(events, _state);
            Assert.AreEqual("E_REPEAT", second, "Non-once event should fire again");

            Object.DestroyImmediate(evt);
        }

        [Test]
        public void EventDirector_LowPriorityFires_AfterHighPriorityConsumed()
        {
            // Checklist 5A #6: khi sự kiện priority cao đã phát (once),
            // sự kiện priority thấp phát ở lượt kế tiếp
            var evtHigh = ScriptableObject.CreateInstance<GameEventDefinition>();
            evtHigh.eventId = "E_HIGH";
            evtHigh.priority = 50;
            evtHigh.once = true;
            evtHigh.conditions = new Condition[0];

            var evtLow = ScriptableObject.CreateInstance<GameEventDefinition>();
            evtLow.eventId = "E_LOW";
            evtLow.priority = 10;
            evtLow.once = true;
            evtLow.conditions = new Condition[0];

            var events = new List<GameEventDefinition> { evtHigh, evtLow };

            // Lần 1: E_HIGH phát
            string first = EventDirector.EvaluatePure(events, _state);
            Assert.AreEqual("E_HIGH", first);

            // Lần 2: E_HIGH đã bị tiêu thụ, E_LOW phát
            string second = EventDirector.EvaluatePure(events, _state);
            Assert.AreEqual("E_LOW", second,
                "After high-priority once-event consumed, low-priority should fire");

            Object.DestroyImmediate(evtHigh);
            Object.DestroyImmediate(evtLow);
        }

        [Test]
        public void EventDirector_ConditionNotMet_DoesNotFire()
        {
            var evt = ScriptableObject.CreateInstance<GameEventDefinition>();
            evt.eventId = "E_COND";
            evt.priority = 30;
            evt.once = true;
            evt.conditions = new Condition[]
            {
                new Condition { type = ConditionType.ProgressMin, intValue = 50 }
            };

            var events = new List<GameEventDefinition> { evt };

            _state.progress = 30; // Không đủ
            string fired = EventDirector.EvaluatePure(events, _state);
            Assert.IsNull(fired, "Event should not fire when conditions not met");

            _state.progress = 50; // Đủ
            fired = EventDirector.EvaluatePure(events, _state);
            Assert.AreEqual("E_COND", fired, "Event should fire when conditions met");

            Object.DestroyImmediate(evt);
        }

        [Test]
        public void EventDirector_StressDelta_IsApplied()
        {
            var evt = ScriptableObject.CreateInstance<GameEventDefinition>();
            evt.eventId = "E_STRESS";
            evt.priority = 30;
            evt.once = true;
            evt.stressDelta = 15;
            evt.conditions = new Condition[0];

            var events = new List<GameEventDefinition> { evt };
            _state.stress = 20;

            EventDirector.EvaluatePure(events, _state);

            Assert.AreEqual(35, _state.stress, "Stress should increase by event's stressDelta");

            Object.DestroyImmediate(evt);
        }

        [Test]
        public void EventDirector_ClueReward_IsGranted()
        {
            var evt = ScriptableObject.CreateInstance<GameEventDefinition>();
            evt.eventId = "E_CLUE";
            evt.priority = 30;
            evt.once = true;
            evt.clueRewardId = "clue_core_passcode";
            evt.clueIsCore = true;
            evt.conditions = new Condition[0];

            var events = new List<GameEventDefinition> { evt };

            EventDirector.EvaluatePure(events, _state);

            Assert.IsTrue(_state.cluesThisRun.Contains("clue_core_passcode"),
                "Clue reward should be added to RunState");

            Object.DestroyImmediate(evt);
        }

        // =====================================================================
        // 9. ConditionEvaluator — Đánh giá điều kiện
        // =====================================================================

        [Test]
        public void ConditionEvaluator_TimeRange_WithMax()
        {
            var cond = new Condition { type = ConditionType.TimeRange, intValue = 100, intValueMax = 200 };

            _state.minute = 50;
            Assert.IsFalse(ConditionEvaluator.Evaluate(cond, _state));

            _state.minute = 150;
            Assert.IsTrue(ConditionEvaluator.Evaluate(cond, _state));

            _state.minute = 250;
            Assert.IsFalse(ConditionEvaluator.Evaluate(cond, _state));
        }

        [Test]
        public void ConditionEvaluator_TimeRange_MinOnly()
        {
            var cond = new Condition { type = ConditionType.TimeRange, intValue = 240, intValueMax = 0 };

            _state.minute = 200;
            Assert.IsFalse(ConditionEvaluator.Evaluate(cond, _state));

            _state.minute = 240;
            Assert.IsTrue(ConditionEvaluator.Evaluate(cond, _state));

            _state.minute = 350;
            Assert.IsTrue(ConditionEvaluator.Evaluate(cond, _state));
        }

        [Test]
        public void ConditionEvaluator_InArea()
        {
            var cond = new Condition { type = ConditionType.InArea, areaValue = AreaId.Library };

            _state.currentArea = AreaId.Dorm;
            Assert.IsFalse(ConditionEvaluator.Evaluate(cond, _state));

            _state.currentArea = AreaId.Library;
            Assert.IsTrue(ConditionEvaluator.Evaluate(cond, _state));
        }

        [Test]
        public void ConditionEvaluator_HasClue()
        {
            var cond = new Condition { type = ConditionType.HasClue, stringValue = "clue_core_passcode" };

            Assert.IsFalse(ConditionEvaluator.Evaluate(cond, _state));

            _state.cluesThisRun.Add("clue_core_passcode");
            Assert.IsTrue(ConditionEvaluator.Evaluate(cond, _state));
        }

        [Test]
        public void ConditionEvaluator_CoreClueCountMin()
        {
            var cond = new Condition { type = ConditionType.CoreClueCountMin, intValue = 3 };

            Assert.IsFalse(ConditionEvaluator.Evaluate(cond, _state));

            _state.cluesThisRun.Add("clue_core_passcode");
            _state.cluesThisRun.Add("clue_core_attempt_data");
            Assert.IsFalse(ConditionEvaluator.Evaluate(cond, _state));

            _state.cluesThisRun.Add("clue_core_loop_record");
            Assert.IsTrue(ConditionEvaluator.Evaluate(cond, _state));
        }

        [Test]
        public void ConditionEvaluator_EvaluateAll_EmptyIsTrue()
        {
            Assert.IsTrue(ConditionEvaluator.EvaluateAll(null, _state));
            Assert.IsTrue(ConditionEvaluator.EvaluateAll(new Condition[0], _state));
        }

        [Test]
        public void ConditionEvaluator_EvaluateAll_AllMustPass()
        {
            var conditions = new Condition[]
            {
                new Condition { type = ConditionType.ProgressMin, intValue = 50 },
                new Condition { type = ConditionType.InArea, areaValue = AreaId.Dorm }
            };

            _state.progress = 60;
            _state.currentArea = AreaId.Dorm;
            Assert.IsTrue(ConditionEvaluator.EvaluateAll(conditions, _state));

            _state.currentArea = AreaId.Library;
            Assert.IsFalse(ConditionEvaluator.EvaluateAll(conditions, _state),
                "Should fail when one condition is not met");
        }

        // =====================================================================
        // 10. ClueSystem — ATTEMPT mở đọc
        // =====================================================================

        [Test]
        public void ClueSystem_OpenAttempt_FirstTimeInRun_AddsStress()
        {
            _state.stress = 10;

            bool result = ClueSystem.OpenAttempt(_state, _memory, 1);

            Assert.IsTrue(result);
            Assert.AreEqual(20, _state.stress, "First-time ATTEMPT should add +10 stress");
            Assert.IsTrue(_state.attemptsOpenedThisRun.Contains(1));
            Assert.IsTrue(_memory.HasUnlockedAttempt(1));
        }

        [Test]
        public void ClueSystem_OpenAttempt_SecondTimeInRun_NoExtraStress()
        {
            _state.stress = 10;

            ClueSystem.OpenAttempt(_state, _memory, 1);
            Assert.AreEqual(20, _state.stress);

            ClueSystem.OpenAttempt(_state, _memory, 1);
            Assert.AreEqual(20, _state.stress, "Second open should NOT add stress again");
        }

        [Test]
        public void ClueSystem_OpenAttempt27_RequiresConditions()
        {
            _state.progress = 50;
            _state.minute = 200;

            bool result = ClueSystem.OpenAttempt(_state, _memory, 27);
            Assert.IsFalse(result, "ATTEMPT_27 needs progress >= 70 or minute >= 240");

            _state.progress = 70;
            result = ClueSystem.OpenAttempt(_state, _memory, 27);
            Assert.IsTrue(result, "ATTEMPT_27 should open when progress >= 70");
        }

        [Test]
        public void ClueSystem_OpenAttempt27_WithTimeCondition()
        {
            _state.progress = 50;
            _state.minute = 240;

            bool result = ClueSystem.OpenAttempt(_state, _memory, 27);
            Assert.IsTrue(result, "ATTEMPT_27 should open when minute >= 240");
        }

        [Test]
        public void ClueSystem_OpenAttempt_PersistsToMemory()
        {
            ClueSystem.OpenAttempt(_state, _memory, 5);

            Assert.IsTrue(_memory.HasUnlockedAttempt(5));

            // Simulate new run
            _state.Reset();
            Assert.IsFalse(_state.attemptsOpenedThisRun.Contains(5),
                "Run state should be cleared");
            Assert.IsTrue(_memory.HasUnlockedAttempt(5),
                "Memory should still have the attempt");
        }

        [Test]
        public void ClueSystem_IsCoreClue_IdentifiesCorrectly()
        {
            Assert.IsTrue(ClueSystem.IsCoreClue("clue_core_passcode"));
            Assert.IsTrue(ClueSystem.IsCoreClue("clue_core_attempt_data"));
            Assert.IsTrue(ClueSystem.IsCoreClue("clue_core_loop_record"));
            Assert.IsFalse(ClueSystem.IsCoreClue("clue_minor_strange_note"));
            Assert.IsFalse(ClueSystem.IsCoreClue("random_string"));
        }

        // =====================================================================
        // 11. MemoryData — Persistence
        // =====================================================================

        [Test]
        public void MemoryData_DefaultSettings()
        {
            Assert.AreEqual(0.8f, _memory.musicVolume);
            Assert.AreEqual(0.8f, _memory.sfxVolume);
            Assert.AreEqual(1.0f, _memory.brightness);
        }

        [Test]
        public void MemoryData_AddUnlockedAttempt_NoDuplicates()
        {
            _memory.AddUnlockedAttempt(5);
            _memory.AddUnlockedAttempt(5);
            _memory.AddUnlockedAttempt(5);

            int count = 0;
            foreach (var a in _memory.unlockedAttempts)
            {
                if (a == 5) count++;
            }
            Assert.AreEqual(1, count, "Should not have duplicates");
        }

        // =====================================================================
        // 12. Tích hợp: Pipeline hành động đầy đủ
        // =====================================================================

        [Test]
        public void Integration_ActionCode_DrainStats()
        {
            // Simulate act_code: 20min, -15 energy, +3 stress, +15 progress
            _state.minute = 0;
            _state.energy = 100;
            _state.stress = 10;
            _state.progress = 0;

            TimeSystem.Advance(_state, 20);
            StatsSystem.ApplyDelta(_state, -15, +3, +15);

            Assert.AreEqual(20, _state.minute);
            Assert.AreEqual(85, _state.energy);
            Assert.AreEqual(13, _state.stress);
            Assert.AreEqual(15, _state.progress);
        }

        [Test]
        public void Integration_ActionEat_RestoresEnergy()
        {
            // Simulate act_eat: 15min, +20 energy, -5 stress, 0 progress
            _state.minute = 60;
            _state.energy = 50;
            _state.stress = 40;
            _state.progress = 30;

            TimeSystem.Advance(_state, 15);
            StatsSystem.ApplyDelta(_state, +20, -5, 0);

            Assert.AreEqual(75, _state.minute);
            Assert.AreEqual(70, _state.energy);
            Assert.AreEqual(35, _state.stress);
            Assert.AreEqual(30, _state.progress);
        }

        [Test]
        public void Integration_ActionRest_RestoresEnergyReducesStress()
        {
            // Simulate act_rest: 30min, +30 energy, -10 stress, 0 progress
            _state.minute = 100;
            _state.energy = 30;
            _state.stress = 60;
            _state.progress = 50;

            TimeSystem.Advance(_state, 30);
            StatsSystem.ApplyDelta(_state, +30, -10, 0);

            Assert.AreEqual(130, _state.minute);
            Assert.AreEqual(60, _state.energy);
            Assert.AreEqual(50, _state.stress);
            Assert.AreEqual(50, _state.progress);
        }

        [Test]
        public void Integration_FullPlaythrough_NormalEnding()
        {
            // Chơi bình thường, code vừa đủ, đến 23:59 → Normal Ending
            _state.energy = 50;
            _state.progress = 80;
            _state.minute = 359;

            var ending = EndingResolver.Resolve(_state, FinalChoice.None);
            Assert.AreEqual(EndingType.Normal, ending);
        }

        [Test]
        public void Integration_FullPlaythrough_BadEnding()
        {
            // Hoàn thành đồ án 100% nhưng không biết bí mật → Bad Ending
            _state.energy = 50;
            _state.progress = 100;
            _state.minute = 359;

            var ending = EndingResolver.Resolve(_state, FinalChoice.SubmitProject);
            Assert.AreEqual(EndingType.Bad, ending);
        }
    }
}

// ============================================================================
// HUDController.cs — Bộ điều khiển giao diện HUD chính trong gameplay (M10)
// Đồng hồ, Energy (cảnh báo <= 24), Stress (viền đỏ >= 81), Progress, Prompt 'E', Toast
// ============================================================================

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeadlineRush.Core;

namespace DeadlineRush.UI
{
    /// <summary>
    /// Điều khiển hiển thị thanh trạng thái thời gian thực trong gameplay.
    /// Không bao giờ can thiệp trực tiếp vào RunState; chỉ đăng ký lắng nghe EventBus.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        public static HUDController Instance { get; private set; }

        [Header("Time & Clock")]
        [SerializeField] private TextMeshProUGUI clockText;
        [SerializeField] private TextMeshProUGUI phaseText;

        [Header("Project Progress")]
        [SerializeField] private Slider progressSlider;
        [SerializeField] private TextMeshProUGUI progressText;

        [Header("Energy System")]
        [SerializeField] private Slider energySlider;
        [SerializeField] private TextMeshProUGUI energyText;
        [SerializeField] private GameObject exhaustedWarningObj; // Cảnh báo "Bạn đang kiệt sức" khi <= 24
        [SerializeField] private Image energyFillImage;

        [Header("Stress System")]
        [SerializeField] private Slider stressSlider;
        [SerializeField] private TextMeshProUGUI stressText;
        [SerializeField] private GameObject stressRedBorderObj; // Viền đỏ cảnh báo khi >= 81
        [SerializeField] private Image stressFillImage;

        [Header("Interaction & Toast")]
        [SerializeField] private GameObject interactionPromptObj;
        [SerializeField] private TextMeshProUGUI interactionPromptText;
        [SerializeField] private GameObject toastPanelObj;
        [SerializeField] private TextMeshProUGUI toastText;

        // Màu cảnh báo theo giờ
        private static readonly Color ColorNormalTime = Color.white;
        private static readonly Color ColorWarn22 = new Color(0.95f, 0.77f, 0.06f); // Vàng (< 2 giờ)
        private static readonly Color ColorWarn23 = new Color(0.90f, 0.49f, 0.13f); // Cam (< 1 giờ)
        private static readonly Color ColorWarn2330 = new Color(0.91f, 0.30f, 0.24f); // Đỏ (< 30 phút)

        private Coroutine _toastCoroutine;
        private Coroutine _exhaustedBlinkCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            HideInteractionPrompt();
            if (toastPanelObj != null) toastPanelObj.SetActive(false);
            if (exhaustedWarningObj != null) exhaustedWarningObj.SetActive(false);
            if (stressRedBorderObj != null) stressRedBorderObj.SetActive(false);
        }

        private void OnEnable()
        {
            EventBus.OnTimeChanged += HandleTimeChanged;
            EventBus.OnStatsChanged += HandleStatsChanged;
            EventBus.OnClueAdded += HandleClueAdded;
            EventBus.OnGameEventTriggered += HandleGameEventTriggered;
        }

        private void OnDisable()
        {
            EventBus.OnTimeChanged -= HandleTimeChanged;
            EventBus.OnStatsChanged -= HandleStatsChanged;
            EventBus.OnClueAdded -= HandleClueAdded;
            EventBus.OnGameEventTriggered -= HandleGameEventTriggered;
        }

        // ─── Cập nhật Thời gian ───────────────────────────────────────────
        private void HandleTimeChanged(int minute, WorldPhase phase)
        {
            int totalMin = Mathf.Clamp(minute, 0, GameConfig.MaxMinutes);
            int hour = 18 + totalMin / 60;
            int min = totalMin % 60;
            string timeString = $"{hour:00}:{min:00}";

            if (clockText != null)
            {
                clockText.text = timeString;

                // Đổi màu cảnh báo đồng hồ theo 3 mốc quan trọng
                if (totalMin >= 330) // >= 23:30 (còn < 30 phút)
                {
                    clockText.color = ColorWarn2330;
                }
                else if (totalMin >= 300) // >= 23:00 (còn < 1 giờ)
                {
                    clockText.color = ColorWarn23;
                }
                else if (totalMin >= 240) // >= 22:00 (còn < 2 giờ)
                {
                    clockText.color = ColorWarn22;
                }
                else
                {
                    clockText.color = ColorNormalTime;
                }
            }

            if (phaseText != null)
            {
                phaseText.text = GetPhaseDisplayName(phase);
            }
        }

        private string GetPhaseDisplayName(WorldPhase phase)
        {
            switch (phase)
            {
                case WorldPhase.Phase1_18_20: return "Khởi đầu (18:00 - 20:00)";
                case WorldPhase.Phase2_20_22: return "Nghi ngờ (20:00 - 22:00)";
                case WorldPhase.Phase3_22_23: return "Cao trào kinh dị (22:00 - 23:00)";
                case WorldPhase.Phase4_23_24: return "Lựa chọn cuối (23:00 - 23:59)";
                default: return string.Empty;
            }
        }

        // ─── Cập nhật Chỉ số ──────────────────────────────────────────────
        private void HandleStatsChanged(int energy, int stress, int progress)
        {
            // Progress
            if (progressSlider != null) progressSlider.value = progress;
            if (progressText != null) progressText.text = $"Tiến độ: {progress}%";

            // Energy
            if (energySlider != null) energySlider.value = energy;
            if (energyText != null) energyText.text = $"Energy: {energy}/100";

            // Quy tắc Energy <= 24: nhấp nháy + cảnh báo kiệt sức
            bool isExhausted = energy <= GameConfig.EnergyLowThreshold;
            if (exhaustedWarningObj != null)
            {
                exhaustedWarningObj.SetActive(isExhausted);
            }
            if (isExhausted)
            {
                if (_exhaustedBlinkCoroutine == null)
                    _exhaustedBlinkCoroutine = StartCoroutine(BlinkEnergyWarningRoutine());
            }
            else
            {
                if (_exhaustedBlinkCoroutine != null)
                {
                    StopCoroutine(_exhaustedBlinkCoroutine);
                    _exhaustedBlinkCoroutine = null;
                }
                if (energyFillImage != null) energyFillImage.color = Color.green;
            }

            // Stress
            if (stressSlider != null) stressSlider.value = stress;
            if (stressText != null) stressText.text = $"Stress: {stress}/100";

            // Quy tắc Stress >= 81: viền đỏ cảnh báo
            bool isHighStress = stress >= GameConfig.StressHighThreshold;
            if (stressRedBorderObj != null)
            {
                stressRedBorderObj.SetActive(isHighStress);
            }
            if (stressFillImage != null)
            {
                stressFillImage.color = isHighStress ? Color.red : Color.yellow;
            }
        }

        private IEnumerator BlinkEnergyWarningRoutine()
        {
            while (true)
            {
                if (energyFillImage != null) energyFillImage.color = Color.red;
                yield return new WaitForSeconds(0.4f);
                if (energyFillImage != null) energyFillImage.color = new Color(0.8f, 0.2f, 0.2f, 0.3f);
                yield return new WaitForSeconds(0.4f);
            }
        }

        // ─── Lời nhắc tương tác 'E' ──────────────────────────────────────
        public void ShowInteractionPrompt(string customText = "E — Tương tác")
        {
            if (interactionPromptObj != null) interactionPromptObj.SetActive(true);
            if (interactionPromptText != null) interactionPromptText.text = customText;
        }

        public void HideInteractionPrompt()
        {
            if (interactionPromptObj != null) interactionPromptObj.SetActive(false);
        }

        // ─── Toast Notifications ──────────────────────────────────────────
        private void HandleClueAdded(string clueId, bool isCore)
        {
            var clue = ContentDataProvider.GetClue(clueId);
            string title = clue != null ? clue.title : clueId;
            string typeStr = isCore ? "[MANH MỐI CỐT LÕI]" : "[Manh mối phụ]";
            ShowToast($"{typeStr} Đã tìm thấy: {title}!");
        }

        private void HandleGameEventTriggered(string eventId)
        {
            if (eventId == "E01") ShowToast("⚠️ Có gì đó vừa thay đổi vị trí trong phòng...");
            else if (eventId == "E04") ShowToast("⚠️ Tiếng gõ cửa vang lên từ phía cuối hành lang...");
            else if (eventId == "E07") ShowToast("⚠️ PROJECT COMPLETED — SUBMISSION SUCCESSFUL!");
            else if (eventId == "E11") ShowToast("⏰ Thời gian đang trôi qua rất nhanh...");
        }

        public void ShowToast(string message, float duration = 3.5f)
        {
            if (_toastCoroutine != null) StopCoroutine(_toastCoroutine);
            _toastCoroutine = StartCoroutine(ToastRoutine(message, duration));
        }

        private IEnumerator ToastRoutine(string message, float duration)
        {
            if (toastPanelObj != null) toastPanelObj.SetActive(true);
            if (toastText != null) toastText.text = message;

            yield return new WaitForSeconds(duration);

            if (toastPanelObj != null) toastPanelObj.SetActive(false);
            _toastCoroutine = null;
        }
    }
}

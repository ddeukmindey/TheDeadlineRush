// ============================================================================
// ActionConfirmModal.cs — Modal xác nhận chi phí hành động (M10, M3)
// Lắng nghe OnActionConfirmRequested, hiển thị chi phí dự kiến, Enter/E xác nhận, Esc hủy
// ============================================================================

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeadlineRush.Core;

namespace DeadlineRush.UI
{
    /// <summary>
    /// Popup xác nhận chi phí trước khi thực hiện hành động tốn thời gian.
    /// Đảm bảo tính toán minh bạch chi phí trước khi người chơi chấp nhận.
    /// </summary>
    public class ActionConfirmModal : MonoBehaviour
    {
        public static ActionConfirmModal Instance { get; private set; }

        [Header("UI Panel")]
        [SerializeField] private GameObject modalPanel;
        [SerializeField] private TextMeshProUGUI actionTitleText;
        [SerializeField] private TextMeshProUGUI costTimeText;
        [SerializeField] private TextMeshProUGUI costEnergyText;
        [SerializeField] private TextMeshProUGUI costStressText;
        [SerializeField] private TextMeshProUGUI costProgressText;

        [Header("Buttons")]
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        public bool IsOpen { get; private set; }
        private Action _pendingConfirmCallback;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (modalPanel != null) modalPanel.SetActive(false);
            if (confirmButton != null) confirmButton.onClick.AddListener(Confirm);
            if (cancelButton != null) cancelButton.onClick.AddListener(Cancel);
        }

        private void OnEnable()
        {
            EventBus.OnActionConfirmRequested += HandleActionConfirmRequested;
        }

        private void OnDisable()
        {
            EventBus.OnActionConfirmRequested -= HandleActionConfirmRequested;
        }

        private void Update()
        {
            if (!IsOpen) return;

            // Bấm E hoặc Enter để xác nhận
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                Confirm();
                return;
            }

            // Bấm Esc để hủy
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cancel();
                return;
            }
        }

        private void HandleActionConfirmRequested(ActionCostPreview preview, Action onConfirm)
        {
            Open(preview, onConfirm);
        }

        public void Open(ActionCostPreview preview, Action onConfirm)
        {
            if (preview == null) return;

            _pendingConfirmCallback = onConfirm;
            IsOpen = true;

            EventBus.RaiseUIModalStateChanged(true);

            if (modalPanel != null) modalPanel.SetActive(true);

            if (actionTitleText != null)
                actionTitleText.text = $"XÁC NHẬN: {preview.displayName.ToUpper()}";

            if (costTimeText != null)
                costTimeText.text = $"⏱ Thời gian tiêu tốn: +{preview.minutes} phút";

            if (costEnergyText != null)
            {
                string sign = preview.dEnergy >= 0 ? "+" : "";
                costEnergyText.text = $"⚡ Thể lực (Energy): {sign}{preview.dEnergy}";
                costEnergyText.color = preview.dEnergy < 0 ? Color.red : Color.green;
            }

            if (costStressText != null)
            {
                string sign = preview.dStress >= 0 ? "+" : "";
                costStressText.text = $"🧠 Căng thẳng (Stress): {sign}{preview.dStress}";
                costStressText.color = preview.dStress > 0 ? Color.red : Color.green;
            }

            if (costProgressText != null)
            {
                string sign = preview.dProgress >= 0 ? "+" : "";
                costProgressText.text = $"📊 Tiến độ đồ án: {sign}{preview.dProgress}%";
                costProgressText.color = preview.dProgress > 0 ? Color.cyan : Color.white;
            }

            Debug.Log($"[ActionConfirmModal] Opened for action: {preview.displayName} ({preview.minutes} mins)");
        }

        public void Confirm()
        {
            IsOpen = false;
            if (modalPanel != null) modalPanel.SetActive(false);

            EventBus.RaiseUIModalStateChanged(false);

            var cb = _pendingConfirmCallback;
            _pendingConfirmCallback = null;

            cb?.Invoke();
            Debug.Log("[ActionConfirmModal] Action confirmed.");
        }

        public void Cancel()
        {
            IsOpen = false;
            _pendingConfirmCallback = null;

            if (modalPanel != null) modalPanel.SetActive(false);

            EventBus.RaiseUIModalStateChanged(false);
            Debug.Log("[ActionConfirmModal] Action canceled by player.");
        }
    }
}

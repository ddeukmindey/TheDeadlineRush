// ============================================================================
// PhoneUI.cs — Giao diện điện thoại & Tin nhắn bí ẩn (E02)
// Lắng nghe OnGameEventTriggered("E02"), hiển thị 3 lựa chọn, khóa phím Esc
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DeadlineRush.Core;

namespace DeadlineRush.UI
{
    /// <summary>
    /// Giao diện điện thoại thông minh của nhân vật.
    /// Kích hoạt khi nhận tin nhắn bí ẩn E02: "Đừng hoàn thành nó."
    /// </summary>
    public class PhoneUI : MonoBehaviour
    {
        public static PhoneUI Instance { get; private set; }

        [Header("UI Panels")]
        [SerializeField] private GameObject phonePanel;
        [SerializeField] private TextMeshProUGUI senderText;
        [SerializeField] private TextMeshProUGUI messageBodyText;
        [SerializeField] private Transform optionsContainer;
        [SerializeField] private GameObject optionPrefab;

        public bool IsOpen { get; private set; }
        public int SelectedOptionIndex { get; private set; }

        // Danh sách 3 lựa chọn chuẩn theo GDD
        public static readonly string FlagDelete = "flag_msg_delete";
        public static readonly string FlagReply = "flag_msg_reply";
        public static readonly string FlagInvestigate = "flag_msg_investigate";

        private readonly List<(string text, string flag)> _options = new List<(string text, string flag)>
        {
            ("Xóa tin nhắn (Bỏ qua cảnh báo)", FlagDelete),
            ("Trả lời tin nhắn ('Mày là ai?')", FlagReply),
            ("Điều tra nguồn gốc (Truy vết IP)", FlagInvestigate)
        };

        public event Action<string> OnOptionSelected;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (phonePanel != null) phonePanel.SetActive(false);
        }

        private void OnEnable()
        {
            EventBus.OnGameEventTriggered += HandleGameEvent;
        }

        private void OnDisable()
        {
            EventBus.OnGameEventTriggered -= HandleGameEvent;
        }

        private void Update()
        {
            if (!IsOpen) return;

            // Xử lý điều hướng W/S, Up/Down
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                SelectedOptionIndex = (SelectedOptionIndex - 1 + _options.Count) % _options.Count;
                UpdateOptionVisuals();
            }
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                SelectedOptionIndex = (SelectedOptionIndex + 1) % _options.Count;
                UpdateOptionVisuals();
            }

            // Xác nhận lựa chọn
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                SelectOption(SelectedOptionIndex);
                return;
            }

            // Quy tắc chống né lựa chọn: Esc bị khóa hoàn toàn trong E02
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Debug.Log("[PhoneUI] Cannot close Phone during mandatory mystery message (E02). You must choose!");
            }
        }

        private void HandleGameEvent(string eventId)
        {
            if (eventId == "E02")
            {
                OpenMysteryMessage();
            }
        }

        /// <summary>
        /// Mở tin nhắn bí ẩn E02.
        /// </summary>
        public void OpenMysteryMessage()
        {
            IsOpen = true;
            SelectedOptionIndex = 0;

            EventBus.RaiseUIModalStateChanged(true);

            if (phonePanel != null) phonePanel.SetActive(true);
            if (senderText != null) senderText.text = "TIN NHẮN TỪ SỐ LẠ [ẨN DANH]";
            if (messageBodyText != null) messageBodyText.text = "“Đừng hoàn thành nó.”";

            UpdateOptionVisuals();
            Debug.Log("[PhoneUI] E02 Mystery Message opened.");
        }

        private void UpdateOptionVisuals()
        {
            if (optionsContainer == null) return;

            foreach (Transform child in optionsContainer)
            {
                Destroy(child.gameObject);
            }

            for (int i = 0; i < _options.Count; i++)
            {
                var opt = _options[i];
                if (optionPrefab != null)
                {
                    var go = Instantiate(optionPrefab, optionsContainer);
                    var text = go.GetComponentInChildren<TextMeshProUGUI>();
                    if (text != null)
                    {
                        string prefix = (i == SelectedOptionIndex) ? "> " : "  ";
                        text.text = prefix + opt.text;
                        text.color = (i == SelectedOptionIndex) ? Color.yellow : Color.white;
                    }
                }
            }
        }

        public void SelectOption(int index)
        {
            if (index < 0 || index >= _options.Count) return;

            string selectedFlag = _options[index].flag;

            // Thêm flag vào RunState
            var run = GameManager.Instance != null ? GameManager.Instance.CurrentRun : null;
            if (run != null)
            {
                run.flags.Add(selectedFlag);
                Debug.Log($"[PhoneUI] Player chose option: {_options[index].text}. Flag: {selectedFlag}");
            }

            OnOptionSelected?.Invoke(selectedFlag);
            ClosePhone();

            // Nếu người chơi chọn Trả lời hoặc Điều tra, phát đoạn hội thoại phản hồi tiếp theo
            if (selectedFlag == FlagReply)
            {
                EventBus.RaiseDialogueRequested("phone_reply", false);
            }
            else if (selectedFlag == FlagInvestigate)
            {
                EventBus.RaiseDialogueRequested("phone_investigate", false);
            }
        }

        public void ClosePhone()
        {
            IsOpen = false;
            if (phonePanel != null) phonePanel.SetActive(false);

            EventBus.RaiseUIModalStateChanged(false);
            Debug.Log("[PhoneUI] Phone closed.");
        }
    }
}

// ============================================================================
// DialogueRunner.cs — Hệ thống trình chiếu hội thoại và lựa chọn cốt truyện (M7)
// Lắng nghe EventBus.OnDialogueRequested, điều hướng W/S, Enter/E, khóa Esc khi bắt buộc
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DeadlineRush.Core;

namespace DeadlineRush.UI
{
    /// <summary>
    /// Điều khiển hiển thị hộp thoại, lựa chọn nhánh và ghi cờ (flags) vào RunState.
    /// </summary>
    public class DialogueRunner : MonoBehaviour
    {
        public static DialogueRunner Instance { get; private set; }

        [Header("UI Components (Optional for headless/testing)")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TextMeshProUGUI speakerLabel;
        [SerializeField] private TextMeshProUGUI contentLabel;
        [SerializeField] private Transform choicesContainer;
        [SerializeField] private GameObject choiceItemPrefab;

        // Trạng thái hiện tại
        public bool IsOpen { get; private set; }
        public bool IsMandatory { get; private set; }
        public DialogueRow CurrentDialogue { get; private set; }
        public int SelectedChoiceIndex { get; private set; }

        public event Action<DialogueRow> OnDialogueStarted;
        public event Action<DialogueChoice> OnChoiceSelected;
        public event Action OnDialogueClosed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            EventBus.OnDialogueRequested += HandleDialogueRequested;
        }

        private void OnDisable()
        {
            EventBus.OnDialogueRequested -= HandleDialogueRequested;
        }

        private void Update()
        {
            if (!IsOpen) return;

            HandleInput();
        }

        // ─── Xử lý Input điều khiển ───────────────────────────────────────
        private void HandleInput()
        {
            // Nếu có danh sách lựa chọn
            if (CurrentDialogue != null && CurrentDialogue.choices != null && CurrentDialogue.choices.Count > 0)
            {
                int choiceCount = CurrentDialogue.choices.Count;

                // Điều hướng lên: W hoặc UpArrow
                if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                {
                    SelectedChoiceIndex = (SelectedChoiceIndex - 1 + choiceCount) % choiceCount;
                    UpdateChoiceVisuals();
                }
                // Điều hướng xuống: S hoặc DownArrow
                else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                {
                    SelectedChoiceIndex = (SelectedChoiceIndex + 1) % choiceCount;
                    UpdateChoiceVisuals();
                }

                // Xác nhận: E hoặc Enter hoặc KeypadEnter
                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    ConfirmSelection(SelectedChoiceIndex);
                    return;
                }
            }
            else
            {
                // Không có lựa chọn: bấm E, Enter hoặc Space để đóng
                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                {
                    CloseDialogue();
                    return;
                }
            }

            // Quy tắc bắt buộc: Chống né lựa chọn
            // Khi isMandatory == true, khóa phím Esc hoàn toàn!
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (IsMandatory)
                {
                    Debug.Log("[DialogueRunner] Esc key is blocked during mandatory dialogue choice.");
                }
                else
                {
                    CloseDialogue();
                }
            }
        }

        // ─── Khởi chạy hội thoại ──────────────────────────────────────────
        private void HandleDialogueRequested(string groupId, bool mandatory)
        {
            StartDialogue(groupId, mandatory);
        }

        public void StartDialogue(string groupId, bool mandatory = false)
        {
            var run = GameManager.Instance != null ? GameManager.Instance.CurrentRun : null;
            int minute = run?.minute ?? 0;
            int stress = run?.stress ?? 0;
            var flags = run?.flags;

            DialogueRow row = ContentDataProvider.FindBestDialogue(groupId, minute, stress, flags);
            if (row == null)
            {
                // Nếu không có câu thoại khớp trong data, sinh câu thoại dự phòng
                row = new DialogueRow
                {
                    id = "dlg_fallback_" + groupId,
                    groupId = groupId,
                    speaker = "Hệ thống",
                    text = $"[Thông điệp sự kiện: {groupId}]",
                    minMinute = 0,
                    maxMinute = 359,
                    minStress = 0,
                    choices = new List<DialogueChoice>()
                };
            }

            DisplayDialogue(row, mandatory);
        }

        public void DisplayDialogue(DialogueRow row, bool mandatory)
        {
            CurrentDialogue = row;
            IsMandatory = mandatory;
            IsOpen = true;
            SelectedChoiceIndex = 0;

            // Thông báo cho hệ thống để khóa di chuyển nhân vật
            EventBus.RaiseUIModalStateChanged(true);

            if (dialoguePanel != null) dialoguePanel.SetActive(true);
            if (speakerLabel != null) speakerLabel.text = row.speaker;
            if (contentLabel != null) contentLabel.text = row.text;

            UpdateChoiceVisuals();
            OnDialogueStarted?.Invoke(row);

            Debug.Log($"[DialogueRunner] Opened: [{row.speaker}] {row.text} (Mandatory: {mandatory})");
        }

        private void UpdateChoiceVisuals()
        {
            if (choicesContainer == null) return;

            // Xóa các lựa chọn cũ nếu có
            foreach (Transform child in choicesContainer)
            {
                Destroy(child.gameObject);
            }

            if (CurrentDialogue == null || CurrentDialogue.choices == null) return;

            for (int i = 0; i < CurrentDialogue.choices.Count; i++)
            {
                var choice = CurrentDialogue.choices[i];
                if (choiceItemPrefab != null)
                {
                    var go = Instantiate(choiceItemPrefab, choicesContainer);
                    var text = go.GetComponentInChildren<TextMeshProUGUI>();
                    if (text != null)
                    {
                        string prefix = (i == SelectedChoiceIndex) ? "> " : "  ";
                        text.text = prefix + choice.text;
                        text.color = (i == SelectedChoiceIndex) ? Color.yellow : Color.white;
                    }
                }
            }
        }

        public void ConfirmSelection(int index)
        {
            if (CurrentDialogue == null || CurrentDialogue.choices == null)
            {
                CloseDialogue();
                return;
            }

            if (index >= 0 && index < CurrentDialogue.choices.Count)
            {
                var chosen = CurrentDialogue.choices[index];

                // Ghi nhận flag vào RunState (nếu có)
                if (!string.IsNullOrEmpty(chosen.flag))
                {
                    var run = GameManager.Instance != null ? GameManager.Instance.CurrentRun : null;
                    if (run != null)
                    {
                        run.flags.Add(chosen.flag);
                        Debug.Log($"[DialogueRunner] Flag added to RunState: {chosen.flag}");
                    }
                }

                OnChoiceSelected?.Invoke(chosen);
            }

            CloseDialogue();
        }

        public void CloseDialogue()
        {
            IsOpen = false;
            CurrentDialogue = null;
            SelectedChoiceIndex = 0;

            if (dialoguePanel != null) dialoguePanel.SetActive(false);

            // Mở khóa Player di chuyển
            EventBus.RaiseUIModalStateChanged(false);
            OnDialogueClosed?.Invoke();

            Debug.Log("[DialogueRunner] Closed.");
        }
    }
}

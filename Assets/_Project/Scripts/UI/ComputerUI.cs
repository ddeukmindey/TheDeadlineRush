// ============================================================================
// ComputerUI.cs — Màn hình Terminal máy tính (M10, M3, M6, M9)
// Code đồ án, Đọc 28 tệp ATTEMPT, Lựa chọn True Ending (23:50-23:59)
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeadlineRush.Core;
using DeadlineRush.Systems;

namespace DeadlineRush.UI
{
    /// <summary>
    /// Giao diện máy tính phòng KTX và máy tính phòng Lab.
    /// Cho phép Code đồ án, kiểm tra tiến độ, duyệt tệp ATTEMPT và đưa ra lựa chọn cuối.
    /// </summary>
    public class ComputerUI : MonoBehaviour
    {
        public static ComputerUI Instance { get; private set; }

        [Header("Main Panels")]
        [SerializeField] private GameObject computerPanel;
        [SerializeField] private GameObject normalTabPanel;
        [SerializeField] private GameObject finalChoicePanel;

        [Header("Project Progress Display")]
        [SerializeField] private Slider progressSlider;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private GameObject submissionSuccessfulBanner;

        [Header("Actions")]
        [SerializeField] private Button codeButton;
        [SerializeField] private ActionDefinition codeActionDef; // ScriptableObject nếu có

        [Header("ATTEMPT Viewer")]
        [SerializeField] private Transform attemptListContainer;
        [SerializeField] private GameObject attemptListItemPrefab;
        [SerializeField] private TextMeshProUGUI attemptTitleText;
        [SerializeField] private TextMeshProUGUI attemptContentText;

        [Header("Final Choice (23:50 - 23:59)")]
        [SerializeField] private Button submitProjectButton;
        [SerializeField] private Button deleteAndDisconnectButton;

        [Header("Close")]
        [SerializeField] private Button closeButton;

        public bool IsOpen { get; private set; }
        public bool IsInFinalChoice { get; private set; }
        private int _currentSelectedAttempt = 1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (computerPanel != null) computerPanel.SetActive(false);
            if (finalChoicePanel != null) finalChoicePanel.SetActive(false);

            if (codeButton != null) codeButton.onClick.AddListener(OnCodeButtonClicked);
            if (submitProjectButton != null) submitProjectButton.onClick.AddListener(OnSubmitProjectClicked);
            if (deleteAndDisconnectButton != null) deleteAndDisconnectButton.onClick.AddListener(OnDeleteAndDisconnectClicked);
            if (closeButton != null) closeButton.onClick.AddListener(CloseComputer);
        }

        private void OnEnable()
        {
            EventBus.OnStatsChanged += HandleStatsChanged;
            EventBus.OnTimeChanged += HandleTimeChanged;
            EventBus.OnAttemptOpened += HandleAttemptOpened;
        }

        private void OnDisable()
        {
            EventBus.OnStatsChanged -= HandleStatsChanged;
            EventBus.OnTimeChanged -= HandleTimeChanged;
            EventBus.OnAttemptOpened -= HandleAttemptOpened;
        }

        private void Update()
        {
            if (!IsOpen) return;

            // Nếu đang trong cửa sổ lựa chọn cuối bắt buộc thì khóa phím Esc
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (IsInFinalChoice)
                {
                    Debug.Log("[ComputerUI] Cannot close Computer during final choice (23:50-23:59)!");
                }
                else
                {
                    CloseComputer();
                }
            }
        }

        // ─── Mở & Đóng Máy tính ───────────────────────────────────────────
        public void OpenComputer()
        {
            IsOpen = true;
            EventBus.RaiseUIModalStateChanged(true);

            if (computerPanel != null) computerPanel.SetActive(true);

            RefreshUI();
            CheckFinalChoiceConditions();
            SelectAttempt(1);

            Debug.Log("[ComputerUI] Computer terminal opened.");
        }

        public void CloseComputer()
        {
            if (IsInFinalChoice) return;

            IsOpen = false;
            if (computerPanel != null) computerPanel.SetActive(false);

            EventBus.RaiseUIModalStateChanged(false);
            Debug.Log("[ComputerUI] Computer terminal closed.");
        }

        // ─── Làm mới giao diện ────────────────────────────────────────────
        private void RefreshUI()
        {
            var run = GameManager.Instance != null ? GameManager.Instance.CurrentRun : null;
            int progress = run?.progress ?? 0;

            if (progressSlider != null) progressSlider.value = progress;
            if (progressText != null) progressText.text = $"TIẾN ĐỘ ĐỒ ÁN: {progress}%";

            // Khi đạt 100%
            bool isComplete = progress >= 100;
            if (submissionSuccessfulBanner != null)
            {
                submissionSuccessfulBanner.SetActive(isComplete);
            }

            PopulateAttemptList();
        }

        private void CheckFinalChoiceConditions()
        {
            var run = GameManager.Instance != null ? GameManager.Instance.CurrentRun : null;
            if (run == null) return;

            // Kiểm tra điều kiện mở Final Choice lúc 23:50–23:59:
            // minute >= 350, đủ 3 Clue cốt lõi, đã mở SecretArea
            bool canShowFinalChoice = run.minute >= GameConfig.FinalChoiceMinuteMin &&
                                      run.CoreClueCount == 3 &&
                                      run.secretAreaOpened;

            IsInFinalChoice = canShowFinalChoice;
            if (finalChoicePanel != null) finalChoicePanel.SetActive(canShowFinalChoice);
            if (normalTabPanel != null) normalTabPanel.SetActive(!canShowFinalChoice);
        }

        private void HandleStatsChanged(int energy, int stress, int progress)
        {
            if (IsOpen) RefreshUI();
        }

        private void HandleTimeChanged(int minute, WorldPhase phase)
        {
            if (IsOpen) CheckFinalChoiceConditions();
        }

        private void HandleAttemptOpened(int attemptNum)
        {
            if (IsOpen)
            {
                PopulateAttemptList();
                SelectAttempt(attemptNum);
            }
        }

        // ─── Hành động Code Đồ án ─────────────────────────────────────────
        public void OnCodeButtonClicked()
        {
            if (codeActionDef != null && ActionSystem.Instance != null)
            {
                ActionSystem.Instance.RequestPerform(codeActionDef);
            }
            else
            {
                // Fallback: Gửi ActionCostPreview qua EventBus
                var preview = new ActionCostPreview
                {
                    actionId = "act_code",
                    displayName = "Code đồ án",
                    minutes = 20,
                    dEnergy = -15,
                    dStress = 3,
                    dProgress = 15
                };

                EventBus.RaiseActionConfirmRequested(preview, () =>
                {
                    if (codeActionDef != null && ActionSystem.Instance != null)
                    {
                        ActionSystem.Instance.RequestPerform(codeActionDef);
                    }
                });
            }
        }

        // ─── Đọc các tệp ATTEMPT (1..28) ──────────────────────────────────
        private void PopulateAttemptList()
        {
            if (attemptListContainer == null) return;

            foreach (Transform child in attemptListContainer)
            {
                Destroy(child.gameObject);
            }

            var run = GameManager.Instance != null ? GameManager.Instance.CurrentRun : null;
            var mem = GameManager.Instance != null ? GameManager.Instance.Memory : null;

            for (int i = 1; i <= GameConfig.TotalAttempts; i++)
            {
                int num = i;
                bool isUnlocked = mem != null && mem.HasUnlockedAttempt(num);
                bool isOpenedInRun = run != null && run.attemptsOpenedThisRun.Contains(num);

                if (attemptListItemPrefab != null)
                {
                    var item = Instantiate(attemptListItemPrefab, attemptListContainer);
                    var text = item.GetComponentInChildren<TextMeshProUGUI>();
                    var btn = item.GetComponent<Button>();

                    if (text != null)
                    {
                        string status = isOpenedInRun ? "[ĐÃ ĐỌC]" : (isUnlocked ? "[KÝ ỨC]" : "[CHƯA MỞ]");
                        text.text = $"ATTEMPT_{num:00} {status}";
                    }

                    if (btn != null)
                    {
                        btn.onClick.AddListener(() => OnAttemptItemClicked(num));
                    }
                }
            }
        }

        public void OnAttemptItemClicked(int attemptNum)
        {
            var run = GameManager.Instance != null ? GameManager.Instance.CurrentRun : null;
            var mem = GameManager.Instance != null ? GameManager.Instance.Memory : null;

            if (run != null && mem != null)
            {
                bool success = ClueSystem.OpenAttempt(run, mem, attemptNum);
                if (success)
                {
                    SelectAttempt(attemptNum);
                }
                else
                {
                    if (attemptTitleText != null) attemptTitleText.text = $"ATTEMPT_{attemptNum:00} [TRUY CẬP BỊ TỪ CHỐI]";
                    if (attemptContentText != null)
                        attemptContentText.text = "Bạn chưa đủ điều kiện để giải mã tệp tin này (Yêu cầu Tiến độ >= 70% hoặc sau 22:00).";
                }
            }
            else
            {
                SelectAttempt(attemptNum);
            }
        }

        public void SelectAttempt(int attemptNum)
        {
            _currentSelectedAttempt = attemptNum;
            var data = ContentDataProvider.GetAttempt(attemptNum);

            if (data != null)
            {
                if (attemptTitleText != null) attemptTitleText.text = data.title;
                if (attemptContentText != null) attemptContentText.text = $"{data.timestamp}\n\n{data.content}";
            }
            else
            {
                if (attemptTitleText != null) attemptTitleText.text = $"ATTEMPT_{attemptNum:00}";
                if (attemptContentText != null) attemptContentText.text = "[Nội dung đang được giải mã...]";
            }
        }

        // ─── Lựa chọn cuối cùng (Final Choice) ─────────────────────────────
        public void OnSubmitProjectClicked()
        {
            Debug.Log("[ComputerUI] Player chose: Submit Project!");
            ResolveFinalChoice(FinalChoice.SubmitProject);
        }

        public void OnDeleteAndDisconnectClicked()
        {
            Debug.Log("[ComputerUI] Player chose: Delete Project Copy and Disconnect!");
            ResolveFinalChoice(FinalChoice.DeleteAndDisconnect);
        }

        private void ResolveFinalChoice(FinalChoice choice)
        {
            var run = GameManager.Instance != null ? GameManager.Instance.CurrentRun : null;
            if (run == null) return;

            EndingType ending = EndingResolver.Resolve(run, choice);
            CloseComputer();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerEnding(ending);
            }
            else
            {
                EventBus.RaiseGameEnded(ending);
            }
        }
    }
}

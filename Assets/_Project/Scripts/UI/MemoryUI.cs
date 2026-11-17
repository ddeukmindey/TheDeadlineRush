// ============================================================================
// MemoryUI.cs — Mục Ký ức vĩnh viễn qua mọi Run (M10, M6)
// Tách biệt hoàn toàn với JournalUI. Đọc từ MemoryData.unlockedAttempts (giữ nguyên khi New Game)
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeadlineRush.Core;

namespace DeadlineRush.UI
{
    /// <summary>
    /// Giao diện Ký Ức (Memory Archive): lưu trữ các tệp ATTEMPT đã từng mở qua TẤT CẢ các lần chơi.
    /// Không bao giờ bị xóa khi người chơi bấm New Game.
    /// </summary>
    public class MemoryUI : MonoBehaviour
    {
        public static MemoryUI Instance { get; private set; }

        [Header("Panels")]
        [SerializeField] private GameObject memoryPanel;
        [SerializeField] private Transform attemptGridContainer;
        [SerializeField] private GameObject attemptButtonPrefab;
        [SerializeField] private TextMeshProUGUI statsSummaryText;

        [Header("Reading Area")]
        [SerializeField] private TextMeshProUGUI selectedAttemptTitleText;
        [SerializeField] private TextMeshProUGUI selectedAttemptTimestampText;
        [SerializeField] private TextMeshProUGUI selectedAttemptContentText;

        [Header("Close")]
        [SerializeField] private Button closeButton;

        public bool IsOpen { get; private set; }
        private int _selectedAttemptNum = 1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (memoryPanel != null) memoryPanel.SetActive(false);
            if (closeButton != null) closeButton.onClick.AddListener(CloseMemory);
        }

        private void OnEnable()
        {
            EventBus.OnAttemptOpened += HandleAttemptOpened;
        }

        private void OnDisable()
        {
            EventBus.OnAttemptOpened -= HandleAttemptOpened;
        }

        private void Update()
        {
            // Phím tắt mở/đóng Ký ức: phím M
            if (Input.GetKeyDown(KeyCode.M))
            {
                ToggleMemory();
            }

            if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                CloseMemory();
            }
        }

        public void ToggleMemory()
        {
            if (IsOpen) CloseMemory();
            else OpenMemory();
        }

        public void OpenMemory()
        {
            IsOpen = true;
            EventBus.RaiseUIModalStateChanged(true);

            if (memoryPanel != null) memoryPanel.SetActive(true);

            RefreshMemoryGrid();
            SelectAttempt(_selectedAttemptNum);
            Debug.Log("[MemoryUI] Permanent Memory Archive opened.");
        }

        public void CloseMemory()
        {
            IsOpen = false;
            if (memoryPanel != null) memoryPanel.SetActive(false);

            EventBus.RaiseUIModalStateChanged(false);
            Debug.Log("[MemoryUI] Permanent Memory Archive closed.");
        }

        private void HandleAttemptOpened(int num)
        {
            if (IsOpen) RefreshMemoryGrid();
        }

        public void RefreshMemoryGrid()
        {
            if (attemptGridContainer == null) return;

            foreach (Transform child in attemptGridContainer)
            {
                Destroy(child.gameObject);
            }

            var mem = GameManager.Instance != null ? GameManager.Instance.Memory : null;
            int unlockedCount = 0;

            for (int i = 1; i <= GameConfig.TotalAttempts; i++)
            {
                int num = i;
                bool isUnlocked = mem != null && mem.HasUnlockedAttempt(num);
                if (isUnlocked) unlockedCount++;

                if (attemptButtonPrefab != null)
                {
                    var go = Instantiate(attemptButtonPrefab, attemptGridContainer);
                    var text = go.GetComponentInChildren<TextMeshProUGUI>();
                    var btn = go.GetComponent<Button>();

                    if (text != null)
                    {
                        text.text = isUnlocked ? $"ATTEMPT #{num:00}" : $"#{num:00} [???]";
                        text.color = isUnlocked ? (num == 27 ? Color.yellow : Color.white) : Color.gray;
                    }

                    if (btn != null)
                    {
                        btn.onClick.AddListener(() => SelectAttempt(num));
                    }
                }
            }

            if (statsSummaryText != null)
            {
                statsSummaryText.text = $"KÝ ỨC ĐÃ MỞ KHÓA: {unlockedCount}/{GameConfig.TotalAttempts} TỆP";
            }
        }

        public void SelectAttempt(int attemptNum)
        {
            _selectedAttemptNum = attemptNum;
            var mem = GameManager.Instance != null ? GameManager.Instance.Memory : null;
            bool isUnlocked = mem != null && mem.HasUnlockedAttempt(attemptNum);

            if (!isUnlocked)
            {
                if (selectedAttemptTitleText != null)
                    selectedAttemptTitleText.text = $"ATTEMPT #{attemptNum:00} — [BỊ KHÓA]";
                if (selectedAttemptTimestampText != null)
                    selectedAttemptTimestampText.text = "Thời gian: [KHÔNG XÁC ĐỊNH]";
                if (selectedAttemptContentText != null)
                    selectedAttemptContentText.text = "Tệp tin nhật ký này chưa từng được giải mã trong bất kỳ vòng lặp nào.\nHãy khám phá máy tính KTX và phòng máy khi tiến độ cao hoặc sau 22:00 để mở khóa!";
                return;
            }

            var data = ContentDataProvider.GetAttempt(attemptNum);
            if (data != null)
            {
                if (selectedAttemptTitleText != null) selectedAttemptTitleText.text = data.title;
                if (selectedAttemptTimestampText != null) selectedAttemptTimestampText.text = data.timestamp;
                if (selectedAttemptContentText != null) selectedAttemptContentText.text = data.content;
            }
            else
            {
                if (selectedAttemptTitleText != null) selectedAttemptTitleText.text = $"ATTEMPT #{attemptNum:00}";
                if (selectedAttemptTimestampText != null) selectedAttemptTimestampText.text = "";
                if (selectedAttemptContentText != null) selectedAttemptContentText.text = "[Dữ liệu tệp tin đang được phục hồi...]";
            }
        }
    }
}

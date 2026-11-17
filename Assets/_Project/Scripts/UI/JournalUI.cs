// ============================================================================
// JournalUI.cs — Sổ tay manh mối (Clue Journal) trong Run hiện tại (M10, M6)
// Tách biệt hoàn toàn với MemoryUI. Chỉ đọc từ RunState.cluesThisRun (reset mỗi Run)
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
    /// Sổ tay ghi chép manh mối thu thập ĐƯỢC TRONG LƯỢT CHƠI HIỆN TẠI.
    /// Reset sạch sẽ khi bắt đầu New Game. Phân chia 2 Tab: Cốt lõi (Core) và Phụ (Minor).
    /// </summary>
    public class JournalUI : MonoBehaviour
    {
        public static JournalUI Instance { get; private set; }

        [Header("Panels")]
        [SerializeField] private GameObject journalPanel;
        [SerializeField] private Button coreCluesTabButton;
        [SerializeField] private Button minorCluesTabButton;
        [SerializeField] private Transform clueListContainer;
        [SerializeField] private GameObject clueListItemPrefab;

        [Header("Clue Details")]
        [SerializeField] private TextMeshProUGUI tabSummaryText;
        [SerializeField] private TextMeshProUGUI selectedClueTitleText;
        [SerializeField] private TextMeshProUGUI selectedClueDescriptionText;
        [SerializeField] private TextMeshProUGUI selectedClueHintText;

        [Header("Close Button")]
        [SerializeField] private Button closeButton;

        public bool IsOpen { get; private set; }
        public bool IsViewingCoreTab { get; private set; } = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (journalPanel != null) journalPanel.SetActive(false);

            if (coreCluesTabButton != null) coreCluesTabButton.onClick.AddListener(() => SwitchTab(true));
            if (minorCluesTabButton != null) minorCluesTabButton.onClick.AddListener(() => SwitchTab(false));
            if (closeButton != null) closeButton.onClick.AddListener(CloseJournal);
        }

        private void OnEnable()
        {
            EventBus.OnClueAdded += HandleClueAdded;
        }

        private void OnDisable()
        {
            EventBus.OnClueAdded -= HandleClueAdded;
        }

        private void Update()
        {
            // Phím tắt mở/đóng sổ tay: phím J
            if (Input.GetKeyDown(KeyCode.J))
            {
                ToggleJournal();
            }

            if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                CloseJournal();
            }
        }

        public void ToggleJournal()
        {
            if (IsOpen) CloseJournal();
            else OpenJournal();
        }

        public void OpenJournal()
        {
            IsOpen = true;
            EventBus.RaiseUIModalStateChanged(true);

            if (journalPanel != null) journalPanel.SetActive(true);

            SwitchTab(IsViewingCoreTab);
            Debug.Log("[JournalUI] Clue Journal opened.");
        }

        public void CloseJournal()
        {
            IsOpen = false;
            if (journalPanel != null) journalPanel.SetActive(false);

            EventBus.RaiseUIModalStateChanged(false);
            Debug.Log("[JournalUI] Clue Journal closed.");
        }

        public void SwitchTab(bool viewCore)
        {
            IsViewingCoreTab = viewCore;
            RefreshClueList();
        }

        private void HandleClueAdded(string clueId, bool isCore)
        {
            if (IsOpen) RefreshClueList();
        }

        public void RefreshClueList()
        {
            if (clueListContainer == null) return;

            foreach (Transform child in clueListContainer)
            {
                Destroy(child.gameObject);
            }

            var run = GameManager.Instance != null ? GameManager.Instance.CurrentRun : null;
            var collectedClues = run?.cluesThisRun ?? new HashSet<string>();

            var allClues = ContentDataProvider.GetAllClues();
            var filteredClues = new List<ClueData>();

            foreach (var c in allClues)
            {
                if (c.isCore == IsViewingCoreTab)
                {
                    filteredClues.Add(c);
                }
            }

            int collectedCount = 0;
            foreach (var c in filteredClues)
            {
                if (collectedClues.Contains(c.id)) collectedCount++;
            }

            if (tabSummaryText != null)
            {
                string tabName = IsViewingCoreTab ? "MANH MỐI CỐT LÕI (Cần cho True Ending)" : "MANH MỐI PHỤ (Bí mật vòng lặp)";
                tabSummaryText.text = $"{tabName}: {collectedCount}/{filteredClues.Count}";
            }

            bool hasSelectedFirst = false;

            for (int i = 0; i < filteredClues.Count; i++)
            {
                var clue = filteredClues[i];
                bool isCollected = collectedClues.Contains(clue.id);

                if (clueListItemPrefab != null)
                {
                    var go = Instantiate(clueListItemPrefab, clueListContainer);
                    var text = go.GetComponentInChildren<TextMeshProUGUI>();
                    var btn = go.GetComponent<Button>();

                    if (text != null)
                    {
                        text.text = isCollected ? $"✔ {clue.title}" : "🔒 ??? (Chưa tìm thấy)";
                        text.color = isCollected ? Color.white : Color.gray;
                    }

                    if (btn != null)
                    {
                        btn.interactable = isCollected;
                        btn.onClick.AddListener(() => DisplayClueDetails(clue, isCollected));
                    }
                }

                if (isCollected && !hasSelectedFirst)
                {
                    DisplayClueDetails(clue, true);
                    hasSelectedFirst = true;
                }
            }

            if (!hasSelectedFirst)
            {
                DisplayClueDetails(null, false);
            }
        }

        private void DisplayClueDetails(ClueData clue, bool isCollected)
        {
            if (clue == null || !isCollected)
            {
                if (selectedClueTitleText != null) selectedClueTitleText.text = "CHƯA KHÁM PHÁ";
                if (selectedClueDescriptionText != null) selectedClueDescriptionText.text = "Bạn chưa tìm thấy manh mối này trong lượt chơi hiện tại.";
                if (selectedClueHintText != null) selectedClueHintText.text = "Gợi ý: Hãy khám phá môi trường, thư viện hoặc nói chuyện với NPC.";
                return;
            }

            if (selectedClueTitleText != null) selectedClueTitleText.text = clue.title.ToUpper();
            if (selectedClueDescriptionText != null) selectedClueDescriptionText.text = clue.description;
            if (selectedClueHintText != null) selectedClueHintText.text = $"Nguồn manh mối: {clue.foundHint}";
        }
    }
}

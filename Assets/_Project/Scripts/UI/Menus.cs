// ============================================================================
// Menus.cs — Hệ thống Menu chính, Cài đặt, Tạm dừng và Màn hình Kết thúc (M10)
// MainMenuUI (TUYỆT ĐỐI KHÔNG CÓ NÚT CONTINUE), SettingsUI, PauseMenuUI, EndingUI
// ============================================================================

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeadlineRush.Core;

namespace DeadlineRush.UI
{
    // ─── 1. MainMenuUI ────────────────────────────────────────────────────────
    /// <summary>
    /// Giao diện Menu chính.
    /// QUY TẮC BẮT BUỘC: Tuyệt đối không tạo nút Continue / Checkpoint.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        public static MainMenuUI Instance { get; private set; }

        [Header("Menu Panel")]
        [SerializeField] private GameObject menuPanel;

        [Header("Buttons")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (newGameButton != null) newGameButton.onClick.AddListener(OnNewGameClicked);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
            if (exitButton != null) exitButton.onClick.AddListener(OnExitClicked);
        }

        public void Open()
        {
            if (menuPanel != null) menuPanel.SetActive(true);
        }

        public void Close()
        {
            if (menuPanel != null) menuPanel.SetActive(false);
        }

        public void OnNewGameClicked()
        {
            Debug.Log("[MainMenuUI] New Game clicked. Starting fresh run...");
            Close();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartNewRun();
            }
        }

        public void OnSettingsClicked()
        {
            if (SettingsUI.Instance != null)
            {
                SettingsUI.Instance.Open();
            }
        }

        public void OnExitClicked()
        {
            Debug.Log("[MainMenuUI] Exit clicked. Quitting application...");
            Application.Quit();
        }
    }

    // ─── 2. SettingsUI ────────────────────────────────────────────────────────
    /// <summary>
    /// Giao diện cài đặt: Âm lượng nhạc, Âm lượng hiệu ứng, Độ sáng.
    /// Dữ liệu lưu vĩnh viễn trong MemoryData.
    /// </summary>
    public class SettingsUI : MonoBehaviour
    {
        public static SettingsUI Instance { get; private set; }

        [Header("Panel")]
        [SerializeField] private GameObject settingsPanel;

        [Header("Sliders")]
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Slider brightnessSlider;

        [Header("Buttons")]
        [SerializeField] private Button backButton;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (settingsPanel != null) settingsPanel.SetActive(false);

            if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            if (brightnessSlider != null) brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
            if (backButton != null) backButton.onClick.AddListener(Close);
        }

        public void Open()
        {
            if (settingsPanel != null) settingsPanel.SetActive(true);

            var mem = GameManager.Instance != null ? GameManager.Instance.Memory : null;
            if (mem != null)
            {
                if (musicVolumeSlider != null) musicVolumeSlider.value = mem.musicVolume;
                if (sfxVolumeSlider != null) sfxVolumeSlider.value = mem.sfxVolume;
                if (brightnessSlider != null) brightnessSlider.value = mem.brightness;
            }
        }

        public void Close()
        {
            var mem = GameManager.Instance != null ? GameManager.Instance.Memory : null;
            if (mem != null)
            {
                SaveService.Save(mem);
                Debug.Log("[SettingsUI] Settings saved to disk.");
            }

            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        private void OnMusicVolumeChanged(float val)
        {
            var mem = GameManager.Instance != null ? GameManager.Instance.Memory : null;
            if (mem != null) mem.musicVolume = val;
        }

        private void OnSfxVolumeChanged(float val)
        {
            var mem = GameManager.Instance != null ? GameManager.Instance.Memory : null;
            if (mem != null) mem.sfxVolume = val;
        }

        private void OnBrightnessChanged(float val)
        {
            var mem = GameManager.Instance != null ? GameManager.Instance.Memory : null;
            if (mem != null) mem.brightness = val;
        }
    }

    // ─── 3. PauseMenuUI ───────────────────────────────────────────────────────
    /// <summary>
    /// Menu tạm dừng khi bấm Esc trong lúc chơi (thời gian vẫn chạy để giữ áp lực).
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        public static PauseMenuUI Instance { get; private set; }

        [Header("Panel")]
        [SerializeField] private GameObject pausePanel;

        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button returnToMenuButton;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (pausePanel != null) pausePanel.SetActive(false);

            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (returnToMenuButton != null) returnToMenuButton.onClick.AddListener(ReturnToMenu);
        }

        private void Update()
        {
            // Bấm Esc để bật/tắt pause menu (nếu không có modal bắt buộc nào đang mở)
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // Kiểm tra xem Dialogue hay Phone hay Computer có đang chặn không
                if (DialogueRunner.Instance != null && DialogueRunner.Instance.IsOpen) return;
                if (PhoneUI.Instance != null && PhoneUI.Instance.IsOpen) return;
                if (ComputerUI.Instance != null && ComputerUI.Instance.IsInFinalChoice) return;
                if (ActionConfirmModal.Instance != null && ActionConfirmModal.Instance.IsOpen) return;
                if (PasscodeKeypadUI.Instance != null && PasscodeKeypadUI.Instance.IsOpen) return;

                Toggle();
            }
        }

        public void Toggle()
        {
            if (IsOpen) Resume();
            else Pause();
        }

        public void Pause()
        {
            IsOpen = true;
            if (pausePanel != null) pausePanel.SetActive(true);
            EventBus.RaiseUIModalStateChanged(true);
        }

        public void Resume()
        {
            IsOpen = false;
            if (pausePanel != null) pausePanel.SetActive(false);
            EventBus.RaiseUIModalStateChanged(false);
        }

        private void OpenSettings()
        {
            if (SettingsUI.Instance != null) SettingsUI.Instance.Open();
        }

        private void ReturnToMenu()
        {
            Resume();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToMainMenu();
            }
            if (MainMenuUI.Instance != null)
            {
                MainMenuUI.Instance.Open();
            }
        }
    }

    // ─── 4. EndingUI ──────────────────────────────────────────────────────────
    /// <summary>
    /// Màn hình kết thúc: hiển thị kết cục theo EndingType (True, Secret, Bad, Normal, GameOver).
    /// </summary>
    public class EndingUI : MonoBehaviour
    {
        public static EndingUI Instance { get; private set; }

        [Header("Panel")]
        [SerializeField] private GameObject endingPanel;

        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI endingTitleText;
        [SerializeField] private TextMeshProUGUI endingSubtitleText;
        [SerializeField] private TextMeshProUGUI endingDescriptionText;
        [SerializeField] private TextMeshProUGUI endingDetailsText;

        [Header("Buttons")]
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button returnToMenuButton;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (endingPanel != null) endingPanel.SetActive(false);

            if (playAgainButton != null) playAgainButton.onClick.AddListener(OnPlayAgainClicked);
            if (returnToMenuButton != null) returnToMenuButton.onClick.AddListener(OnReturnToMenuClicked);
        }

        private void OnEnable()
        {
            EventBus.OnGameEnded += HandleGameEnded;
        }

        private void OnDisable()
        {
            EventBus.OnGameEnded -= HandleGameEnded;
        }

        private void HandleGameEnded(EndingType ending)
        {
            ShowEnding(ending);
        }

        public void ShowEnding(EndingType ending)
        {
            IsOpen = true;
            EventBus.RaiseUIModalStateChanged(true);

            if (endingPanel != null) endingPanel.SetActive(true);

            var data = ContentDataProvider.GetEnding(ending);
            if (data != null)
            {
                if (endingTitleText != null)
                {
                    endingTitleText.text = data.title;
                    endingTitleText.color = GetEndingColor(ending);
                }
                if (endingSubtitleText != null) endingSubtitleText.text = data.subtitle;
                if (endingDescriptionText != null) endingDescriptionText.text = data.description;
                if (endingDetailsText != null) endingDetailsText.text = data.details;
            }
            else
            {
                if (endingTitleText != null) endingTitleText.text = ending.ToString().ToUpper();
                if (endingSubtitleText != null) endingSubtitleText.text = "Trò chơi kết thúc.";
                if (endingDescriptionText != null) endingDescriptionText.text = "";
                if (endingDetailsText != null) endingDetailsText.text = "";
            }

            Debug.Log($"[EndingUI] Displayed Ending: {ending}");
        }

        private Color GetEndingColor(EndingType ending)
        {
            switch (ending)
            {
                case EndingType.True: return Color.cyan;
                case EndingType.Secret: return Color.yellow;
                case EndingType.Bad: return Color.red;
                case EndingType.GameOver: return new Color(0.8f, 0.1f, 0.1f);
                case EndingType.Normal: return Color.white;
                default: return Color.white;
            }
        }

        public void OnPlayAgainClicked()
        {
            if (endingPanel != null) endingPanel.SetActive(false);
            IsOpen = false;
            EventBus.RaiseUIModalStateChanged(false);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartNewRun();
            }
        }

        public void OnReturnToMenuClicked()
        {
            if (endingPanel != null) endingPanel.SetActive(false);
            IsOpen = false;
            EventBus.RaiseUIModalStateChanged(false);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToMainMenu();
            }
            if (MainMenuUI.Instance != null)
            {
                MainMenuUI.Instance.Open();
            }
        }
    }
}

// ============================================================================
// PasscodeKeypadUI.cs — Bàn phím số nhập mật mã cửa phòng bí mật (M10, M4)
// Kiểm tra mật mã "2728", mở khóa SecretArea, hỗ trợ phím số và chuột
// ============================================================================

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeadlineRush.Core;

namespace DeadlineRush.UI
{
    /// <summary>
    /// Giao diện bàn phím số điện tử để mở khóa cửa phòng bí mật cuối hành lang.
    /// Mật mã đúng là "2728".
    /// </summary>
    public class PasscodeKeypadUI : MonoBehaviour
    {
        public static PasscodeKeypadUI Instance { get; private set; }

        [Header("UI Panel")]
        [SerializeField] private GameObject keypadPanel;
        [SerializeField] private TextMeshProUGUI inputDisplayText;
        [SerializeField] private TextMeshProUGUI statusMessageText;

        [Header("Buttons")]
        [SerializeField] private Button[] numberButtons; // 0..9
        [SerializeField] private Button clearButton;
        [SerializeField] private Button submitButton;
        [SerializeField] private Button closeButton;

        public bool IsOpen { get; private set; }
        public string CurrentInput { get; private set; } = string.Empty;

        public event Action OnPasscodeSuccess;
        public event Action OnPasscodeFailed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (keypadPanel != null) keypadPanel.SetActive(false);

            // Gán listener cho các nút số
            if (numberButtons != null)
            {
                for (int i = 0; i < numberButtons.Length; i++)
                {
                    int digit = i;
                    if (numberButtons[i] != null)
                    {
                        numberButtons[i].onClick.AddListener(() => AddDigit(digit.ToString()));
                    }
                }
            }

            if (clearButton != null) clearButton.onClick.AddListener(ClearInput);
            if (submitButton != null) submitButton.onClick.AddListener(SubmitPasscode);
            if (closeButton != null) closeButton.onClick.AddListener(CloseKeypad);
        }

        private void Update()
        {
            if (!IsOpen) return;

            // Đọc phím số bàn phím (Alpha0..Alpha9 hoặc Keypad0..Keypad9)
            for (int i = 0; i <= 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
                {
                    AddDigit(i.ToString());
                }
            }

            // Phím xóa
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                Backspace();
            }

            // Phím Enter xác nhận
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                SubmitPasscode();
            }

            // Phím Esc thoát
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseKeypad();
            }
        }

        public void OpenKeypad()
        {
            IsOpen = true;
            CurrentInput = string.Empty;
            EventBus.RaiseUIModalStateChanged(true);

            if (keypadPanel != null) keypadPanel.SetActive(true);
            if (statusMessageText != null) statusMessageText.text = "NHẬP MẬT MÃ 4 CHỮ SỐ:";
            UpdateDisplay();

            Debug.Log("[PasscodeKeypadUI] Keypad opened.");
        }

        public void CloseKeypad()
        {
            IsOpen = false;
            CurrentInput = string.Empty;
            if (keypadPanel != null) keypadPanel.SetActive(false);

            EventBus.RaiseUIModalStateChanged(false);
            Debug.Log("[PasscodeKeypadUI] Keypad closed.");
        }

        public void AddDigit(string digit)
        {
            if (CurrentInput.Length >= 4) return;

            CurrentInput += digit;
            UpdateDisplay();
        }

        public void Backspace()
        {
            if (CurrentInput.Length > 0)
            {
                CurrentInput = CurrentInput.Substring(0, CurrentInput.Length - 1);
                UpdateDisplay();
            }
        }

        public void ClearInput()
        {
            CurrentInput = string.Empty;
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (inputDisplayText != null)
            {
                // Hiển thị dạng [ _ _ _ _ ] hoặc [ 2 7 _ _ ]
                string display = "";
                for (int i = 0; i < 4; i++)
                {
                    if (i < CurrentInput.Length)
                        display += CurrentInput[i] + " ";
                    else
                        display += "_ ";
                }
                inputDisplayText.text = display.Trim();
            }
        }

        public void SubmitPasscode()
        {
            if (CurrentInput == GameConfig.Passcode) // "2728"
            {
                if (statusMessageText != null)
                {
                    statusMessageText.text = "MẬT MÃ CHÍNH XÁC! CỬA ĐÃ MỞ.";
                    statusMessageText.color = Color.green;
                }

                // Cập nhật trạng thái mở cửa vào RunState
                var run = GameManager.Instance != null ? GameManager.Instance.CurrentRun : null;
                if (run != null)
                {
                    run.secretAreaOpened = true;
                }

                EventBus.RaiseGameEventTriggered("E09");
                OnPasscodeSuccess?.Invoke();
                Debug.Log("[PasscodeKeypadUI] Passcode correct (2728). Secret area unlocked!");

                // Tự động đóng sau 1 giây
                Invoke(nameof(CloseKeypad), 0.8f);
            }
            else
            {
                if (statusMessageText != null)
                {
                    statusMessageText.text = "MẬT MÃ KHÔNG ĐÚNG! VUI LÒNG THỬ LẠI.";
                    statusMessageText.color = Color.red;
                }

                ClearInput();
                OnPasscodeFailed?.Invoke();
                Debug.LogWarning($"[PasscodeKeypadUI] Wrong passcode submitted: {CurrentInput}");
            }
        }
    }
}

// ============================================================================
// GameManager.cs — Singleton quản lý State Machine và vòng đời game
// MainMenu → Playing → Ending/GameOver → MainMenu
// ============================================================================

using UnityEngine;

namespace DeadlineRush.Core
{
    /// <summary>
    /// Singleton quản lý trạng thái game (State Machine).
    /// Cung cấp StartNewRun(), TriggerEnding(), ReturnToMainMenu().
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // ─── State ───────────────────────────────────────────────────────
        public GameState CurrentGameState { get; private set; } = GameState.MainMenu;
        public RunState CurrentRun { get; private set; }
        public MemoryData Memory { get; private set; }

        // ─── Unity Lifecycle ─────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Tải MemoryData từ đĩa
            Memory = SaveService.Load();
            CurrentRun = new RunState();
        }

        private void OnEnable()
        {
            EventBus.OnGameEnded += HandleGameEnded;
        }

        private void OnDisable()
        {
            EventBus.OnGameEnded -= HandleGameEnded;
        }

        // ─── Public API ──────────────────────────────────────────────────

        /// <summary>
        /// Bắt đầu một Run mới. Reset RunState, giữ nguyên MemoryData.
        /// </summary>
        public void StartNewRun()
        {
            CurrentRun.Reset();
            CurrentGameState = GameState.Playing;

            // Phát sự kiện ban đầu để UI/World đồng bộ
            EventBus.RaiseStatsChanged(CurrentRun.energy, CurrentRun.stress, CurrentRun.progress);
            EventBus.RaiseTimeChanged(CurrentRun.minute, Systems.TimeSystem.GetPhase(CurrentRun.minute));
            EventBus.RaiseAreaChanged(CurrentRun.currentArea);

            Debug.Log("[GameManager] New Run started. State reset to defaults.");
        }

        /// <summary>
        /// Kích hoạt kết thúc game với loại Ending cụ thể.
        /// </summary>
        public void TriggerEnding(EndingType endingType)
        {
            if (endingType == EndingType.GameOver)
            {
                CurrentGameState = GameState.GameOver;
            }
            else
            {
                CurrentGameState = GameState.Ending;
            }

            // Lưu MemoryData ngay lập tức
            SaveService.Save(Memory);

            EventBus.RaiseGameEnded(endingType);
            Debug.Log($"[GameManager] Game ended with: {endingType}");
        }

        /// <summary>
        /// Quay về Main Menu. Lưu MemoryData.
        /// </summary>
        public void ReturnToMainMenu()
        {
            SaveService.Save(Memory);
            CurrentGameState = GameState.MainMenu;
            Debug.Log("[GameManager] Returned to Main Menu.");
        }

        // ─── Event Handlers ──────────────────────────────────────────────
        private void HandleGameEnded(EndingType endingType)
        {
            if (endingType == EndingType.GameOver)
            {
                CurrentGameState = GameState.GameOver;
            }
            else
            {
                CurrentGameState = GameState.Ending;
            }

            // Lưu MemoryData
            SaveService.Save(Memory);
            Debug.Log($"[GameManager] Handled ending event: {endingType}");
        }
    }
}

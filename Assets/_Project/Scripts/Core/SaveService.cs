// ============================================================================
// SaveService.cs — Ghi/đọc MemoryData ra file JSON
// Tuyệt đối không lưu RunState ra đĩa.
// ============================================================================

using System;
using System.IO;
using UnityEngine;

namespace DeadlineRush.Core
{
    /// <summary>
    /// Quản lý lưu/tải MemoryData (dữ liệu vĩnh viễn) bằng JsonUtility.
    /// File lưu tại Application.persistentDataPath/memory_save.json
    /// </summary>
    public static class SaveService
    {
        private const string FileName = "memory_save.json";

        private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>
        /// Lưu MemoryData ra file JSON.
        /// </summary>
        public static void Save(MemoryData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(FilePath, json);
                Debug.Log($"[SaveService] Saved MemoryData to {FilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] Failed to save: {e.Message}");
            }
        }

        /// <summary>
        /// Tải MemoryData từ file JSON. Nếu không tồn tại, trả về instance mới.
        /// </summary>
        public static MemoryData Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);
                    MemoryData data = JsonUtility.FromJson<MemoryData>(json);
                    Debug.Log($"[SaveService] Loaded MemoryData from {FilePath}");
                    return data;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] Failed to load: {e.Message}");
            }

            Debug.Log("[SaveService] No save file found, creating new MemoryData.");
            return new MemoryData();
        }

        /// <summary>
        /// Xóa file save. Chỉ dùng khi cần reset toàn bộ dữ liệu vĩnh viễn.
        /// </summary>
        public static void DeleteSave()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    File.Delete(FilePath);
                    Debug.Log("[SaveService] Save file deleted.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] Failed to delete save: {e.Message}");
            }
        }
    }
}

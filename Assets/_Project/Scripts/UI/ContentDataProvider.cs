// ============================================================================
// ContentDataProvider.cs — Nạp và cung cấp dữ liệu tường thuật (JSON & CSV)
// Phục vụ M12, M7, M10: Clues, Attempts, Endings, Dialogues
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using DeadlineRush.Core;

namespace DeadlineRush.UI
{
    /// <summary>
    /// Service nạp và tra cứu toàn bộ dữ liệu tường thuật (Data-Driven 100%).
    /// </summary>
    public static class ContentDataProvider
    {
        private static bool _isLoaded = false;
        private static readonly Dictionary<string, ClueData> _cluesById = new Dictionary<string, ClueData>();
        private static readonly Dictionary<int, AttemptData> _attemptsByNumber = new Dictionary<int, AttemptData>();
        private static readonly Dictionary<string, EndingData> _endingsByType = new Dictionary<string, EndingData>();
        private static readonly List<DialogueRow> _dialogues = new List<DialogueRow>();

        /// <summary>
        /// Đường dẫn thư mục Data cơ sở.
        /// </summary>
        public static string BaseDataPath
        {
            get
            {
                // Thử đường dẫn trong Assets/_Project/Data
                string p = Path.Combine(Application.dataPath, "_Project", "Data");
                if (Directory.Exists(p)) return p;

                // Thử đường dẫn từ thư mục chạy hiện tại
                p = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "_Project", "Data");
                if (Directory.Exists(p)) return p;

                return Path.Combine(Application.dataPath, "_Project", "Data");
            }
        }

        /// <summary>
        /// Đảm bảo dữ liệu đã được nạp ít nhất một lần.
        /// </summary>
        public static void EnsureLoaded(bool forceReload = false)
        {
            if (_isLoaded && !forceReload) return;

            LoadClues();
            LoadAttempts();
            LoadEndings();
            LoadDialogues();

            _isLoaded = true;
        }

        // ─── Clues ────────────────────────────────────────────────────────
        private static void LoadClues()
        {
            _cluesById.Clear();
            string path = Path.Combine(BaseDataPath, "Clues", "clues.json");
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[ContentDataProvider] Clues file not found at: {path}");
                return;
            }

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                ClueCollection col = JsonUtility.FromJson<ClueCollection>(json);
                if (col != null && col.clues != null)
                {
                    foreach (var c in col.clues)
                    {
                        if (!string.IsNullOrEmpty(c.id))
                        {
                            _cluesById[c.id] = c;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ContentDataProvider] Error parsing clues.json: {ex.Message}");
            }
        }

        public static IReadOnlyCollection<ClueData> GetAllClues()
        {
            EnsureLoaded();
            return _cluesById.Values;
        }

        public static ClueData GetClue(string id)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(id)) return null;
            _cluesById.TryGetValue(id, out var data);
            return data;
        }

        // ─── Attempts ─────────────────────────────────────────────────────
        private static void LoadAttempts()
        {
            _attemptsByNumber.Clear();
            string path = Path.Combine(BaseDataPath, "Attempts", "attempts.json");
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[ContentDataProvider] Attempts file not found at: {path}");
                return;
            }

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                AttemptCollection col = JsonUtility.FromJson<AttemptCollection>(json);
                if (col != null && col.attempts != null)
                {
                    foreach (var a in col.attempts)
                    {
                        _attemptsByNumber[a.attemptNumber] = a;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ContentDataProvider] Error parsing attempts.json: {ex.Message}");
            }
        }

        public static IReadOnlyCollection<AttemptData> GetAllAttempts()
        {
            EnsureLoaded();
            return _attemptsByNumber.Values;
        }

        public static AttemptData GetAttempt(int number)
        {
            EnsureLoaded();
            _attemptsByNumber.TryGetValue(number, out var data);
            return data;
        }

        // ─── Endings ──────────────────────────────────────────────────────
        private static void LoadEndings()
        {
            _endingsByType.Clear();
            string path = Path.Combine(BaseDataPath, "Endings", "endings.json");
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[ContentDataProvider] Endings file not found at: {path}");
                return;
            }

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                EndingCollection col = JsonUtility.FromJson<EndingCollection>(json);
                if (col != null && col.endings != null)
                {
                    foreach (var e in col.endings)
                    {
                        if (!string.IsNullOrEmpty(e.endingType))
                        {
                            _endingsByType[e.endingType] = e;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ContentDataProvider] Error parsing endings.json: {ex.Message}");
            }
        }

        public static EndingData GetEnding(EndingType type)
        {
            return GetEnding(type.ToString());
        }

        public static EndingData GetEnding(string endingType)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(endingType)) return null;
            _endingsByType.TryGetValue(endingType, out var data);
            return data;
        }

        // ─── Dialogues (CSV) ──────────────────────────────────────────────
        private static void LoadDialogues()
        {
            _dialogues.Clear();
            string path = Path.Combine(BaseDataPath, "Dialogue", "dialogues.csv");
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[ContentDataProvider] dialogues.csv not found at: {path}");
                return;
            }

            try
            {
                string csv = File.ReadAllText(path, Encoding.UTF8);
                var rows = ParseCsv(csv);

                // Dòng đầu tiên là header, duyệt từ index 1
                for (int i = 1; i < rows.Count; i++)
                {
                    var cols = rows[i];
                    if (cols.Count < 9) continue;

                    string id = cols[0].Trim();
                    string groupId = cols[1].Trim();
                    string speaker = cols[2].Trim();
                    string text = cols[3].Trim();
                    int.TryParse(cols[4].Trim(), out int minMinute);
                    int.TryParse(cols[5].Trim(), out int maxMinute);
                    int.TryParse(cols[6].Trim(), out int minStress);
                    string reqFlag = cols[7].Trim();
                    string choicesJson = cols[8].Trim();

                    var row = new DialogueRow
                    {
                        id = id,
                        groupId = groupId,
                        speaker = speaker,
                        text = text,
                        minMinute = minMinute,
                        maxMinute = maxMinute,
                        minStress = minStress,
                        requiredFlag = reqFlag,
                        choices = ParseChoices(choicesJson)
                    };

                    _dialogues.Add(row);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ContentDataProvider] Error parsing dialogues.csv: {ex.Message}");
            }
        }

        private static List<DialogueChoice> ParseChoices(string choicesJson)
        {
            var list = new List<DialogueChoice>();
            if (string.IsNullOrEmpty(choicesJson) || choicesJson == "\"\"") return list;

            try
            {
                // Bọc mảng vào object wrapper để Unity JsonUtility parse được
                string wrapped = "{\"choices\":" + choicesJson + "}";
                var wrapper = JsonUtility.FromJson<DialogueChoiceListWrapper>(wrapped);
                if (wrapper != null && wrapper.choices != null)
                {
                    list.AddRange(wrapper.choices);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ContentDataProvider] Could not parse choices JSON '{choicesJson}': {ex.Message}");
            }

            return list;
        }

        /// <summary>
        /// Bộ phân tích cú pháp CSV hỗ trợ trường có nháy kép và dấu phẩy.
        /// </summary>
        public static List<List<string>> ParseCsv(string content)
        {
            var result = new List<List<string>>();
            if (string.IsNullOrEmpty(content)) return result;

            var currentRow = new List<string>();
            var currentField = new StringBuilder();
            bool insideQuotes = false;

            for (int i = 0; i < content.Length; i++)
            {
                char c = content[i];

                if (insideQuotes)
                {
                    if (c == '"')
                    {
                        // Kiểm tra nháy kép escape ("")
                        if (i + 1 < content.Length && content[i + 1] == '"')
                        {
                            currentField.Append('"');
                            i++;
                        }
                        else
                        {
                            insideQuotes = false;
                        }
                    }
                    else
                    {
                        currentField.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        insideQuotes = true;
                    }
                    else if (c == ',')
                    {
                        currentRow.Add(currentField.ToString());
                        currentField.Clear();
                    }
                    else if (c == '\r')
                    {
                        // Bỏ qua \r, xử lý ở \n
                    }
                    else if (c == '\n')
                    {
                        currentRow.Add(currentField.ToString());
                        currentField.Clear();
                        if (currentRow.Any(f => !string.IsNullOrWhiteSpace(f)))
                        {
                            result.Add(currentRow);
                        }
                        currentRow = new List<string>();
                    }
                    else
                    {
                        currentField.Append(c);
                    }
                }
            }

            if (currentField.Length > 0 || currentRow.Count > 0)
            {
                currentRow.Add(currentField.ToString());
                if (currentRow.Any(f => !string.IsNullOrWhiteSpace(f)))
                {
                    result.Add(currentRow);
                }
            }

            return result;
        }

        public static IReadOnlyList<DialogueRow> GetAllDialogues()
        {
            EnsureLoaded();
            return _dialogues;
        }

        /// <summary>
        /// Tìm câu thoại phù hợp nhất theo groupId và trạng thái game hiện tại.
        /// </summary>
        public static DialogueRow FindBestDialogue(string groupId, int minute, int stress, HashSet<string> flags)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(groupId)) return null;

            flags = flags ?? new HashSet<string>();

            // Lọc các thoại khớp groupId và điều kiện
            var matched = _dialogues.Where(d =>
                d.groupId.Equals(groupId, StringComparison.OrdinalIgnoreCase) &&
                minute >= d.minMinute &&
                minute <= d.maxMinute &&
                stress >= d.minStress &&
                (string.IsNullOrEmpty(d.requiredFlag) || flags.Contains(d.requiredFlag))
            ).ToList();

            if (matched.Count == 0)
            {
                // Fallback: chỉ lọc theo groupId nếu không tìm thấy khớp điều kiện nghiêm ngặt
                return _dialogues.FirstOrDefault(d => d.groupId.Equals(groupId, StringComparison.OrdinalIgnoreCase));
            }

            // Ưu tiên thoại có điều kiện cụ thể hơn (yêu cầu flag, minStress cao hơn, minMinute cao hơn)
            return matched
                .OrderByDescending(d => !string.IsNullOrEmpty(d.requiredFlag))
                .ThenByDescending(d => d.minStress)
                .ThenByDescending(d => d.minMinute)
                .FirstOrDefault();
        }
    }
}

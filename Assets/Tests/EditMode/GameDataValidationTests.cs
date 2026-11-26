using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DeadlineRush.Core;
using DeadlineRush.Systems;
using DeadlineRush.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeadlineRush.Tests.Validation
{
    /// <summary>
    /// Bộ EditMode test quét toàn bộ ScriptableObject và file dữ liệu (JSON/CSV) trong dự án
    /// để phát hiện sớm trường null/thiếu tham chiếu, trùng ID và chỉ số âm không hợp lệ.
    /// </summary>
    [TestFixture]
    public class GameDataValidationTests
    {
        private static readonly string[] IgnoredAssetPrefixes =
        {
            "Assets/TutorialInfo/",
            "Assets/Settings/"
        };

        // ─── 1. ScriptableObjects ──────────────────────────────────────────

        [Test]
        public void ScriptableObjects_AllInProject_HaveNoMissingReferencesDuplicateIdsOrInvalidNegativeStats()
        {
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets" });
            var errors = new List<string>();
            var actionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var eventIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (IsIgnoredAssetPath(path))
                {
                    continue;
                }

                ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null)
                {
                    errors.Add($"[MissingScript] ScriptableObject tại '{path}' bị null (có thể thiếu script).");
                    continue;
                }

                CheckMissingSerializedReferences(asset, path, errors);

                if (asset is ActionDefinition actionDef)
                {
                    ValidateActionDefinition(actionDef, path, actionIds, errors);
                }
                else if (asset is GameEventDefinition eventDef)
                {
                    ValidateGameEventDefinition(eventDef, path, eventIds, errors);
                }
            }

            Assert.IsEmpty(errors, "Phát hiện lỗi dữ liệu ScriptableObject:\n- " + string.Join("\n- ", errors));
        }

        private static void CheckMissingSerializedReferences(ScriptableObject asset, string path, List<string> errors)
        {
            var serializedObject = new SerializedObject(asset);
            SerializedProperty prop = serializedObject.GetIterator();

            while (prop.NextVisible(true))
            {
                // Unity 6000.6.8f1: sử dụng objectReferenceEntityIdValue != EntityId.None
                // thay cho objectReferenceInstanceIDValue đã obsolete (CS0619)
                if (prop.propertyType == SerializedPropertyType.ObjectReference
                    && prop.objectReferenceValue == null
                    && prop.objectReferenceEntityIdValue != EntityId.None)
                {
                    errors.Add($"[MissingReference] '{path}' có tham chiếu bị hỏng (Missing) tại field '{prop.propertyPath}'.");
                }
            }
        }

        private static void ValidateActionDefinition(
            ActionDefinition action,
            string path,
            HashSet<string> seenIds,
            List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(action.actionId))
            {
                errors.Add($"[NullOrEmpty] ActionDefinition tại '{path}' bị trống 'actionId'.");
            }
            else if (!seenIds.Add(action.actionId.Trim()))
            {
                errors.Add($"[DuplicateID] ActionDefinition tại '{path}' trùng actionId '{action.actionId}'.");
            }

            if (string.IsNullOrWhiteSpace(action.displayName))
            {
                errors.Add($"[NullOrEmpty] ActionDefinition tại '{path}' bị trống 'displayName'.");
            }

            // Số phút tốn khi thực hiện không được âm (dEnergy/dStress có thể âm theo thiết kế ActionDefinition)
            if (action.minutes < 0)
            {
                errors.Add($"[NegativeStat] ActionDefinition '{action.actionId}' ({path}) có minutes < 0 ({action.minutes}).");
            }

            if (action.dProgress < 0)
            {
                errors.Add($"[NegativeStat] ActionDefinition '{action.actionId}' ({path}) có dProgress < 0 ({action.dProgress}).");
            }

            ValidateConditions(action.conditions, path, errors);
        }

        private static void ValidateGameEventDefinition(
            GameEventDefinition gameEvent,
            string path,
            HashSet<string> seenIds,
            List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(gameEvent.eventId))
            {
                errors.Add($"[NullOrEmpty] GameEventDefinition tại '{path}' bị trống 'eventId'.");
            }
            else if (!seenIds.Add(gameEvent.eventId.Trim()))
            {
                errors.Add($"[DuplicateID] GameEventDefinition tại '{path}' trùng eventId '{gameEvent.eventId}'.");
            }

            if (string.IsNullOrWhiteSpace(gameEvent.eventName))
            {
                errors.Add($"[NullOrEmpty] GameEventDefinition tại '{path}' bị trống 'eventName'.");
            }

            if (gameEvent.priority < 0)
            {
                errors.Add($"[NegativeStat] GameEventDefinition '{gameEvent.eventId}' ({path}) có priority < 0 ({gameEvent.priority}).");
            }

            ValidateConditions(gameEvent.conditions, path, errors);
        }

        private static void ValidateConditions(Condition[] conditions, string path, List<string> errors)
        {
            if (conditions == null)
            {
                return;
            }

            for (int i = 0; i < conditions.Length; i++)
            {
                Condition cond = conditions[i];
                if (cond.intValue < 0 || cond.intValueMax < 0)
                {
                    errors.Add($"[NegativeStat] '{path}' conditions[{i}] có giá trị âm (intValue={cond.intValue}, intValueMax={cond.intValueMax}).");
                }

                if ((cond.type == ConditionType.HasClue || cond.type == ConditionType.HasFlag)
                    && string.IsNullOrWhiteSpace(cond.stringValue))
                {
                    errors.Add($"[NullOrEmpty] '{path}' conditions[{i}] ({cond.type}) bị trống stringValue.");
                }
            }
        }

        private static bool IsIgnoredAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return true;
            }

            foreach (string prefix in IgnoredAssetPrefixes)
            {
                if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        // ─── 2. JSON & CSV Data Files ──────────────────────────────────────

        [Test]
        public void DataFiles_AllJsonAndCsvUnderProjectData_ExistAndAreReadable()
        {
            string dataRoot = Path.Combine(Application.dataPath, "_Project", "Data");
            Assert.IsTrue(Directory.Exists(dataRoot), $"Không tìm thấy thư mục dữ liệu: {dataRoot}");

            string[] jsonFiles = Directory.GetFiles(dataRoot, "*.json", SearchOption.AllDirectories);
            string[] csvFiles = Directory.GetFiles(dataRoot, "*.csv", SearchOption.AllDirectories);

            Assert.IsNotEmpty(jsonFiles, "Không có file .json nào trong Assets/_Project/Data.");
            Assert.IsNotEmpty(csvFiles, "Không có file .csv nào trong Assets/_Project/Data.");

            foreach (string file in jsonFiles)
            {
                string content = File.ReadAllText(file, Encoding.UTF8);
                Assert.IsFalse(string.IsNullOrWhiteSpace(content), $"File JSON bị rỗng: {file}");
            }

            foreach (string file in csvFiles)
            {
                string content = File.ReadAllText(file, Encoding.UTF8);
                Assert.IsFalse(string.IsNullOrWhiteSpace(content), $"File CSV bị rỗng: {file}");
            }
        }

        [Test]
        public void CluesJson_AllEntries_HaveValidFieldsAndUniqueIds()
        {
            string path = Path.Combine(Application.dataPath, "_Project", "Data", "Clues", "clues.json");
            Assert.IsTrue(File.Exists(path), $"Thiếu file dữ liệu: {path}");

            string json = File.ReadAllText(path, Encoding.UTF8);
            ClueCollection collection = JsonUtility.FromJson<ClueCollection>(json);
            Assert.IsNotNull(collection, "Không thể parse clues.json.");
            Assert.IsNotNull(collection.clues, "Danh sách clues trong clues.json bị null.");
            Assert.IsNotEmpty(collection.clues, "clues.json không có phần tử nào.");

            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var errors = new List<string>();

            for (int i = 0; i < collection.clues.Count; i++)
            {
                ClueData clue = collection.clues[i];
                if (clue == null)
                {
                    errors.Add($"clues[{i}] bị null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(clue.id))
                {
                    errors.Add($"clues[{i}] thiếu 'id'.");
                }
                else if (!seenIds.Add(clue.id.Trim()))
                {
                    errors.Add($"clues[{i}] trùng id '{clue.id}'.");
                }

                if (string.IsNullOrWhiteSpace(clue.title))
                {
                    errors.Add($"clues[{i}] ('{clue.id}') thiếu 'title'.");
                }

                if (string.IsNullOrWhiteSpace(clue.description))
                {
                    errors.Add($"clues[{i}] ('{clue.id}') thiếu 'description'.");
                }

                if (string.IsNullOrWhiteSpace(clue.foundHint))
                {
                    errors.Add($"clues[{i}] ('{clue.id}') thiếu 'foundHint'.");
                }
            }

            Assert.IsEmpty(errors, "Lỗi dữ liệu clues.json:\n- " + string.Join("\n- ", errors));
        }

        [Test]
        public void AttemptsJson_AllEntries_HavePositiveUniqueNumbersAndValidFields()
        {
            string path = Path.Combine(Application.dataPath, "_Project", "Data", "Attempts", "attempts.json");
            Assert.IsTrue(File.Exists(path), $"Thiếu file dữ liệu: {path}");

            string json = File.ReadAllText(path, Encoding.UTF8);
            AttemptCollection collection = JsonUtility.FromJson<AttemptCollection>(json);
            Assert.IsNotNull(collection, "Không thể parse attempts.json.");
            Assert.IsNotNull(collection.attempts, "Danh sách attempts trong attempts.json bị null.");
            Assert.IsNotEmpty(collection.attempts, "attempts.json không có phần tử nào.");

            var seenNumbers = new HashSet<int>();
            var errors = new List<string>();

            for (int i = 0; i < collection.attempts.Count; i++)
            {
                AttemptData attempt = collection.attempts[i];
                if (attempt == null)
                {
                    errors.Add($"attempts[{i}] bị null.");
                    continue;
                }

                if (attempt.attemptNumber <= 0)
                {
                    errors.Add($"attempts[{i}] có attemptNumber <= 0 ({attempt.attemptNumber}).");
                }
                else if (!seenNumbers.Add(attempt.attemptNumber))
                {
                    errors.Add($"attempts[{i}] trùng attemptNumber #{attempt.attemptNumber}.");
                }

                if (string.IsNullOrWhiteSpace(attempt.title))
                {
                    errors.Add($"attempts[{i}] (#{attempt.attemptNumber}) thiếu 'title'.");
                }

                if (string.IsNullOrWhiteSpace(attempt.timestamp))
                {
                    errors.Add($"attempts[{i}] (#{attempt.attemptNumber}) thiếu 'timestamp'.");
                }

                if (string.IsNullOrWhiteSpace(attempt.content))
                {
                    errors.Add($"attempts[{i}] (#{attempt.attemptNumber}) thiếu 'content'.");
                }
            }

            Assert.IsEmpty(errors, "Lỗi dữ liệu attempts.json:\n- " + string.Join("\n- ", errors));
        }

        [Test]
        public void EndingsJson_AllEntries_HaveValidUniqueTypesAndRequiredFields()
        {
            string path = Path.Combine(Application.dataPath, "_Project", "Data", "Endings", "endings.json");
            Assert.IsTrue(File.Exists(path), $"Thiếu file dữ liệu: {path}");

            string json = File.ReadAllText(path, Encoding.UTF8);
            EndingCollection collection = JsonUtility.FromJson<EndingCollection>(json);
            Assert.IsNotNull(collection, "Không thể parse endings.json.");
            Assert.IsNotNull(collection.endings, "Danh sách endings trong endings.json bị null.");
            Assert.IsNotEmpty(collection.endings, "endings.json không có phần tử nào.");

            var seenTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var errors = new List<string>();

            for (int i = 0; i < collection.endings.Count; i++)
            {
                EndingData ending = collection.endings[i];
                if (ending == null)
                {
                    errors.Add($"endings[{i}] bị null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(ending.endingType))
                {
                    errors.Add($"endings[{i}] thiếu 'endingType'.");
                }
                else
                {
                    if (!seenTypes.Add(ending.endingType.Trim()))
                    {
                        errors.Add($"endings[{i}] trùng endingType '{ending.endingType}'.");
                    }

                    if (!Enum.TryParse(ending.endingType, false, out EndingType _))
                    {
                        errors.Add($"endings[{i}] endingType '{ending.endingType}' không khớp enum EndingType.");
                    }
                }

                if (string.IsNullOrWhiteSpace(ending.title))
                {
                    errors.Add($"endings[{i}] ('{ending.endingType}') thiếu 'title'.");
                }

                if (string.IsNullOrWhiteSpace(ending.subtitle))
                {
                    errors.Add($"endings[{i}] ('{ending.endingType}') thiếu 'subtitle'.");
                }

                if (string.IsNullOrWhiteSpace(ending.description))
                {
                    errors.Add($"endings[{i}] ('{ending.endingType}') thiếu 'description'.");
                }
            }

            Assert.IsEmpty(errors, "Lỗi dữ liệu endings.json:\n- " + string.Join("\n- ", errors));
        }

        [Test]
        public void DialoguesCsv_AllRows_HaveUniqueIdsNonNegativeStatsAndValidChoices()
        {
            string path = Path.Combine(Application.dataPath, "_Project", "Data", "Dialogue", "dialogues.csv");
            Assert.IsTrue(File.Exists(path), $"Thiếu file dữ liệu: {path}");

            string csv = File.ReadAllText(path, Encoding.UTF8);
            List<List<string>> rows = ContentDataProvider.ParseCsv(csv);
            Assert.Greater(rows.Count, 1, "dialogues.csv phải có header và ít nhất 1 dòng dữ liệu.");

            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var errors = new List<string>();

            for (int i = 1; i < rows.Count; i++)
            {
                List<string> cols = rows[i];
                if (cols.Count < 9)
                {
                    errors.Add($"Dòng {i + 1} chỉ có {cols.Count} cột (cần >= 9).");
                    continue;
                }

                string id = cols[0].Trim();
                string groupId = cols[1].Trim();
                string speaker = cols[2].Trim();
                string text = cols[3].Trim();
                string minMinuteRaw = cols[4].Trim();
                string maxMinuteRaw = cols[5].Trim();
                string minStressRaw = cols[6].Trim();
                string choicesJson = cols[8].Trim();

                if (string.IsNullOrWhiteSpace(id))
                {
                    errors.Add($"Dòng {i + 1} bị trống 'id'.");
                }
                else if (!seenIds.Add(id))
                {
                    errors.Add($"Dòng {i + 1} trùng id '{id}'.");
                }

                if (string.IsNullOrWhiteSpace(groupId))
                {
                    errors.Add($"Dòng {i + 1} ('{id}') bị trống 'groupId'.");
                }

                if (string.IsNullOrWhiteSpace(speaker))
                {
                    errors.Add($"Dòng {i + 1} ('{id}') bị trống 'speaker'.");
                }

                if (string.IsNullOrWhiteSpace(text))
                {
                    errors.Add($"Dòng {i + 1} ('{id}') bị trống 'text'.");
                }

                if (!int.TryParse(minMinuteRaw, out int minMinute) || minMinute < 0)
                {
                    errors.Add($"Dòng {i + 1} ('{id}') có minMinute không hợp lệ hoặc âm: '{minMinuteRaw}'.");
                }

                if (!int.TryParse(maxMinuteRaw, out int maxMinute) || maxMinute < 0)
                {
                    errors.Add($"Dòng {i + 1} ('{id}') có maxMinute không hợp lệ hoặc âm: '{maxMinuteRaw}'.");
                }
                else if (maxMinute < minMinute)
                {
                    errors.Add($"Dòng {i + 1} ('{id}') có maxMinute ({maxMinute}) < minMinute ({minMinute}).");
                }

                if (!int.TryParse(minStressRaw, out int minStress) || minStress < 0)
                {
                    errors.Add($"Dòng {i + 1} ('{id}') có minStress không hợp lệ hoặc âm: '{minStressRaw}'.");
                }

                if (!string.IsNullOrEmpty(choicesJson) && choicesJson != "\"\"")
                {
                    string wrapped = "{\"choices\":" + choicesJson + "}";
                    DialogueChoiceListWrapper wrapper = JsonUtility.FromJson<DialogueChoiceListWrapper>(wrapped);
                    if (wrapper == null || wrapper.choices == null || wrapper.choices.Count == 0)
                    {
                        errors.Add($"Dòng {i + 1} ('{id}') có choicesJson không parse được: '{choicesJson}'.");
                    }
                    else
                    {
                        for (int c = 0; c < wrapper.choices.Count; c++)
                        {
                            if (string.IsNullOrWhiteSpace(wrapper.choices[c].text))
                            {
                                errors.Add($"Dòng {i + 1} ('{id}') choice[{c}] bị trống 'text'.");
                            }
                        }
                    }
                }
            }

            Assert.IsEmpty(errors, "Lỗi dữ liệu dialogues.csv:\n- " + string.Join("\n- ", errors));
        }
    }
}

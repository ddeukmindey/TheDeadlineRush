// ============================================================================
// NarrativeDataModels.cs — Định nghĩa cấu trúc dữ liệu tường thuật cho M12
// Clues, ATTEMPTs, Dialogues, Endings
// ============================================================================

using System;
using System.Collections.Generic;

namespace DeadlineRush.UI
{
    [Serializable]
    public class ClueData
    {
        public string id;
        public string title;
        public bool isCore;
        public string description;
        public string foundHint;
    }

    [Serializable]
    public class ClueCollection
    {
        public List<ClueData> clues = new List<ClueData>();
    }

    [Serializable]
    public class AttemptData
    {
        public int attemptNumber;
        public string title;
        public string timestamp;
        public string content;
        public bool isKeyAttempt;
    }

    [Serializable]
    public class AttemptCollection
    {
        public List<AttemptData> attempts = new List<AttemptData>();
    }

    [Serializable]
    public class EndingData
    {
        public string endingType;
        public string title;
        public string subtitle;
        public string description;
        public string details;
    }

    [Serializable]
    public class EndingCollection
    {
        public List<EndingData> endings = new List<EndingData>();
    }

    [Serializable]
    public class DialogueChoice
    {
        public string text;
        public string flag;
    }

    [Serializable]
    public class DialogueChoiceListWrapper
    {
        public List<DialogueChoice> choices = new List<DialogueChoice>();
    }

    [Serializable]
    public class DialogueRow
    {
        public string id;
        public string groupId;
        public string speaker;
        public string text;
        public int minMinute;
        public int maxMinute;
        public int minStress;
        public string requiredFlag;
        public List<DialogueChoice> choices = new List<DialogueChoice>();
    }
}

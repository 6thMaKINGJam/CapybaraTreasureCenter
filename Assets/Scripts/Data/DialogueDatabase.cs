using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueDatabase", menuName = "Game/Dialogue Database")]
public class DialogueDatabase : ScriptableObject
{
    [System.Serializable]
    public class DialogueEntry
    {
        public DialogueType Type;
        public List<string> Dialogues = new List<string>();
        public bool IsPersistent = false;
        public bool IsLoop = false;
        public float LoopInterval = 3f;
    }
    
    public List<DialogueEntry> Entries = new List<DialogueEntry>();
    
    /// <summary>
    /// 특정 타입의 DialogueEntry 찾기
    /// </summary>
    public DialogueEntry GetEntry(DialogueType type)
    {
        return Entries.Find(e => e.Type == type);
    }
    
    /// <summary>
    /// 특정 타입의 랜덤 대사 가져오기
    /// </summary>
    public string GetRandomDialogue(DialogueType type)
    {
        var entry = GetEntry(type);
        if (entry == null || entry.Dialogues.Count == 0)
            return "";
        
        return entry.Dialogues[Random.Range(0, entry.Dialogues.Count)];
    }
}
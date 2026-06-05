using UnityEngine;

[System.Serializable]
public class DialogueData
{
    public string DialogueKey;
    public string AniID;
}

/// <summary>
/// 대화
/// </summary>
[CreateAssetMenu(fileName = "DialogueConfig", menuName = "Dialogue/DialogueConfig")]
public class DialogueConfig : ScriptableObject
{
    [SerializeField] DialogueData[] _dialogues;

    public DialogueData GetDialogue(string key)
    {
        foreach (DialogueData dialogue in _dialogues)
        {
            if (dialogue.DialogueKey == key) return dialogue;
        }
        return null;
    }
}
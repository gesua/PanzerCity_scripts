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

    /// <summary>
    /// 대화 반환
    /// </summary>
    public DialogueData GetDialogue(string key)
    {
        foreach (DialogueData dialogue in _dialogues)
        {
            if (dialogue.DialogueKey == key) return dialogue;
        }
        return null;
    }

    /// <summary>
    /// 몇 개 있는지 검색
    /// </summary>
    public int GetDialogueCount(string prefix)
    {
        int count = 0;

        foreach (DialogueData dialogue in _dialogues)
        {
            if (dialogue.DialogueKey.StartsWith(prefix))
            {
                count++;
            }
        }

        return count;
    }
}
using UnityEngine;
using UnityEditor;

public class MissingScriptFinder
{
    [MenuItem("Tools/Find Missing Scripts")]
    static void FindMissingScripts()
    {
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject go in allObjects)
        {
            Component[] components = go.GetComponents<Component>();
            foreach (Component comp in components)
            {
                if (comp == null)
                {
                    Debug.LogWarning("Missing Script found in: " + go.name, go);
                }
            }
        }
    }
}
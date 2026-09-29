using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class FontAssetReplacer : EditorWindow
{
    private const string OldFontName = "RussoOne-Regular SDF";
    private const string NewFontName = "RussoOne-Regular New SDF";

    [MenuItem("Tools/Font/Replace RussoOne Font")]
    private static void ReplaceFont()
    {
        TMP_FontAsset oldFont = FindFont(OldFontName);
        TMP_FontAsset newFont = FindFont(NewFontName);

        if (oldFont == null || newFont == null)
        {
            Debug.LogError("기존 폰트 또는 새 폰트를 찾을 수 없습니다.");
            return;
        }

        if (!EditorUtility.DisplayDialog(
            "폰트 일괄 교체",
            $"기존 폰트를 새 폰트로 교체합니다.\n\n" +
            $"기존: {OldFontName}\n" +
            $"변경: {NewFontName}\n\n" +
            "씬과 프리팹을 수정합니다. 진행하시겠습니까?",
            "교체",
            "취소"))
        {
            return;
        }

        int prefabCount = ReplaceInPrefabs(oldFont, newFont);
        int sceneCount = ReplaceInScenes(oldFont, newFont);

        AssetDatabase.SaveAssets();

        Debug.Log(
            $"폰트 교체 완료\n" +
            $"프리팹: {prefabCount}개\n" +
            $"씬: {sceneCount}개");
    }

    private static TMP_FontAsset FindFont(string fontName)
    {
        string[] guids = AssetDatabase.FindAssets(
            $"{fontName} t:TMP_FontAsset");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);

            if (font != null && font.name == fontName)
                return font;
        }

        return null;
    }

    private static int ReplaceInPrefabs(
        TMP_FontAsset oldFont,
        TMP_FontAsset newFont)
    {
        int count = 0;

        string[] guids = AssetDatabase.FindAssets("t:Prefab");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // 프로젝트 Assets 폴더에 있는 프리팹만 처리합니다.
            if (!path.StartsWith("Assets/"))
                continue;

            GameObject prefab = PrefabUtility.LoadPrefabContents(path);

            bool changed = ReplaceFontsInHierarchy(
                prefab, oldFont, newFont);

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
                count++;
            }

            PrefabUtility.UnloadPrefabContents(prefab);
        }

        return count;
    }

    private static int ReplaceInScenes(
        TMP_FontAsset oldFont,
        TMP_FontAsset newFont)
    {
        int count = 0;

        // 기존에 열려 있던 씬 구성을 저장합니다.
        var previousSetup = EditorSceneManager.GetSceneManagerSetup();

        string[] guids = AssetDatabase.FindAssets("t:Scene");

        try
        {
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                // 프로젝트 Assets 폴더에 있는 씬만 처리합니다.
                if (!path.StartsWith("Assets/"))
                    continue;

                Scene scene = EditorSceneManager.OpenScene(
                    path, OpenSceneMode.Single);

                bool changed = false;

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (ReplaceFontsInHierarchy(root, oldFont, newFont))
                        changed = true;
                }

                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    count++;
                }
            }
        }
        finally
        {
            // 작업 전 씬 구성을 복원합니다.
            EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
        }

        return count;
    }

    private static bool ReplaceFontsInHierarchy(
        GameObject root,
        TMP_FontAsset oldFont,
        TMP_FontAsset newFont)
    {
        bool changed = false;

        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);

        foreach (TMP_Text text in texts)
        {
            if (text.font == oldFont)
            {
                Undo.RecordObject(text, "Replace TMP Font");
                text.font = newFont;
                EditorUtility.SetDirty(text);
                changed = true;
            }
        }

        return changed;
    }
}
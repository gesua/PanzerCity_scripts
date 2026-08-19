using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Bitgem.VFX.StylisedWater;

/// <summary>
/// 에디터 상단 메뉴에 도구를 추가하여 물 타일의 겹치는 판넬을 자동으로 꺼주는 윈도우
/// </summary>
public class WaterOptimizerWindow : EditorWindow
{
    // 물 타일의 그리드 간격
    float _gridSize = 3.0f;

    // 좌표 오차 허용 범위
    float _tolerance = 0.1f;

    // 판넬 이름 키워드
    string _nameUp = "Up";
    string _nameDown = "Down";
    string _nameLeft = "Left";
    string _nameRight = "Right";

    [MenuItem("Tools/물 타일 겹침 최적화 (Optimize Water)")]
    public static void ShowWindow()
    {
        // 툴 윈도우 띄우기
        GetWindow<WaterOptimizerWindow>("물 타일 최적화 툴");
    }

    void OnGUI()
    {
        GUILayout.Label("물 타일 판넬 설정", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Water의 직계 자식 Marker를 물 타일로 인식합니다.\n" +
            "서로 인접한 Marker의 Up / Down / Left / Right 판넬을 자동으로 끕니다.\n" +
            "Bottom과 MiniMapIcon은 건드리지 않습니다.",
            MessageType.Info);

        _nameUp = EditorGUILayout.TextField("상 (Z+) 이름", _nameUp);
        _nameDown = EditorGUILayout.TextField("하 (Z-) 이름", _nameDown);
        _nameLeft = EditorGUILayout.TextField("좌 (X-) 이름", _nameLeft);
        _nameRight = EditorGUILayout.TextField("우 (X+) 이름", _nameRight);

        GUILayout.Space(10);

        _gridSize = EditorGUILayout.FloatField("그리드 간격", _gridSize);
        _tolerance = EditorGUILayout.FloatField("좌표 오차 허용 범위", _tolerance);

        GUILayout.Space(20);

        if (GUILayout.Button("전체 물 타일 겹침 판넬 끄기", GUILayout.Height(40)))
        {
            OptimizeAllWater();
        }

        if (GUILayout.Button("모든 물 판넬 다시 켜기 (초기화)", GUILayout.Height(30)))
        {
            ResetAllWater();
        }
    }

    void OptimizeAllWater()
    {
        // WaterVolumeTransforms가 붙어있는 Water를 모두 찾습니다.
        WaterVolumeTransforms[] waters =
            FindObjectsByType<WaterVolumeTransforms>(FindObjectsSortMode.None);

        if (waters.Length == 0)
        {
            Debug.LogWarning("씬에 WaterVolumeTransforms를 가진 Water 오브젝트가 없습니다.");
            return;
        }

        int disabledCount = 0;
        int detectedNeighborCount = 0;

        // 실행 전 모든 방향 판넬을 켭니다.
        ResetAllWater();

        foreach (WaterVolumeTransforms water in waters)
        {
            List<Transform> markers = GetMarkers(water.transform);

            for (int i = 0; i < markers.Count; i++)
            {
                Transform current = markers[i];

                // WaterVolumeTransforms와 동일하게 로컬 좌표를 사용합니다.
                Vector3 currentPos = current.localPosition;

                for (int j = 0; j < markers.Count; j++)
                {
                    if (i == j)
                        continue;

                    Transform other = markers[j];

                    Vector3 otherPos = other.localPosition;
                    Vector3 diff = otherPos - currentPos;

                    // X/Z 기준으로 한 칸 떨어진 타일인지 확인합니다.
                    float deltaX = Mathf.Abs(diff.x);
                    float deltaZ = Mathf.Abs(diff.z);

                    bool isHorizontalNeighbor =
                        Mathf.Abs(deltaX - _gridSize) < _tolerance &&
                        deltaZ < _tolerance;

                    bool isVerticalNeighbor =
                        Mathf.Abs(deltaZ - _gridSize) < _tolerance &&
                        deltaX < _tolerance;

                    if (isHorizontalNeighbor == false &&
                        isVerticalNeighbor == false)
                    {
                        continue;
                    }

                    detectedNeighborCount++;

                    if (Mathf.Abs(diff.x) < _tolerance &&
                        diff.z > (_gridSize - _tolerance))
                    {
                        // 다른 타일이 위(Z+)에 있음 -> 현재 타일의 Up 판넬 끄기
                        if (DisablePanel(current, _nameUp))
                            disabledCount++;
                    }
                    else if (Mathf.Abs(diff.x) < _tolerance &&
                             diff.z < -(_gridSize - _tolerance))
                    {
                        // 다른 타일이 아래(Z-)에 있음 -> 현재 타일의 Down 판넬 끄기
                        if (DisablePanel(current, _nameDown))
                            disabledCount++;
                    }
                    else if (Mathf.Abs(diff.z) < _tolerance &&
                             diff.x > (_gridSize - _tolerance))
                    {
                        // 다른 타일이 우(X+)에 있음 -> 현재 타일의 Right 판넬 끄기
                        if (DisablePanel(current, _nameRight))
                            disabledCount++;
                    }
                    else if (Mathf.Abs(diff.z) < _tolerance &&
                             diff.x < -(_gridSize - _tolerance))
                    {
                        // 다른 타일이 좌(X-)에 있음 -> 현재 타일의 Left 판넬 끄기
                        if (DisablePanel(current, _nameLeft))
                            disabledCount++;
                    }
                }
            }
        }

        Debug.Log(
            $"[물 타일 최적화 완료] " +
            $"이웃 감지 {detectedNeighborCount}회 / " +
            $"총 {disabledCount}개의 판넬을 비활성화했습니다.");
    }

    /// <summary>
    /// Water의 직계 자식 중 Marker만 가져옵니다.
    /// </summary>
    List<Transform> GetMarkers(Transform water)
    {
        List<Transform> markers = new List<Transform>();

        for (int i = 0; i < water.childCount; i++)
        {
            Transform child = water.GetChild(i);

            if (child.name.Contains("Marker"))
            {
                markers.Add(child);
            }
        }

        return markers;
    }

    /// <summary>
    /// 씬에 있는 모든 Water의 방향 판넬을 다시 켭니다.
    /// </summary>
    void ResetAllWater()
    {
        // 씬에 있는 모든 WaterVolumeTransforms를 찾습니다.
        WaterVolumeTransforms[] waters =
            FindObjectsByType<WaterVolumeTransforms>(FindObjectsSortMode.None);

        foreach (WaterVolumeTransforms water in waters)
        {
            List<Transform> markers = GetMarkers(water.transform);

            foreach (Transform marker in markers)
            {
                EnablePanel(marker, _nameUp);
                EnablePanel(marker, _nameDown);
                EnablePanel(marker, _nameLeft);
                EnablePanel(marker, _nameRight);
            }
        }
    }

    /// <summary>
    /// 지정된 이름의 판넬을 비활성화합니다.
    /// </summary>
    bool DisablePanel(Transform marker, string keyword)
    {
        Transform panel = FindChildByKeyword(marker, keyword);

        if (panel != null && panel.gameObject.activeSelf)
        {
            Undo.RecordObject(panel.gameObject, "Disable Water Panel");

            panel.gameObject.SetActive(false);

            EditorUtility.SetDirty(panel.gameObject);

            return true;
        }

        return false;
    }

    /// <summary>
    /// 지정된 이름의 판넬을 활성화합니다.
    /// </summary>
    void EnablePanel(Transform marker, string keyword)
    {
        Transform panel = FindChildByKeyword(marker, keyword);

        if (panel != null && panel.gameObject.activeSelf == false)
        {
            Undo.RecordObject(panel.gameObject, "Enable Water Panel");

            panel.gameObject.SetActive(true);

            EditorUtility.SetDirty(panel.gameObject);
        }
    }

    /// <summary>
    /// Marker 아래에서 지정된 키워드를 가진 자식 오브젝트를 찾습니다.
    /// </summary>
    Transform FindChildByKeyword(Transform parent, string keyword)
    {
        if (string.IsNullOrEmpty(keyword))
            return null;

        string lowerKeyword = keyword.ToLower();

        // 비활성화된 판넬도 찾을 수 있도록 true를 사용합니다.
        Transform[] children =
            parent.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child == parent)
                continue;

            if (child.name.ToLower().Contains(lowerKeyword))
            {
                return child;
            }
        }

        return null;
    }
}
using UnityEngine;
using UnityEditor;

/// <summary>
/// 에디터 상단 메뉴에 도구를 추가하여 풀숲의 겹치는 판넬을 자동으로 꺼주는 윈도우
/// </summary>
public class BushOptimizerWindow : EditorWindow
{
    // 사용자가 에디터에서 설정할 수 있는 판넬 이름 키워드
    string _nameUp = "Up";       // 상 (Z+)
    string _nameDown = "Down";   // 하 (Z-)
    string _nameLeft = "Left";   // 좌 (X-)
    string _nameRight = "Right"; // 우 (X+)

    float _gridSize = 3.0f;      // 블록 간격 (15 - 12 = 3)
    float _tolerance = 0.1f;     // 좌표 오차 허용 범위

    [MenuItem("Tools/풀숲 겹침 최적화 (Optimize Bushes)")]
    public static void ShowWindow()
    {
        // 툴 윈도우 띄우기
        GetWindow<BushOptimizerWindow>("풀숲 최적화 툴");
    }

    void OnGUI()
    {
        GUILayout.Label("판넬 게임오브젝트 이름 포함 단어 설정", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("자식 오브젝트 이름에 아래 단어가 포함되어 있으면 해당 방향의 판넬로 인식합니다. (대소문자 구분 없음)\n예: 'Panel_Up', '상' 등", MessageType.Info);

        _nameUp = EditorGUILayout.TextField("상 (Z+ 방향) 이름", _nameUp);
        _nameDown = EditorGUILayout.TextField("하 (Z- 방향) 이름", _nameDown);
        _nameLeft = EditorGUILayout.TextField("좌 (X- 방향) 이름", _nameLeft);
        _nameRight = EditorGUILayout.TextField("우 (X+ 방향) 이름", _nameRight);

        GUILayout.Space(10);
        _gridSize = EditorGUILayout.FloatField("그리드 간격 (블록 크기)", _gridSize);
        _tolerance = EditorGUILayout.FloatField("좌표 오차 허용 범위", _tolerance);

        GUILayout.Space(20);

        if (GUILayout.Button("전체 풀숲 겹침 판넬 끄기", GUILayout.Height(40)))
        {
            OptimizeAllBushes();
        }

        if (GUILayout.Button("모든 판넬 다시 켜기 (초기화)", GUILayout.Height(30)))
        {
            ResetAllBushes();
        }
    }

    void OptimizeAllBushes()
    {
        // 씬에 있는 모든 BushBlock을 찾습니다.
        BushBlock[] bushes = FindObjectsByType<BushBlock>(FindObjectsSortMode.None);
        if (bushes.Length == 0)
        {
            Debug.LogWarning("씬에 BushBlock 컴포넌트를 가진 오브젝트가 없습니다.");
            return;
        }

        int disabledCount = 0;
        int detectedNeighborCount = 0;

        // 실행 전 모든 판넬을 켜서 꼬임을 방지합니다.
        ResetAllBushes(bushes);

        for (int i = 0; i < bushes.Length; i++)
        {
            BushBlock current = bushes[i];
            Vector3 pos = current.transform.position;

            for (int j = 0; j < bushes.Length; j++)
            {
                if (i == j) continue; // 자기 자신은 건너뜀

                BushBlock other = bushes[j];
                Vector3 otherPos = other.transform.position;
                Vector3 diff = otherPos - pos;

                Debug.Log($"{current.name}과 {other.name} 비교");
                Debug.Log($"{pos}, {otherPos}, {otherPos - pos}");

                // Y축은 무시하고 X/Z 좌표만 사용하여 그리드 인접 여부를 확인합니다.
                // 풀숲의 높이가 조금 달라도 같은 그리드 위치로 판단할 수 있습니다.
                float deltaX = Mathf.Abs(diff.x);
                float deltaZ = Mathf.Abs(diff.z);

                bool isHorizontalNeighbor =
                    Mathf.Abs(deltaX - _gridSize) <= _tolerance &&
                    deltaZ <= _tolerance;

                Debug.Log($"isHorizontalNeighbor = {isHorizontalNeighbor}," +
                    $"{deltaX} - {_gridSize} <= {_tolerance}," +
                    $"{deltaZ} <= {_tolerance}");

                bool isVerticalNeighbor =
                    Mathf.Abs(deltaZ - _gridSize) <= _tolerance &&
                    deltaX <= _tolerance;

                Debug.Log($"isVerticalNeighbor = {isVerticalNeighbor}," +
                    $"{deltaZ} - {_gridSize} <= {_tolerance}," +
                    $"{deltaX} <= {_tolerance}");

                if (isHorizontalNeighbor == false && isVerticalNeighbor == false)
                    continue; // 그리드 간격이 아니면 패스

                detectedNeighborCount++;


                Debug.Log($"{current.name}과 {other.name} 방향 비교");

                // 방향 판별 (Y축 차이는 무시하고 X, Z축 중심)
                if (Mathf.Abs(diff.x) <= _tolerance && diff.z > (_gridSize - _tolerance))
                {
                    // 다른 블록이 위(Z+)에 있음 -> 나의 상(Up) 판넬 끄기
                    if (DisablePanel(current, _nameUp)) disabledCount++;
                }
                else if (Mathf.Abs(diff.x) <= _tolerance && diff.z < -(_gridSize - _tolerance))
                {
                    // 다른 블록이 아래(Z-)에 있음 -> 나의 하(Down) 판넬 끄기
                    if (DisablePanel(current, _nameDown)) disabledCount++;
                }
                else if (Mathf.Abs(diff.z) <= _tolerance && diff.x > (_gridSize - _tolerance))
                {
                    // 다른 블록이 우(X+)에 있음 -> 나의 우(Right) 판넬 끄기
                    if (DisablePanel(current, _nameRight)) disabledCount++;
                }
                else if (Mathf.Abs(diff.z) <= _tolerance && diff.x < -(_gridSize - _tolerance))
                {
                    // 다른 블록이 좌(X-)에 있음 -> 나의 좌(Left) 판넬 끄기
                    if (DisablePanel(current, _nameLeft)) disabledCount++;
                }
            }
        }

        if (detectedNeighborCount > 0 && disabledCount == 0)
        {
            Debug.LogWarning($"[풀숲 최적화 실패] 이웃한 블록을 {detectedNeighborCount}번 감지했지만 판넬을 하나도 끄지 못했습니다! 설정창의 '판넬 이름'이 실제 게임오브젝트 이름과 일치하는지 확인해 주세요.");
        }
        else
        {
            Debug.Log($"[풀숲 최적화 완료] 이웃 감지 {detectedNeighborCount}회 / 총 {disabledCount}개의 판넬을 비활성화했습니다.");
        }
    }

    void ResetAllBushes(BushBlock[] targetBushes = null)
    {
        BushBlock[] bushes = targetBushes ?? FindObjectsByType<BushBlock>(FindObjectsSortMode.None);
        foreach (var bush in bushes)
        {
            EnablePanel(bush, _nameUp);
            EnablePanel(bush, _nameDown);
            EnablePanel(bush, _nameLeft);
            EnablePanel(bush, _nameRight);
        }
    }

    bool DisablePanel(BushBlock bush, string keyword)
    {
        Transform panel = FindChildByKeyword(bush.transform, keyword);
        if (panel != null && panel.gameObject.activeSelf)
        {
            Undo.RecordObject(panel.gameObject, "Disable Bush Panel"); // Ctrl+Z(실행 취소) 지원
            panel.gameObject.SetActive(false);
            EditorUtility.SetDirty(panel.gameObject); // 씬 변경사항 저장 마킹
            return true;
        }
        return false;
    }

    void EnablePanel(BushBlock bush, string keyword)
    {
        Transform panel = FindChildByKeyword(bush.transform, keyword);
        if (panel != null && !panel.gameObject.activeSelf)
        {
            Undo.RecordObject(panel.gameObject, "Enable Bush Panel");
            panel.gameObject.SetActive(true);
            EditorUtility.SetDirty(panel.gameObject);
        }
    }

    Transform FindChildByKeyword(Transform parent, string keyword)
    {
        if (string.IsNullOrEmpty(keyword)) return null;

        string lowerKeyword = keyword.ToLower();

        // GetComponentsInChildren을 사용하면 자식의 자식(하위 전체)까지 모두 찾습니다. (true: 비활성화된 오브젝트 포함)
        Transform[] allChildren = parent.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in allChildren)
        {
            if (child == parent) continue; // 자기 자신은 제외

            if (child.name.ToLower().Contains(lowerKeyword))
            {
                return child;
            }
        }
        return null;
    }
}
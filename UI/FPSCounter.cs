using TMPro;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    [SerializeField] private TMP_Text _fpsText;

    private static FPSCounter _instance; // 씬을 다시 불러올 때마다 DontDestroyOnLoad 복제본이 쌓이지 않도록 하나만 유지

    private float _deltaTime;

    private void Awake()
    {
        // 이미 살아있는 FPS 표시가 있으면 새로 생긴 쪽을 제거(씬 재로드/재시작마다 복제본이 누적되는 것 방지)
        if (_instance != null)
        {
            Debug.LogWarning("[FPSCounter] 중복 인스턴스를 제거합니다."); // 복제본이 실제로 생기고 있었는지 logcat으로 확인용
            Destroy(gameObject);
            return;
        }

        _instance = this;

        // 씬이 바뀌어도 FPS 표시를 유지합니다.
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (_instance != this) return; // 제거 대기 중인 복제본은 갱신하지 않음

        // 프레임 간 시간의 평균을 이용해 FPS 변화를 조금 완화합니다.
        _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;

        float fps = 1f / _deltaTime;

        _fpsText.text = $"{fps:0} FPS";
    }
}
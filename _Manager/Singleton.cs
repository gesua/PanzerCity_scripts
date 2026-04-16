using UnityEngine;

/// <summary>
/// 씬 전환 시에도 인스턴스를 유지하는 제네릭 싱글톤 클래스
/// - DontDestroyOnLoad() 사용
/// - 게임 전역 매니저에 적합
/// - 이 클래스를 상속 받으면 싱글톤 객체 사용 가능
/// </summary>
public class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    // 유일한 인스턴스
    static T _instance;

    /// <summary>
    /// 싱글톤 인스턴스
    /// </summary>
    public static T Instance
    {
        get
        {
            // 유일한 객체(싱글톤 인스턴스)가 없었으면
            if (_instance == null)
            {
                // 씬 안에 이미 존재하는지 확인
                _instance = FindFirstObjectByType<T>();

                // 없으면 새로 만듦
                if (_instance == null)
                {
                    // 새 게임 오브젝트 생성
                    GameObject go = new GameObject(typeof(T).ToString() + " (Singleton)"); // 해당 클래스 이름으로 생성

                    // 싱글톤 인스턴스 생성 및 설정
                    _instance = go.AddComponent<T>();

                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        // 싱글톤 인스턴스가 없으면 자신으로 등록
        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else // 이미 싱글톤 인스턴스가 있다면 자신 제거
        {
            Destroy(gameObject);
        }
    }
}

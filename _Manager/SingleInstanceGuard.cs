using System.Threading;
using UnityEngine;

/// <summary>
/// 빌드 실행 파일이 중복 실행되는 것을 방지
/// Mutex를 이용해 이미 실행 중인 인스턴스가 있으면 새 인스턴스를 즉시 종료함
/// 에디터 Play 모드에서는 동작하지 않음(빌드에서만 적용)
/// </summary>
public static class SingleInstanceGuard
{
    const string MutexName = "PanzerCity_SingleInstanceMutex"; // 다른 프로그램과 겹치지 않는 고유한 이름

    static Mutex _mutex;

#if !UNITY_EDITOR
    /// <summary>
    /// 첫 씬이 로드되기 전에 실행되어, 중복 실행이면 씬 로드 자체를 막음
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void CheckSingleInstance()
    {
        _mutex = new Mutex(true, MutexName, out bool createdNew);

        if (createdNew == false) // 이미 다른 인스턴스가 Mutex를 잡고 있음
        {
            Debug.Log("이미 실행 중인 인스턴스가 있어 종료합니다.");
            Application.Quit();
            return;
        }

        Application.quitting += ReleaseMutex;
    }

    /// <summary>
    /// 앱 종료 시 Mutex 해제
    /// </summary>
    static void ReleaseMutex()
    {
        Application.quitting -= ReleaseMutex;

        if (_mutex == null) return;

        _mutex.ReleaseMutex();
        _mutex.Dispose();
        _mutex = null;
    }
#endif
}
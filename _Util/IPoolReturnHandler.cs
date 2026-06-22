/// <summary>
/// 풀에 반환되기 직전 호출되는 콜백
/// 정상 경로(Remove 등)와 강제 반환(Pool.ReturnAll) 모두에서 동일하게 호출됨
/// </summary>
public interface IPoolReturnHandler
{
    void OnBeforeReturnToPool();
}

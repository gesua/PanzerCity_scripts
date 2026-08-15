/// <summary>
/// 채팅 표시 관련 공통 유틸 — GameScene ChatUI와 Room 채팅(RoomChatRelay)이 공유
/// </summary>
public static class ChatFormatUtility
{
    static readonly string[] PlayerColors = { "#00FF00", "#0099FF", "#000000", "#FFFF00" }; // 1P 연두 / 2P 파랑 / 3P 검정 / 4P 노랑

    /// <summary>
    /// 룸에서 배정받은 자리(playerIndex, 0~3)로 채팅 색상 조회 — 범위를 벗어나면(조회 실패로 -1이 넘어오는 경우 등) 기본 흰색으로 대체
    /// </summary>
    public static string ResolveColor(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= PlayerColors.Length) return "#FFFFFF";

        return PlayerColors[playerIndex];
    }

    /// <summary>
    /// TMP 리치 텍스트 태그 주입 방지 — 닉네임/메시지에 '&lt;', '&gt;'가 섞여 있어도 색상 태그가 깨지지 않도록 치환
    /// </summary>
    public static string SanitizeForRichText(string text)
    {
        return text.Replace("<", "‹").Replace(">", "›");
    }
}
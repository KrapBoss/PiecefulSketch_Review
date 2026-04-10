using UnityEngine;

/// <summary>
/// 퍼즐 클리어 시간에 따른 보상을 계산하는 유틸리티 클래스입니다.
/// </summary>
public static class PuzzleRewardCalculator
{
    private const int TIME_ATTACK_RANK_COUNT = 3;

    /// <summary>
    /// 등급별 목표 시간을 반환합니다.
    /// </summary>
    /// <param name="rank">보상 등급 (1, 2, 3)</param>
    /// <param name="pieceCount">퍼즐 조각 개수</param>
    /// <returns>목표 시간(초)</returns>
    public static float GetTargetTime(int rank, int pieceCount)
    {
        switch (rank)
        {
            case 1: return pieceCount * 6.0f; // #1 보상 (4.0초)
            case 2: return pieceCount * 4.0f; // #2 보상 (2.0초)
            case 3: return pieceCount * 2.3f; // #3 보상 (1.0초)
            default: return float.MaxValue;
        }
    }

    /// <summary>
    /// 등급별 추가 보상액을 반환합니다.
    /// </summary>
    /// <param name="rank">보상 등급 (1, 2, 3)</param>
    /// <param name="baseReward">기본 보상</param>
    /// <returns>추가 보상액</returns>
    public static int GetRewardAmount(int rank, int baseReward)
    {
        switch (rank)
        {
            case 1: return (int)(baseReward * 0.5f); // #1 보상 (0.5배)
            case 2: return (int)(baseReward * 0.5f); // #2 보상 (0.5배)
            case 3: return (int)(baseReward * 1.0f); // #3 보상 (1.0배)
            default: return 0;
        }
    }
    
    /// <summary>
    /// 모든 등급의 목표 시간을 배열로 반환합니다.
    /// </summary>
    /// <param name="pieceCount">퍼즐 조각 개수</param>
    /// <returns>등급별 목표 시간 배열 (내림차순)</returns>
    public static float[] GetTargetTimes(int pieceCount)
    {
        float[] times = new float[TIME_ATTACK_RANK_COUNT];
        for (int i = 0; i < TIME_ATTACK_RANK_COUNT; i++)
        {
            times[i] = GetTargetTime(i + 1, pieceCount);
        }
        return times;
    }
    
    /// <summary>
    /// 클리어 시간으로 달성한 모든 추가 보상을 합산하여 반환합니다.
    /// </summary>
    /// <param name="clearTime">클리어 시간</param>
    /// <param name="puzzle">퍼즐 데이터</param>
    /// <returns>총 추가 보상</returns>
    public static int GetTotalAdditionalReward(float clearTime, PuzzleSO puzzle)
    {
        int totalReward = 0;
        for (int rank = 1; rank <= TIME_ATTACK_RANK_COUNT; rank++)
        {
            if (clearTime <= GetTargetTime(rank, puzzle.PieceCount))
            {
                totalReward += GetRewardAmount(rank, puzzle.RewardCoin);
            }
        }
        return totalReward;
    }

    /// <summary>
    /// 반복 클리어 시의 반복 보상액을 계산합니다.
    /// </summary>
    /// <param name="progress">플레이어의 퍼즐 진행도</param>
    /// <param name="puzzleData">해당 퍼즐의 SO 데이터</param>
    /// <returns>계산된 반복 보상액. 클리어하지 않았으면 0을 반환.</returns>
    public static int CalculateRepeatReward(PuzzleProgress progress, PuzzleSO puzzleData)
    {
        if (!progress.IsCleared)
        {
            return 0;
        }

        // 현재까지 달성한 모든 보상을 합산한다.
        int totalPastReward = puzzleData.RewardCoin; // 기본 클리어 보상부터 시작
        for (int rank = 1; rank <= 3; rank++)
        {
            if (progress.TimeAttackRewardsClaimed[rank - 1])
            {
                totalPastReward += GetRewardAmount(rank, puzzleData.RewardCoin);
            }
        }

        // 합산된 총액의 20%를 반복 보상으로 설정
        return Mathf.FloorToInt(totalPastReward * 0.2f);
    }
}

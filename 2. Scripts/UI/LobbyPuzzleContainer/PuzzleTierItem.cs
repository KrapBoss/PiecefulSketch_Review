using UnityEngine;

/// <summary>
/// 퍼즐 현재 완성도에 따른 티어 정보를 표기하는 기능
/// </summary>
public class PuzzleTierItem : MonoBehaviour
{
    [SerializeField] GameObject rewardIcon;
    [SerializeField] private GameObject[] rewardTierIcons = new GameObject[4];

    public void UpdateRewardTierIcons(PuzzleProgress progress)
    {
        if (rewardTierIcons == null || rewardTierIcons.Length != 4) return;

        foreach (var icon in rewardTierIcons)
        {
            if (icon != null) icon.SetActive(false);
        }

        if (progress == null || !progress.IsCleared)
        {
            if (rewardIcon) rewardIcon.SetActive(false);
            return;
        }

        if (rewardIcon) rewardIcon.SetActive(true);

        int highestTierIndex = 0;
        if (progress.TimeAttackRewardsClaimed.Length > 2 && progress.TimeAttackRewardsClaimed[2]) highestTierIndex = 3;
        else if (progress.TimeAttackRewardsClaimed.Length > 1 && progress.TimeAttackRewardsClaimed[1]) highestTierIndex = 2;
        else if (progress.TimeAttackRewardsClaimed.Length > 0 && progress.TimeAttackRewardsClaimed[0]) highestTierIndex = 1;

        if (rewardTierIcons[highestTierIndex] != null)
        {
            rewardTierIcons[highestTierIndex].SetActive(true);
        }
    }
}

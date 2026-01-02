using System;
using System.Collections.Generic;

[Serializable]
public class LevelStarData
{
    public int LevelNumber;
    public int Stars;
    
    public LevelStarData(int level, int stars)
    {
        LevelNumber = level;
        Stars = stars;
    }
}

[Serializable]
public class ProgressData
{
    public int LastClearedLevel;
    public int BestTime;
    public bool TutorialCompleted;
    public bool EndingCompleted;
    public bool isLevel4Completed;
    
    public List<LevelStarData> LevelStars = new List<LevelStarData>();
    
    public ProgressData()
    {
        LastClearedLevel = 0;
        BestTime = 0;
        TutorialCompleted = false;
        EndingCompleted = false;
        isLevel4Completed = false;
        LevelStars = new List<LevelStarData>();
    }
    
    // 별 개수 가져오기
    public int GetStars(int level)
    {
        var data = LevelStars.Find(x => x.LevelNumber == level);
        return data != null ? data.Stars : 0;
    }
    
    // 별 개수 설정 (기존보다 높으면 갱신)
    public void SetStars(int level, int stars)
    {
        var existing = LevelStars.Find(x => x.LevelNumber == level);
        if (existing != null)
        {
            // 기존 기록보다 높으면 갱신
            if (stars > existing.Stars)
            {
                existing.Stars = stars;
            }
        }
        else
        {
            LevelStars.Add(new LevelStarData(level, stars));
        }
    }
    
    // 해당 레벨 클리어 여부
    public bool HasCleared(int level)
    {
        return LevelStars.Exists(x => x.LevelNumber == level);
    }
}
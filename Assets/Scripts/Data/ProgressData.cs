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
    public bool isTutorialSequenceFinished; // 시퀀스 시청 완료 여부
    public bool isTutorialComplete;         // 튜토리얼 전체(메인홈 버튼 클릭) 완료 여부
    public bool EndingCompleted;
    public bool isLevel100Completed;
    
    public List<LevelStarData> LevelStars = new List<LevelStarData>();
    
    // ✅ 추가: 사과 개수
    public int TotalApples = 0;
    
    public ProgressData()
    {
        LastClearedLevel = 0;
        BestTime = 0;
        isTutorialComplete = false;
        isTutorialSequenceFinished = false;
        EndingCompleted = false;
        isLevel100Completed = false;
        LevelStars = new List<LevelStarData>();
        TotalApples = 0;
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
    
    // ✅ 사과 추가
    public void AddApples(int amount)
    {
        TotalApples += amount;
        SaveManager.Save(this, "ProgressData");
    }
    
    // ✅ 사과 차감
    public bool SpendApples(int amount)
    {
        if(TotalApples < amount) return false;
        TotalApples -= amount;
        SaveManager.Save(this, "ProgressData");
        return true;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelConfig", menuName = "Configs/LevelConfig")]
public class LevelConfig : ScriptableObject
{
    [Header("레벨 기본 정보")]
    public int ChapterNumber; // 1, 2, 3, 100
    
    [Header("난이도 범위")]
    public int MinBox = 5;
    public int MaxBox = 15;
    public float MaxTime = 120f;
    public float MinTime = 60f;
    
    [Header("보석 설정")]
    [Range(1,5)]
    public int GemTypeCount = 5;
    public int MaxRequiredPerBox;
    
    [Header("아이템 기본 개수")]
    [Tooltip("힌트 최대 사용 횟수 (무료)")]
    public int MaxHintCount = 1;
    
    [Tooltip("되돌리기 최대 사용 횟수 (무료)")]
    public int MaxUndoCount = 3;
    
    [Tooltip("새로고침 최대 사용 횟수 (무료)")]
    public int MaxUndo1Count = 3;
    
    [Header("시간추가 설정")]
    public float TimeAddAmount = 10f; // 10초 고정
    
    [Header("배경 설정")]
    public string BackgroundVideoFileName = "bg_chapter1.mp4";
    public AudioClip AdditionalBGMLayer; // 챕터별 추가 사운드
    
    /// <summary>
    /// 챕터 내 레벨 번호에 따른 난이도 계산
    /// </summary>
    /// <param name="levelInChapter">챕터 내 레벨 (1~33 또는 1)</param>
    /// <returns>(상자 개수, 제한 시간)</returns>
    public (int boxCount, float timeLimit) CalculateDifficulty(int levelInChapter)
    {
        // 레벨100은 특수 처리
        if(ChapterNumber == 100)
        {
            return (MaxBox, MinTime);
        }
        
        int boxCount;
        float timeLimit;
        
        if(levelInChapter <= 11)
        {
            // 레벨 1~11: 상자만 증가 (5→15)
            boxCount = MinBox + (levelInChapter - 1);
            timeLimit = MaxTime;
        }
        else
        {
            // 레벨 12~33: 시간만 감소 (비선형)
            boxCount = MaxBox;
            
            float progress = (levelInChapter - 12) / 21f; // 0.0 ~ 1.0
            float curve = Mathf.Pow(progress, 1.5f); // 초반 완만, 후반 가파름
            timeLimit = Mathf.Lerp(MaxTime, MinTime, curve);
        }
        
        return (boxCount, timeLimit);
    }
}
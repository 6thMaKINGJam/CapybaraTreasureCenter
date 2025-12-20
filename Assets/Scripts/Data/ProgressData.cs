using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


// 앱 삭제 전까지 유지되는 진행/기록 데이터

[Serializable]
public class ProgressData
{
    public int LastClearedLevel; // 1. 마지막으로 클리어했던 레벨
    public int BestTime; // 2. 최고 시간 기록 (수정.효빈 bestScore -> bestTime)
    public int TotalGemCount; // 3. 누적 획득한 보석 수

    public bool TutorialCompleted; // 4. 튜토리얼 완료 여부 (추가.효빈)

    public bool EndingCompleted; //5.엔딩 완료 여부 추가.효빈

    public bool hasSeenLevel4Ending; // 6.레벨4 엔딩 시퀀스 시청 여부 추가.연주
    



    // 4. 그리고 진행/기록 데이터 초기화 
    public ProgressData()
    {
        LastClearedLevel = 0;
        BestTime = 0;
        TotalGemCount = 0;
        TutorialCompleted = false;
        EndingCompleted = false; // 추가
        hasSeenLevel4Ending = false; // 추가
    }
}
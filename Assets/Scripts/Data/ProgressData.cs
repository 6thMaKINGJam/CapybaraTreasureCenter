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


    public bool TutorialCompleted; // 4. 튜토리얼 완료 여부 (추가.효빈)

    public bool EndingCompleted; //5.엔딩 완료 여부 추가.효빈

    public bool isLevel4Completed; // 6.레벨4 완료 유무 
    // ✅ 추가: 레벨별 최고 별 개수 (key: 레벨번호, value: 별개수)
    public Dictionary<int, int> LevelStars = new Dictionary<int, int>();




    // 4. 그리고 진행/기록 데이터 초기화 
    public ProgressData()
    {
        LastClearedLevel = 0;
        BestTime = 0;
      
        TutorialCompleted = false;
        EndingCompleted = false; // 추가
        isLevel4Completed = false; // 추가
    }
}
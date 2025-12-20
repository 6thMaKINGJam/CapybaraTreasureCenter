using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


// 앱 삭제 전까지 유지되는 진행/기록 데이터

[Serializable]
public class ProgressData
{
    public int LastClearedLevel; // 1. 마지막으로 클리어했던 레벨
    public int BestScore; // 2. 최고 기록
    public int TotalGemCount; // 3. 누적 획득한 보석 수

    // 4. 그리고 진행/기록 데이터 초기화 
    public ProgressData()
    {
        LastClearedLevel = 0;
        BestScore = 0;
        TotalGemCount = 0;
    }
}
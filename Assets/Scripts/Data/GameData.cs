using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

//GemType/GemCount/GemBundle/Box/
//Chunk/CompletedBox/GameState 직렬화 데이터 구조

// 1. 게임 상태(GameState) 정의
public enum GameState { Ready, Playing, Paused, GameOver, Win, TimeOver }
// 2. 보석 종류(GemType) 정의
public enum GemType { Red, Blue, Green, Yellow, Purple }

// 3. 보석 묶음(GemBundle) 데이터
[Serializable]
public class GemBundle
{
    public string BundleID; // 묶음의 고유 ID
    public GemType GemType; // 보석 색깔
    public int GemCount; // 보석 개수 
}

// 4. 상자(Box/CompletedBox) 데이터
[Serializable]
public class Box
{
    public GemType TargetType; // 상자가 받아야 할 보석 종류
    public int RequiredAmount; // 목표 개수
    public int CurrentAmount; // 현재 담겨 있는 개수
    public bool isCompletedBox; // 상자가 다 찼는지 여부
    
   [System.NonSerialized] // 저장 안 함 (힌트용)
    public List<GemBundle> SolutionBundles; // 이 상자의 정답 조합
}

// 5. 묶음 단위 생성(Chunk) 데이터 -> chunkGenerator.cs로 이동


// 6. 게임 데이터(GameData) 직렬화
[Serializable]
public class GameData
{
    [Header("진행도 데이터")]
    public int CurrentLevelIndex;
    public int CurrentBoxIndex;
    
    [Header("상태 데이터")]
    public GameState GameState;
    public List<Box> Boxes = new List<Box>();
    
    // ===== 추가 =====
    [Header("번들 풀 관리")]
    public List<GemBundle> BundlePool = new List<GemBundle>(); // 전체 묶음 풀
    public List<GemBundle> CurrentDisplayBundles = new List<GemBundle>(); // 화면에 표시 중인 12개
    public List<GemBundle> SelectedBundles = new List<GemBundle>(); // 현재 선택된 묶음들
    
    [Header("보석 총량")]
    public Dictionary<GemType, int> RemainingGems = new Dictionary<GemType, int>();
    
    [Header("완료 이력")]
    public List<CompletedBox> CompletedBoxes = new List<CompletedBox>();
    
    [Header("아이템 사용 정보")]
    public int UndoCount;
    public int RefreshCount;

public int HintCount;   
    
    [Header("시간 기록")]
    public float StartTime; // 레벨 시작 시간
    public float ElapsedTime; // 누적 플레이 시간
}

// 완료된 상자 정보
[Serializable]
public class CompletedBox
{
    public int BoxIndex;
    public List<GemBundle> UsedBundles = new List<GemBundle>();
}
